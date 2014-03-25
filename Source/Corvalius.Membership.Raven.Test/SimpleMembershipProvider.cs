using Moq;
using Raven.Client;
using Raven.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Security;
using Xunit;

namespace Corvalius.Membership.Raven.Test
{
    public class SimpleMembershipProviderTest : RavenTestBase
    {
        private SimpleMembershipProvider CreateDefaultSimpleMembershipProvider(IDocumentStore store)
        {
            Initializer.InitializeDatabaseConnection(store);

            var simpleMembershipProvider = new SimpleMembershipProvider();
            NameValueCollection config = new NameValueCollection();
            simpleMembershipProvider.Initialize("AspNetSqlMembershipProvider", config);

            return simpleMembershipProvider;
        }

        [Fact]
        public void ConfirmAccountReturnsFalseIfNoRecordExistsForToken()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                bool result = simpleMembershipProvider.ConfirmAccount("foo");

                // Assert
                Assert.False(result);
            }
        }

        [Fact]
        public void ConfirmAccountReturnsFalseIfConfirmationTokenDoesNotMatchInCase()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);
                using (var db = store.OpenSession())
                {
                    db.Store(new UserEntity("Foo"));
                    db.Store(new ProfileEntity() { Name = "Foo", IsConfirmed = false, ConfirmationToken = "NotFoo" });

                    db.SaveChanges();
                }

                // Wait for indexing to finish.
                WaitForIndexing(store);

                // Act
                bool result = simpleMembershipProvider.ConfirmAccount("foo");

                // Assert
                Assert.False(result);
            }
        }

        [Fact]
        public void ConfirmAccountReturnsFalseIfNoConfirmationTokenFromMultipleListMatchesInCase()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);
                using (var db = store.OpenSession())
                {
                    db.Store(new UserEntity("Foo"));
                    db.Store(new ProfileEntity() { Name = "Foo", IsConfirmed = false, ConfirmationToken = "Foo" });

                    db.Store(new UserEntity("Foo1"));
                    db.Store(new ProfileEntity() { Name = "Foo1", IsConfirmed = false, ConfirmationToken = "fOo" });

                    db.SaveChanges();
                }

                // Wait for indexing to finish.
                WaitForIndexing(store);

                // Act
                bool result = simpleMembershipProvider.ConfirmAccount("foo");

                // Assert
                Assert.False(result);
            }
        }

        [Fact]
        public void ConfirmAccountUpdatesIsConfirmedFieldIfConfirmationTokenMatches()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);
                using (var db = store.OpenSession())
                {
                    db.Store(new UserEntity("Foo"));
                    db.Store(new ProfileEntity() { Name = "Foo", IsConfirmed = false, ConfirmationToken = "foo" });

                    db.SaveChanges();
                }

                // Wait for indexing to finish.
                WaitForIndexing(store);

                // Act
                bool result = simpleMembershipProvider.ConfirmAccount("foo");

                // Assert
                Assert.True(result);

                using (var db = store.OpenSession())
                {
                    var profile = db.Load<ProfileEntity>(ProfileEntity.IdPrefix + "Foo");
                    Assert.NotNull(profile);
                    Assert.True(profile.IsConfirmed);
                }
            }
        }

        [Fact]
        public void CreateAccountAndUserWhenNeitherExists()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", true);

                // Assert
                Assert.NotEmpty(token);

                using (var db = store.OpenSession())
                {
                    var user = db.Load<UserEntity>(UserEntity.ToRavenId("red"));
                    Assert.NotNull(user);
                    Assert.NotEqual(0, user.ReverseId);
                    Assert.Empty(user.Roles);

                    var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId("red"));
                    Assert.NotNull(profile);
                    Assert.False(profile.IsConfirmed);
                }
            }
        }

        [Fact]
        public void CreateAccountButFailedToCreateAccountBecauseOfEmptyPassword()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                Assert.Throws<MembershipCreateUserException>(() => simpleMembershipProvider.CreateUserAndAccount("red", string.Empty, true));
            }
        }

        [Fact]
        public void CurrentlyUnsupportedFunctionality()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                Assert.Empty(simpleMembershipProvider.GetAccountsForUser(string.Empty));

                MembershipCreateStatus result;
                Assert.Throws<NotSupportedException>(() => simpleMembershipProvider.CreateUser(null, null, null, null, null, true, null, out result));

                Assert.Throws<NotSupportedException>(() => simpleMembershipProvider.UpdateUser(null));
                Assert.Throws<NotSupportedException>(() => simpleMembershipProvider.UnlockUser(null));
            }
        }

        [Fact]
        public void CreateAndConfirmAccount()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                var now = DateTime.UtcNow;
                Thread.Sleep(1000);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", true);
                bool confirmationResult = simpleMembershipProvider.ConfirmAccount("red", token);
                bool isConfirmedResult = simpleMembershipProvider.IsConfirmed("red");
                DateTime createdDate = simpleMembershipProvider.GetCreateDate("red");

                // Assert
                Assert.True(confirmationResult);
                Assert.True(isConfirmedResult);
                Assert.True(createdDate > now);

                using (var db = store.OpenSession())
                {
                    var user = db.Load<UserEntity>(UserEntity.ToRavenId("red"));
                    Assert.NotNull(user);
                    Assert.NotEqual(0, user.ReverseId);

                    var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId("red"));
                    Assert.NotNull(profile);
                    Assert.True(profile.IsConfirmed);
                    Assert.False(string.IsNullOrWhiteSpace(profile.Password));
                }
            }
        }

        [Fact]
        public void CreateUserAndEnsureItIsLocal()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);
                bool isConfirmedResult = simpleMembershipProvider.IsConfirmed("red");

                int userId = simpleMembershipProvider.GetUserIdFromUsername("red");
                bool isLocal = simpleMembershipProvider.HasLocalAccount(userId);

                string username = simpleMembershipProvider.GetUserNameFromId(userId);

                // Assert
                Assert.NotEqual(0, userId);
                Assert.True(isLocal);
                Assert.True(isConfirmedResult);
                Assert.Equal("red", username);
            }
        }

        [Fact]
        public void DeleteUserAndAccount()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);

                int userId = simpleMembershipProvider.GetUserIdFromUsername("red");

                bool isAccountDeleted = simpleMembershipProvider.DeleteAccount("red");
                bool isUserDeleted = simpleMembershipProvider.DeleteUser("red", true);

                // Assert
                Assert.True(isUserDeleted);
                Assert.True(isAccountDeleted);

                using (var db = store.OpenSession())
                {
                    var user = db.Load<UserEntity>(UserEntity.ToRavenId("red"));
                    Assert.Null(user);

                    var userMap = db.Load<UserMapIdEntity>(UserMapIdEntity.ToRavenId(userId));
                    Assert.Null(userMap);

                    var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId("red"));
                    Assert.Null(profile);
                }
            }
        }

        [Fact]
        public void CreateAccountAndValidate()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);

                var now = DateTime.UtcNow;
                Thread.Sleep(1000);

                bool validate = simpleMembershipProvider.ValidateUser("red", "Mypass");
                Assert.False(validate);

                DateTime lastFailure = simpleMembershipProvider.GetLastPasswordFailureDate("red");
                Assert.True(now < lastFailure);

                int failures = simpleMembershipProvider.GetPasswordFailuresSinceLastSuccess("red");
                Assert.Equal(1, failures);

                validate = simpleMembershipProvider.ValidateUser("red", "mypass");
                Assert.True(validate);

                failures = simpleMembershipProvider.GetPasswordFailuresSinceLastSuccess("red");
                Assert.Equal(0, failures);
            }
        }

        [Fact]
        public void ResetPasswordAndValidate()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);

                string resetToken = simpleMembershipProvider.GeneratePasswordResetToken("red");

                var now = DateTime.UtcNow;
                Thread.Sleep(1000);

                bool resetAccomplished = simpleMembershipProvider.ResetPasswordWithToken(resetToken, "myPass");
                DateTime changedDate = simpleMembershipProvider.GetPasswordChangedDate("red");

                // Assert

                Assert.NotEmpty(resetToken);
                Assert.True(resetAccomplished);
                Assert.True(now < changedDate);

                bool validate = simpleMembershipProvider.ValidateUser("red", "myPass");
                Assert.True(validate);

                validate = simpleMembershipProvider.ValidateUser("red", "mypass");
                Assert.False(validate);
            }
        }

        [Fact]
        public void ResetPasswordFailedWithWrongToken()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);

                string resetToken = simpleMembershipProvider.GeneratePasswordResetToken("red");
                bool resetAccomplished = simpleMembershipProvider.ResetPasswordWithToken(resetToken + "extra", "myPass");

                // Assert
                Assert.False(resetAccomplished);

                bool validate = simpleMembershipProvider.ValidateUser("red", "myPass");
                Assert.False(validate);

                validate = simpleMembershipProvider.ValidateUser("red", "mypass");
                Assert.True(validate);
            }
        }

        [Fact]
        public void GetUserFromResetToken()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);
                string resetToken = simpleMembershipProvider.GeneratePasswordResetToken("red");

                WaitForIndexing(store);

                int userId1 = simpleMembershipProvider.GetUserIdFromPasswordResetToken(resetToken);
                int userId2 = simpleMembershipProvider.GetUserIdFromUsername("red");

                Assert.Equal(userId1, userId2);
            }
        }

        [Fact]
        public void GetUser()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);

                var user = simpleMembershipProvider.GetUser("red", false);

                Assert.NotNull(user);
                Assert.Equal("red", user.UserName);

                using (var db = store.OpenSession())
                {
                    var userEntity = db.Load<UserEntity>(UserEntity.ToRavenId(user.UserName));
                    Assert.Equal(userEntity.ReverseId, user.ProviderUserKey);
                }
            }
        }

        [Fact]
        public void ChangePasswordAndValidate()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);

                var now = DateTime.UtcNow;
                Thread.Sleep(1000);

                bool changeAccomplished = simpleMembershipProvider.ChangePassword("red", "mypass", "MyPass");
                DateTime changedDate = simpleMembershipProvider.GetPasswordChangedDate("red");

                // Assert
                Assert.True(changeAccomplished);
                Assert.True(now < changedDate);

                bool validate = simpleMembershipProvider.ValidateUser("red", "MyPass");
                Assert.True(validate);

                validate = simpleMembershipProvider.ValidateUser("red", "mypass");
                Assert.False(validate);
            }
        }

        [Fact]
        public void ChangePasswordFailedWithWrongPassword()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);

                bool changeAccomplished = simpleMembershipProvider.ChangePassword("red", "mypass1", "MyPass");
                DateTime changedDate = simpleMembershipProvider.GetPasswordChangedDate("red");

                // Assert
                Assert.False(changeAccomplished);

                bool validate = simpleMembershipProvider.ValidateUser("red", "MyPass");
                Assert.False(validate);

                validate = simpleMembershipProvider.ValidateUser("red", "mypass");
                Assert.True(validate);
            }
        }

        [Fact]
        public void CreateOAuthAccountAndUserWhenNeitherExistsAndReassociatingItToADifferentUser()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);
                simpleMembershipProvider.CreateOrUpdateOAuthAccount("facebook", "93939322", "red");

                simpleMembershipProvider.CreateUserAndAccount("red1", "mypass", false);
                simpleMembershipProvider.CreateOrUpdateOAuthAccount("facebook", "93939322", "red1");

                WaitForIndexing(store);

                var accounts = simpleMembershipProvider.GetAccountsForUser("red");
                var accounts1 = simpleMembershipProvider.GetAccountsForUser("red1");

                // Assert
                Assert.Empty(token);
                Assert.Equal(0, accounts.Count());
                Assert.Equal(1, accounts1.Count());
                Assert.Equal("facebook", accounts1.First().Provider);
                Assert.Equal("93939322", accounts1.First().ProviderUserId);

                using (var db = store.OpenSession())
                {
                    var profile = db.Load<OAuthProfileEntity>(OAuthProfileEntity.ToRavenId("facebook", "93939322"));
                    Assert.NotNull(profile);
                    Assert.Equal("facebook", profile.Provider);
                    Assert.Equal("93939322", profile.ProviderUserId);
                    Assert.Equal(UserEntity.ToRavenId("red1"), profile.UserId);

                    var user = db.Load<UserEntity>(profile.UserId);
                    Assert.NotNull(user);
                }
            }
        }

        [Fact]
        public void CreateMultipleOAuthAccountsForUser()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);
                simpleMembershipProvider.CreateOrUpdateOAuthAccount("facebook", "93939322", "red");
                simpleMembershipProvider.CreateOrUpdateOAuthAccount("linkedin", "93939322", "red");

                WaitForIndexing(store);

                var accounts = simpleMembershipProvider.GetAccountsForUser("red");

                // Assert
                Assert.Empty(token);
                Assert.Equal(2, accounts.Count());

                using (var db = store.OpenSession())
                {
                    var profile = db.Load<OAuthProfileEntity>(OAuthProfileEntity.ToRavenId("facebook", "93939322"));
                    Assert.NotNull(profile);
                    Assert.Equal("facebook", profile.Provider);
                    Assert.Equal("93939322", profile.ProviderUserId);
                    Assert.Equal(UserEntity.ToRavenId("red"), profile.UserId);

                    var user = db.Load<UserEntity>(profile.UserId);
                    Assert.NotNull(user);
                }
            }
        }

        [Fact]
        public void GetOAuthTokenSecret()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                simpleMembershipProvider.StoreOAuthRequestToken("test", "secret");
                simpleMembershipProvider.StoreOAuthRequestToken("test1", "--");

                string s1 = simpleMembershipProvider.GetOAuthTokenSecret("test");
                string s2 = simpleMembershipProvider.GetOAuthTokenSecret("test1");

                simpleMembershipProvider.StoreOAuthRequestToken("test", "secret1");
                string s3 = simpleMembershipProvider.GetOAuthTokenSecret("test");

                // Assert
                Assert.Equal("secret", s1);
                Assert.Equal("--", s2);
                Assert.Equal("secret1", s3);
            }
        }

        [Fact]
        public void ReplaceOAuthRequestTokenAndDelete()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act

                simpleMembershipProvider.StoreOAuthRequestToken("test", "secret");
                string s1 = simpleMembershipProvider.GetOAuthTokenSecret("test");

                simpleMembershipProvider.ReplaceOAuthRequestTokenWithAccessToken("test", "test", "secret");
                string s2 = simpleMembershipProvider.GetOAuthTokenSecret("test");

                simpleMembershipProvider.ReplaceOAuthRequestTokenWithAccessToken("test", "test1", "secret1");
                string s3 = simpleMembershipProvider.GetOAuthTokenSecret("test1");

                simpleMembershipProvider.DeleteOAuthToken("test1");

                // Assert
                Assert.Equal("secret", s1);
                Assert.Equal("secret", s2);
                Assert.Equal("secret1", s3);

                using (var db = store.OpenSession())
                {
                    var token = db.Load<TokenEntity>("test1");
                    Assert.Null(token);
                }
            }
        }

        [Fact]
        public void CreateOAuthAccountAndDeleteIt()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                // Act
                string token = simpleMembershipProvider.CreateUserAndAccount("red", "mypass", false);
                simpleMembershipProvider.CreateOrUpdateOAuthAccount("facebook", "93939322", "red");

                int userBeforeDelete = simpleMembershipProvider.GetUserIdFromOAuth("facebook", "93939322");
                simpleMembershipProvider.DeleteOAuthAccount("facebook", "93939322");
                int userAfterDelete = simpleMembershipProvider.GetUserIdFromOAuth("facebook", "93939322");

                // Assert
                Assert.NotEqual(-1, userBeforeDelete);
                Assert.Equal(-1, userAfterDelete);
            }
        }

        [Fact]
        public void RequestAndVerifyAccessToken()
        {
            string username = "red";

            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                string token = simpleMembershipProvider.GenerateAccessToken(username, TimeSpan.FromMinutes(10));

                Assert.True(simpleMembershipProvider.VerifyAccessToken(username, token));
                Assert.False(simpleMembershipProvider.VerifyAccessToken(username, token + "a"));
                Assert.False(simpleMembershipProvider.VerifyAccessToken(username, null));
                Assert.False(simpleMembershipProvider.VerifyAccessToken(username, ""));
            }
        }
    }
}
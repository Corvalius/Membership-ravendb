using Raven.Client;
using Raven.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Security;

namespace Corvalius.Membership.Raven
{
    public class SimpleMembershipProvider : ExtendedMembershipProvider
    {
        public static readonly string EnableRavenDbSimpleMembershipKey = "enableRavenDbSimpleMembership";       

        private const int TokenSizeInBytes = 16;
        private readonly MembershipProvider _previousProvider;

        public SimpleMembershipProvider()
            : this(null)
        {
        }

        public SimpleMembershipProvider(MembershipProvider previousProvider)
        {
            _previousProvider = previousProvider;
            if (_previousProvider != null)
            {
                _previousProvider.ValidatingPassword += (sender, args) =>
                {
                    if (!InitializeCalled)
                    {
                        OnValidatingPassword(args);
                    }
                };
            }
        }

        private MembershipProvider PreviousProvider
        {
            get
            {
                if (_previousProvider == null)
                {
                    throw new InvalidOperationException(Resources.Security_InitializeMustBeCalledFirst);
                }
                else
                {
                    return _previousProvider;
                }
            }
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            if (string.IsNullOrEmpty(name))
                name = "RavenSimpleMembershipProvider";

            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "A RavenDB Extended Membership Provider.");
            }

            base.Initialize(name, config);

            config.Remove("connectionStringName");
            config.Remove("enablePasswordRetrieval");
            config.Remove("enablePasswordReset");
            config.Remove("requiresQuestionAndAnswer");
            config.Remove("applicationName");
            config.Remove("requiresUniqueEmail");
            config.Remove("maxInvalidPasswordAttempts");
            config.Remove("passwordAttemptWindow");
            config.Remove("passwordFormat");
            config.Remove("name");
            config.Remove("description");
            config.Remove("minRequiredPasswordLength");
            config.Remove("minRequiredNonalphanumericCharacters");
            config.Remove("passwordStrengthRegularExpression");
            config.Remove("hashAlgorithmType");

            if (config.Count > 0)
            {
                string attribUnrecognized = config.GetKey(0);
                if (!String.IsNullOrEmpty(attribUnrecognized))
                {
                    throw new ProviderException(String.Format(CultureInfo.CurrentCulture, Resources.SimpleMembership_ProviderUnrecognizedAttribute, attribUnrecognized));
                }
            }

            this.InitializeCalled = true;
        }

        // Public properties
        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool EnablePasswordRetrieval
        {
            get { return InitializeCalled ? false : PreviousProvider.EnablePasswordRetrieval; }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool EnablePasswordReset
        {
            get { return InitializeCalled ? false : PreviousProvider.EnablePasswordReset; }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool RequiresQuestionAndAnswer
        {
            get { return InitializeCalled ? false : PreviousProvider.RequiresQuestionAndAnswer; }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool RequiresUniqueEmail
        {
            get { return InitializeCalled ? false : PreviousProvider.RequiresUniqueEmail; }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override MembershipPasswordFormat PasswordFormat
        {
            get { return InitializeCalled ? MembershipPasswordFormat.Hashed : PreviousProvider.PasswordFormat; }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override int MaxInvalidPasswordAttempts
        {
            get { return InitializeCalled ? Int32.MaxValue : PreviousProvider.MaxInvalidPasswordAttempts; }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override int PasswordAttemptWindow
        {
            get { return InitializeCalled ? Int32.MaxValue : PreviousProvider.PasswordAttemptWindow; }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override int MinRequiredPasswordLength
        {
            get { return InitializeCalled ? 0 : PreviousProvider.MinRequiredPasswordLength; }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return InitializeCalled ? 0 : PreviousProvider.MinRequiredNonAlphanumericCharacters; }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override string PasswordStrengthRegularExpression
        {
            get { return InitializeCalled ? String.Empty : PreviousProvider.PasswordStrengthRegularExpression; }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override string ApplicationName
        {
            get
            {
                if (InitializeCalled)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    return PreviousProvider.ApplicationName;
                }
            }
            set
            {
                if (InitializeCalled)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    PreviousProvider.ApplicationName = value;
                }
            }
        }

        #region Operations

        public int GetUserIdFromUsername(string username)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                    throw new ArgumentException(Resources.Security_NoUserFound);

                return user.ReverseId;
            }
        }

        // Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method
        public override int GetUserIdFromPasswordResetToken(string token)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var query = from user in db.Query<ProfileEntity, ProfileEntityByAccountTokensIndex>()
                            where user.PasswordVerificationToken == token
                            select user;

                if (query.Any())
                {
                    var profile = query.First();
                    var user = db.Load<UserEntity>(UserEntity.ToRavenId(profile.Name));

                    return user.ReverseId;
                }
                return -1;
            }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.ChangePasswordQuestionAndAnswer(username, password, newPasswordQuestion, newPasswordAnswer);
            }
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the confirmed flag for the username if it is correct.
        /// </summary>
        /// <returns>True if the account could be successfully confirmed. False if the username was not found or the confirmation token is invalid.</returns>
        /// <remarks>Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method</remarks>
        public override bool ConfirmAccount(string username, string accountConfirmationToken)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(username));
                if (profile == null)
                    return false;

                if (String.Equals(profile.ConfirmationToken, accountConfirmationToken, StringComparison.Ordinal))
                {
                    profile.IsConfirmed = true;
                    db.SaveChanges();

                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Sets the confirmed flag for the username if it is correct.
        /// </summary>
        /// <returns>True if the account could be successfully confirmed. False if the username was not found or the confirmation token is invalid.</returns>
        /// <remarks>Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method.
        /// There is a tiny possibility where this method fails to work correctly. Two or more users could be assigned the same token but specified using different cases.
        /// A workaround for this would be to use the overload that accepts both the user name and confirmation token.
        /// </remarks>
        public override bool ConfirmAccount(string accountConfirmationToken)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var query = from p in db.Query<ProfileEntity, ProfileEntityByAccountTokensIndex>()
                            where p.ConfirmationToken == accountConfirmationToken
                            select p;

                int count = query.Count();

                Debug.Assert(query.Count() < 2, "By virtue of the fact that the ConfirmationToken is random and unique, we can never have two tokens that are identical.");

                if (!query.Any())
                    return false;

                var profile = query.First();
                profile.IsConfirmed = true;

                db.SaveChanges();
                return true;
            }
        }

        // Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method
        public override string CreateAccount(string username, string password, bool requireConfirmationToken)
        {
            VerifyInitialized();

            if (string.IsNullOrWhiteSpace(password))
                throw new MembershipCreateUserException(MembershipCreateStatus.InvalidPassword);

            string hashedPassword = Crypto.HashPassword(password);
            if (hashedPassword.Length > 128)
                throw new MembershipCreateUserException(MembershipCreateStatus.InvalidPassword);

            if (string.IsNullOrWhiteSpace(username))
                throw new MembershipCreateUserException(MembershipCreateStatus.InvalidUserName);

            using (var db = ConnectToDatabase())
            {
                // Step 1: Check if the user exists in the Users table
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                {
                    // User not found
                    throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);
                }

                // Step 2: Check if the user exists in the Membership table: Error if yes.
                var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(username));
                if (profile != null)
                {
                    throw new MembershipCreateUserException(MembershipCreateStatus.DuplicateUserName);
                }

                // Step 3: Create user in Membership table
                string token = string.Empty;
                if (requireConfirmationToken)
                    token = GenerateToken();

                profile = new ProfileEntity
                {
                    Name = username,
                    Password = hashedPassword,
                    PasswordSalt = string.Empty,
                    IsConfirmed = !requireConfirmationToken,
                    ConfirmationToken = token,
                    CreateDate = DateTime.UtcNow,
                    PasswordChangedDate = DateTime.UtcNow,
                    PasswordFailuresSinceLastSuccess = 0,
                };

                db.Store(profile);
                db.SaveChanges();

                return token;
            }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, providerUserKey, out status);
            }
            throw new NotSupportedException();
        }

        // Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method
        public override string CreateUserAndAccount(string username, string password, bool requireConfirmation, IDictionary<string, object> values)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                CreateUser(db, username, values);
                return CreateAccount(username, password, requireConfirmation);
            }
        }

        private void CreateUser(IDocumentSession db, string username, IDictionary<string, object> values)
        {
            var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
            if (user != null)
            {
                // User not found
                throw new MembershipCreateUserException(MembershipCreateStatus.DuplicateUserName);
            }

            var userMapId = new UserMapIdEntity { Name = username };
            db.Store(userMapId);
            db.SaveChanges();

            user = new UserEntity { Name = username, ReverseId = userMapId.GetIdAsInteger() };
            db.Store(user);
            db.SaveChanges();
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override string GetPassword(string username, string answer)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.GetPassword(username, answer);
            }
            throw new NotSupportedException();
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            if (!InitializeCalled)
                return PreviousProvider.ChangePassword(username, oldPassword, newPassword);

            // REVIEW: are commas special in the password?
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "username");

            if (string.IsNullOrWhiteSpace(oldPassword))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "oldPassword");

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "newPassword");

            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                    return false;

                // First check that the old credentials match
                if (!CheckPassword(db, user, oldPassword))
                    return false;

                return SetPassword(db, user, newPassword);
            }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override string ResetPassword(string username, string answer)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.ResetPassword(username, answer);
            }
            throw new NotSupportedException();
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.GetUser(providerUserKey, userIsOnline);
            }
            throw new NotSupportedException();
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.GetUser(username, userIsOnline);
            }

            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                    return null;

                // TODO: Change this to fetch the profile and if it exists to set the proper values.
                return new MembershipUser(System.Web.Security.Membership.Provider.Name, username, user.ReverseId, null, null, null, true, false, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue);
            }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override string GetUserNameByEmail(string email)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.GetUserNameByEmail(email);
            }
            throw new NotSupportedException();
        }

        // Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method
        public override bool DeleteAccount(string username)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                    return false;

                var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(username));
                db.Delete(profile);
                db.SaveChanges();

                return true;
            }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.DeleteUser(username, deleteAllRelatedData);
            }

            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                    return false;

                var mapId = db.Load<UserMapIdEntity>(UserMapIdEntity.ToRavenId(user.ReverseId));
                if (mapId == null)
                    return false;

                db.Delete(user);
                db.Delete(mapId);

                db.SaveChanges();

                //if (deleteAllRelatedData) {
                // REVIEW: do we really want to delete from the user table?
                //}

                return true;
            }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.GetAllUsers(pageIndex, pageSize, out totalRecords);
            }
            throw new NotSupportedException();
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override int GetNumberOfUsersOnline()
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.GetNumberOfUsersOnline();
            }
            throw new NotSupportedException();
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
            }
            throw new NotSupportedException();
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
            }
            throw new NotSupportedException();
        }

        // Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method
        public override int GetPasswordFailuresSinceLastSuccess(string username)
        {
            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoUserFound, username));
                }

                var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(user.Name));
                if (profile == null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoAccountFound, username));
                }

                return profile.PasswordFailuresSinceLastSuccess;
            }
        }

        // Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method
        public override DateTime GetCreateDate(string username)
        {
            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoUserFound, username));
                }

                var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(username));
                if (profile == null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoAccountFound, username));
                }

                return profile.CreateDate.HasValue ? profile.CreateDate.Value : DateTime.MinValue;
            }
        }

        // Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method
        public override DateTime GetPasswordChangedDate(string username)
        {
            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoUserFound, username));
                }

                var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(username));
                if (profile == null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoAccountFound, username));
                }

                return profile.PasswordChangedDate;
            }
        }

        // Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method
        public override DateTime GetLastPasswordFailureDate(string username)
        {
            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoUserFound, username));
                }

                var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(username));
                if (profile == null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoAccountFound, username));
                }

                return profile.LastPasswordFailureDate.HasValue ? profile.LastPasswordFailureDate.Value : DateTime.MinValue;
            }
        }

        // Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method
        public override string GeneratePasswordResetToken(string username, int tokenExpirationInMinutesFromNow)
        {
            VerifyInitialized();
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "userName");

            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoUserFound, username));

                var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(username));
                if (profile == null)
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoAccountFound, username));

                if (!profile.IsConfirmed)
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoUserFound, username));

                string token = profile.PasswordVerificationToken;
                if (string.IsNullOrWhiteSpace(token) || profile.PasswordVerificationTokenExpirationDate < DateTime.UtcNow)
                {
                    token = GenerateToken();

                    profile.PasswordVerificationToken = token;
                    profile.PasswordVerificationTokenExpirationDate = DateTime.UtcNow.AddMinutes(tokenExpirationInMinutesFromNow);

                    db.SaveChanges();
                }

                return token;
            }
        }

        // Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method
        public override bool IsConfirmed(string username)
        {
            VerifyInitialized();
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "userName");

            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                    return false;

                var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(username));
                if (profile == null)
                    return false;

                return profile.IsConfirmed;
            }
        }

        // Inherited from ExtendedMembershipProvider ==> Simple Membership MUST be enabled to use this method
        public override bool ResetPasswordWithToken(string token, string newPassword)
        {
            VerifyInitialized();
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "newPassword");
            }

            using (var db = ConnectToDatabase())
            {
                var query = from profile in db.Query<ProfileEntity, ProfileEntityByAccountTokensIndex>()
                            where profile.PasswordVerificationToken == token && profile.PasswordVerificationTokenExpirationDate >= DateTime.UtcNow
                            select profile;

                if (query.Any())
                {
                    var profile = query.First();

                    bool success = SetPassword(db, profile, newPassword);
                    if (success)
                    {
                        profile.PasswordVerificationToken = string.Empty;
                        profile.PasswordVerificationTokenExpirationDate = DateTime.UtcNow;

                        db.SaveChanges();

                        return true;
                    }
                }

                return false;
            }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override void UpdateUser(MembershipUser user)
        {
            if (!InitializeCalled)
            {
                PreviousProvider.UpdateUser(user);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool UnlockUser(string userName)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.UnlockUser(userName);
            }
            throw new NotSupportedException();
        }

        // Inherited from MembershipProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool ValidateUser(string username, string password)
        {
            if (!InitializeCalled)
            {
                return PreviousProvider.ValidateUser(username, password);
            }

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "username");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "password");

            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                    return false;

                var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(username));
                if (profile == null)
                    return false;

                if (profile.IsConfirmed)
                {
                    return CheckPassword(db, user, password);
                }
                else
                {
                    return false;
                }
            }
        }

        public override string GetUserNameFromId(int userId)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var map = db.Load<UserMapIdEntity>(UserMapIdEntity.ToRavenId(userId));
                return map.Name;
            }
        }

        public override void CreateOrUpdateOAuthAccount(string provider, string providerUserId, string username)
        {
            VerifyInitialized();

            if (string.IsNullOrWhiteSpace(username))
                throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);

            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                    throw new MembershipCreateUserException(MembershipCreateStatus.InvalidUserName);

                var oauthProfile = db.Load<OAuthProfileEntity>(OAuthProfileEntity.ToRavenId(provider, providerUserId));
                if (oauthProfile == null)
                {
                    // account doesn't exist. create a new one.
                    oauthProfile = new OAuthProfileEntity(provider, providerUserId) { UserId = user.ToRavenId() };
                    db.Store(oauthProfile);
                }
                else
                {
                    // account already exist. update it
                    oauthProfile.UserId = user.ToRavenId();
                }

                db.SaveChanges();
            }
        }

        public override void DeleteOAuthAccount(string provider, string providerUserId)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var oauthProfile = db.Load<OAuthProfileEntity>(OAuthProfileEntity.ToRavenId(provider, providerUserId));
                if (oauthProfile == null)
                    throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);

                db.Delete(oauthProfile);
                db.SaveChanges();
            }
        }

        public override int GetUserIdFromOAuth(string provider, string providerUserId)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var oauthProfileEntity = db.Load<OAuthProfileEntity>(OAuthProfileEntity.ToRavenId(provider, providerUserId));
                if (oauthProfileEntity == null)
                    return -1;

                var user = db.Load<UserEntity>(oauthProfileEntity.UserId);
                if (user == null)
                    throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);

                return user.ReverseId;
            }
        }

        public string GetOAuthTokenSecret(string token)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var tokenEntity = db.Load<TokenEntity>(TokenEntity.ToRavenId(token));
                if (tokenEntity == null)
                    throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);

                return tokenEntity.Secret;
            }
        }

        public void StoreOAuthRequestToken(string requestToken, string requestTokenSecret)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var tokenEntity = db.Load<TokenEntity>(TokenEntity.ToRavenId(requestToken));
                if (tokenEntity != null)
                {
                    if (tokenEntity.Secret == requestTokenSecret)
                    {
                        // the record already exists
                        return;
                    }

                    tokenEntity.Secret = requestTokenSecret;
                }
                else
                {
                    tokenEntity = new TokenEntity(requestToken, requestTokenSecret);
                    db.Store(tokenEntity);
                }

                db.SaveChanges();
            }
        }

        /// <summary>
        /// Replaces the request token with access token and secret.
        /// </summary>
        /// <param name="requestToken">The request token.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="accessTokenSecret">The access token secret.</param>
        public void ReplaceOAuthRequestTokenWithAccessToken(string requestToken, string accessToken, string accessTokenSecret)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var tokenEntity = db.Load<TokenEntity>(TokenEntity.ToRavenId(requestToken));
                if (tokenEntity == null)
                    throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);

                db.Delete(tokenEntity);
                db.SaveChanges();

                StoreOAuthRequestToken(accessToken, accessTokenSecret);
            }
        }

        /// <summary>
        /// Deletes the OAuth token from the backing store from the database.
        /// </summary>
        /// <param name="token">The token to be deleted.</param>
        public void DeleteOAuthToken(string token)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var tokenEntity = db.Load<TokenEntity>(TokenEntity.ToRavenId(token));
                if (tokenEntity == null)
                    throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);

                db.Delete(tokenEntity);
                db.SaveChanges();
            }
        }

        public override ICollection<OAuthAccount> GetAccountsForUser(string username)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var user = db.Load<UserEntity>(UserEntity.ToRavenId(username));
                if (user == null)
                    return new OAuthAccount[0];

                var query = from p in db.Query<OAuthProfileEntity, OAuthProfileEntityByTokensIndex>()
                            where p.UserId == UserEntity.ToRavenId(username)
                            select p;

                var accounts = new List<OAuthAccount>();
                foreach (var profile in query)
                    accounts.Add(new OAuthAccount(profile.Provider, profile.ProviderUserId));

                return accounts;
            }
        }

        /// <summary>
        /// Determines whether there exists a local account (as opposed to OAuth account) with the specified userId.
        /// </summary>
        /// <param name="userId">The user id to check for local account.</param>
        /// <returns>
        ///   <c>true</c> if there is a local account with the specified user id]; otherwise, <c>false</c>.
        /// </returns>
        public override bool HasLocalAccount(int userId)
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                var map = db.Load<UserMapIdEntity>(UserMapIdEntity.ToRavenId(userId));
                if (map == null)
                    return false;

                var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(map.Name));
                return profile != null;
            }
        }

        public override string GenerateAccessToken(string username, TimeSpan expiration)
        {
            VerifyInitialized();

            var expiry = DateTime.UtcNow.Add(expiration);
            using (var db = ConnectToDatabase())
            {
                var accessToken = new AccessTokenEntity(username, GenerateToken());

                db.Store(accessToken);
                db.Advanced.GetMetadataFor(accessToken)["Raven-Expiration-Date"] = new RavenJValue(expiry);
                db.SaveChanges();

                return accessToken.Token;
            }
        }

        public override bool VerifyAccessToken(string username, string token)
        {
            VerifyInitialized();

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("username cannot be null, empty or whitespace.", username);

            if (string.IsNullOrWhiteSpace(token))
                return false;

            using (var db = ConnectToDatabase())
            {
                var tokenEntity = db.Load<AccessTokenEntity>(AccessTokenEntity.ToRavenId(token));
                if (tokenEntity == null)
                    return false;

                if (tokenEntity.User != username)
                    return false;

                return true;
            }
        }

        private bool SetPassword(IDocumentSession db, ProfileEntity profile, string newPassword)
        {
            string hashedPassword = Crypto.HashPassword(newPassword);
            if (hashedPassword.Length > 128)
                throw new ArgumentException(Resources.SimpleMembership_PasswordTooLong);

            try
            {
                profile.Password = hashedPassword;
                profile.PasswordChangedDate = DateTime.UtcNow;

                db.SaveChanges();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool SetPassword(IDocumentSession db, UserEntity user, string newPassword)
        {
            var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(user.Name));
            if (profile == null)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoAccountFound, user.Name));

            return SetPassword(db, profile, newPassword);
        }

        private bool CheckPassword(IDocumentSession db, UserEntity user, string password)
        {
            var profile = db.Load<ProfileEntity>(ProfileEntity.ToRavenId(user.Name));
            if (profile == null)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Security_NoAccountFound, user.Name));

            string hashedPassword = profile.Password;
            bool verificationSucceeded = (hashedPassword != null && Crypto.VerifyHashedPassword(hashedPassword, password));
            if (verificationSucceeded)
            {
                // Reset password failure count on successful credential check
                profile.PasswordFailuresSinceLastSuccess = 0;
            }
            else
            {
                int failures = profile.PasswordFailuresSinceLastSuccess;
                if (failures != -1)
                {
                    profile.PasswordFailuresSinceLastSuccess++;
                    profile.LastPasswordFailureDate = DateTime.UtcNow;
                }
            }

            db.SaveChanges();

            return verificationSucceeded;
        }

        #endregion Operations

        internal bool InitializeCalled { get; set; }

        internal override void VerifyInitialized()
        {
            if (!Initializer.Initialized || !InitializeCalled)
            {
                throw new InvalidOperationException(Resources.Security_InitializeMustBeCalledFirst);
            }
        }

        private static string GenerateToken()
        {
            using (var prng = new RNGCryptoServiceProvider())
            {
                return GenerateToken(prng);
            }
        }

        internal static string GenerateToken(RandomNumberGenerator generator)
        {
            byte[] tokenBytes = new byte[TokenSizeInBytes];
            generator.GetBytes(tokenBytes);

            return HttpServerUtility.UrlTokenEncode(tokenBytes);
        }

        internal virtual IDocumentSession ConnectToDatabase()
        {
            return Initializer.DocumentStore.OpenSession();
        }
    }
}
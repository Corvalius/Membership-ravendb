using Raven.Client;
using Raven.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Corvalius.Membership.Raven.Test
{
    public class RoleProviderTest : RavenTestBase
    {
        private SimpleMembershipProvider CreateDefaultSimpleMembershipProvider(IDocumentStore store)
        {
            Initializer.InitializeDatabaseConnection(store);

            var simpleMembershipProvider = new SimpleMembershipProvider();
            NameValueCollection config = new NameValueCollection();
            simpleMembershipProvider.Initialize("AspNetSqlMembershipProvider", config);

            return simpleMembershipProvider;
        }

        private SimpleRoleProvider CreateDefaultSimpleRoleProvider(IDocumentStore store)
        {
            Initializer.InitializeDatabaseConnection(store);

            var simpleRoleProvider = new SimpleRoleProvider();
            NameValueCollection config = new NameValueCollection();
            simpleRoleProvider.Initialize("AspNetSqlRoleProvider", config);

            return simpleRoleProvider;
        }

        [Fact]
        public void CreateAdminRole()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleRoleProvider = CreateDefaultSimpleRoleProvider(store);

                // Act
                simpleRoleProvider.CreateRole("Admin");

                WaitForIndexing(store);

                var roles = simpleRoleProvider.GetAllRoles();

                // Assert
                Assert.Equal(1, roles.Count());
                Assert.Equal("Admin", roles[0]);
            }
        }

        [Fact]
        public void FailedToCreateRoles()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleRoleProvider = CreateDefaultSimpleRoleProvider(store);

                // Act
                Assert.Throws<ArgumentException>(() => simpleRoleProvider.CreateRole(null));
                Assert.Throws<ArgumentException>(() => simpleRoleProvider.CreateRole(string.Empty));
                Assert.Throws<ArgumentException>(() => simpleRoleProvider.CreateRole("  "));

                simpleRoleProvider.CreateRole("Admin");
                Assert.Throws<InvalidOperationException>(() => simpleRoleProvider.CreateRole("Admin"));

                // Assert
                WaitForIndexing(store);
                
                var roles = simpleRoleProvider.GetAllRoles();
                Assert.Equal(1, roles.Count());
                Assert.Equal("Admin", roles[0]);
            }
        }

        [Fact]
        public void EnsureRolesExists()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleRoleProvider = CreateDefaultSimpleRoleProvider(store);

                // Act
                simpleRoleProvider.CreateRole("Test1");
                simpleRoleProvider.CreateRole("Test2");
                simpleRoleProvider.CreateRole("Test3");

                // Assert
                Assert.True(simpleRoleProvider.RoleExists("Test1"));
                Assert.True(simpleRoleProvider.RoleExists("Test2"));
                Assert.True(simpleRoleProvider.RoleExists("Test3"));
                Assert.False(simpleRoleProvider.RoleExists("as"));

                Assert.Throws<ArgumentException>(() => simpleRoleProvider.RoleExists(null));
                Assert.Throws<ArgumentException>(() => simpleRoleProvider.RoleExists(string.Empty));
                Assert.Throws<ArgumentException>(() => simpleRoleProvider.RoleExists("  "));
            }
        }

        [Fact]
        public void DeleteRoles()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleRoleProvider = CreateDefaultSimpleRoleProvider(store);

                // Act
                simpleRoleProvider.CreateRole("Test1");
                simpleRoleProvider.CreateRole("Test2");
                simpleRoleProvider.CreateRole("Test3");

                Assert.True(simpleRoleProvider.DeleteRole("Test1", true));
                Assert.True(simpleRoleProvider.DeleteRole("Test2", true));
                Assert.True(simpleRoleProvider.DeleteRole("Test3", true));

                // Assert

                Assert.False(simpleRoleProvider.RoleExists("Test1"));
                Assert.False(simpleRoleProvider.RoleExists("Test2"));
                Assert.False(simpleRoleProvider.RoleExists("Test3"));
            }
        }

        [Fact]
        public void UsersInRoles()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleRoleProvider = CreateDefaultSimpleRoleProvider(store);
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                string user1 = "testuser1";
                string user2 = "user2";
                string user3 = "testuser3";

                // Act
                simpleRoleProvider.CreateRole("Test1");
                simpleRoleProvider.CreateRole("Test2");
                simpleRoleProvider.CreateRole("Test3");
                simpleRoleProvider.CreateRole("Nobody");

                simpleMembershipProvider.CreateUserAndAccount(user1, "redwood12", false);
                simpleMembershipProvider.CreateUserAndAccount(user2, "redwood12", false);
                simpleMembershipProvider.CreateUserAndAccount(user3, "redwood12", false);

                WaitForIndexing(store);

                simpleRoleProvider.AddUsersToRoles(new[] { user1 }, new[] { "Test1", "Test2" });
                simpleRoleProvider.AddUsersToRoles(new[] { user2 }, new[] { "Test3", "Test2" });

                WaitForIndexing(store);

                // Assert
                Assert.True(simpleRoleProvider.IsUserInRole(user1, "Test1"));
                Assert.True(simpleRoleProvider.IsUserInRole(user1, "Test2"));
                Assert.False(simpleRoleProvider.IsUserInRole(user1, "Test3"));
                Assert.False(simpleRoleProvider.IsUserInRole(user1, "Nobody"));

                Assert.False(simpleRoleProvider.IsUserInRole(user2, "Test1"));
                Assert.True(simpleRoleProvider.IsUserInRole(user2, "Test2"));
                Assert.True(simpleRoleProvider.IsUserInRole(user2, "Test3"));
                Assert.False(simpleRoleProvider.IsUserInRole(user2, "Nobody"));

                Assert.Equal(1, simpleRoleProvider.GetUsersInRole("Test1").Count());
                Assert.Equal(2, simpleRoleProvider.GetUsersInRole("Test2").Count());
                Assert.Equal(1, simpleRoleProvider.GetUsersInRole("Test3").Count());
                Assert.Equal(0, simpleRoleProvider.GetUsersInRole("Nobody").Count());

                Assert.Equal(2, simpleRoleProvider.GetRolesForUser(user1).Count());
                Assert.Equal(2, simpleRoleProvider.GetRolesForUser(user2).Count());
                Assert.Equal(0, simpleRoleProvider.GetRolesForUser(user3).Count());

                Assert.Equal(1, simpleRoleProvider.FindUsersInRole("Test2", "*test*").Count());
                Assert.Equal(2, simpleRoleProvider.FindUsersInRole("Test2", "*user*").Count());
            }
        }

        [Fact]
        public void DeleteUsersFromRoles()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange
                var simpleRoleProvider = CreateDefaultSimpleRoleProvider(store);
                var simpleMembershipProvider = CreateDefaultSimpleMembershipProvider(store);

                string user1 = "testuser1";
                string user2 = "user2";

                // Act
                simpleRoleProvider.CreateRole("Test1");
                simpleRoleProvider.CreateRole("Test2");
                simpleRoleProvider.CreateRole("Test3");
                simpleRoleProvider.CreateRole("Nobody");

                simpleMembershipProvider.CreateUserAndAccount(user1, "redwood12", false);
                simpleMembershipProvider.CreateUserAndAccount(user2, "redwood12", false);

                WaitForIndexing(store);

                simpleRoleProvider.AddUsersToRoles(new[] { user1, user2 }, new[] { "Test1", "Test2" });
                simpleRoleProvider.AddUsersToRoles(new[] { user2 }, new[] { "Test3" });

                WaitForIndexing(store);

                Assert.Throws<InvalidOperationException>(() => simpleRoleProvider.RemoveUsersFromRoles(new[] { user1, user2 }, new[] { "Test1", "Nobody" }));
                Assert.Throws<InvalidOperationException>(() => simpleRoleProvider.RemoveUsersFromRoles(new[] { user1, user2 }, new[] { "Test3" }));

                simpleRoleProvider.RemoveUsersFromRoles(new[] { user1, user2 }, new[] { "Test1" });
                simpleRoleProvider.RemoveUsersFromRoles(new[] { user2 }, new[] { "Test3" });

                // Assert

                Assert.False(simpleRoleProvider.IsUserInRole(user1, "Test1"));
                Assert.True(simpleRoleProvider.IsUserInRole(user1, "Test2"));
                Assert.False(simpleRoleProvider.IsUserInRole(user1, "Test3"));

                Assert.False(simpleRoleProvider.IsUserInRole(user2, "Test1"));
                Assert.True(simpleRoleProvider.IsUserInRole(user2, "Test2"));
                Assert.False(simpleRoleProvider.IsUserInRole(user2, "Test3"));
            }
        }
    }
}

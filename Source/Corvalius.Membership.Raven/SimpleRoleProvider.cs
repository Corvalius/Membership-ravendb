using Raven.Client;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;

namespace Corvalius.Membership.Raven
{
    public class SimpleRoleProvider : RoleProvider
    {
        public static readonly string EnableRavenDbSimpleRolesKey = "enableRavenDbSimpleRoles";
     
        private RoleProvider _previousProvider;

        public SimpleRoleProvider()
            : this(null)
        {
        }

        public SimpleRoleProvider(RoleProvider previousProvider)
        {
            _previousProvider = previousProvider;
        }

        private RoleProvider PreviousProvider
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
                name = "RavenSimpleRoleProvider";

            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "A RavenDB Extended Role Provider.");
            }

            base.Initialize(name, config);

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

        internal bool InitializeCalled { get; set; }

        // Inherited from RoleProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override string ApplicationName
        {
            get
            {
                VerifyInitialized();
                throw new NotSupportedException();
            }
            set
            {
                VerifyInitialized();
                throw new NotSupportedException();
            }
        }

        private void VerifyInitialized()
        {
            if (!InitializeCalled)
            {
                throw new InvalidOperationException(Resources.Security_InitializeMustBeCalledFirst);
            }
        }

        internal virtual IDocumentSession ConnectToDatabase()
        {
            return Initializer.DocumentStore.OpenSession();
        }

        // Inherited from RoleProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            VerifyInitialized();

            if (usernames.Any(x => string.IsNullOrWhiteSpace(x)))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "usernames");

            if (roleNames.Any(x => string.IsNullOrWhiteSpace(x)))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "roleNames");

            using (var db = ConnectToDatabase())
            {
                var query = from u in usernames
                            from r in roleNames
                            select new { Id = UserRoleRef.ToRavenId(u, r), Username = u, Role = r };

                var role = db.Load<RoleEntity>(query.Select(x => RoleEntity.ToRavenId(x.Role)));
                if (role.Any(x => x == null))
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.SimpleRoleProvider_NoRoleFound));

                // Prefetch all data from the database.
                var rolesToAdd = db.Load<UserRoleRef>(query.Select(x => x.Id))
                                   .Where(x => x != null);

                if (rolesToAdd.Any())
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.SimpleRoleProvder_UserAlreadyInRole));

                foreach (var item in query)
                {
                    db.Store(new UserRoleRef (item.Role, item.Username));
                }

                db.SaveChanges();
            }
        }

        // Inherited from RoleProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override void CreateRole(string roleName)
        {
            VerifyInitialized();

            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "roleName");

            using (var db = ConnectToDatabase())
            {
                var role = db.Load<RoleEntity>(RoleEntity.ToRavenId(roleName));
                if (role != null)
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, Resources.SimpleRoleProvider_RoleExists, roleName));

                db.Store(new RoleEntity() { Name = roleName });
                db.SaveChanges();
            }
        }

        // Inherited from RoleProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            VerifyInitialized();

            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "roleName");

            using (var db = ConnectToDatabase())
            {
                var role = db.Load<RoleEntity>(RoleEntity.ToRavenId(roleName));
                if (role == null)
                    return false;

                if (throwOnPopulatedRole)
                {
                    var query = db.Query<UserRoleRef, RolesByUsersIndex>()
                                  .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite(TimeSpan.FromSeconds(20)))
                                  .Where(x => x.Role == roleName);

                    if ( query.Any() )
                        throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, Resources.SimpleRoleProvder_RolePopulated, roleName));
                }
                else
                {
                    var query = db.Query<UserRoleRef, RolesByUsersIndex>()
                                  .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite(TimeSpan.FromSeconds(20)))
                                  .GetStreamed(db)
                                  .Where(x => x.Role == roleName);

                    foreach (var userRole in query)
                        db.Delete(userRole);
                }

                db.Delete(role);
                db.SaveChanges();

                return true;
            }

        }

        // Inherited from RoleProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            VerifyInitialized();

            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "roleName");

            using (var db = ConnectToDatabase())
            {
                var query = db.Query<UserRoleRef, RolesByUsersIndex>()
                                .Where(x => x.Role == roleName)
                                .Search(x => x.Username, usernameToMatch, 1, SearchOptions.And, EscapeQueryOptions.AllowAllWildcards)
                                .Select( x => x.Username );

                return query.ToArray();
            }
        }

        // Inherited from RoleProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override string[] GetAllRoles()
        {
            VerifyInitialized();

            using (var db = ConnectToDatabase())
            {
                return  db.Query<RoleEntity, RolesIndex>()
                          .GetStreamed(db)
                          .Select ( x => x.Name )
                          .ToArray();
            }
        }

        // Inherited from RoleProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override string[] GetRolesForUser(string username)
        {
            VerifyInitialized();

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "username");

            using (var db = ConnectToDatabase())
            {
                var query = from user in db.Query<UserRoleRef, RolesByUsersIndex>()
                            where user.Username == username
                            select user.Role;

                return query.ToArray();         
            }
        }

        // Inherited from RoleProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override string[] GetUsersInRole(string roleName)
        {
            VerifyInitialized();

            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "roleName");

            using (var db = ConnectToDatabase())
            {
                var query = from user in db.Query<UserRoleRef, RolesByUsersIndex>()
                            where user.Role == roleName
                            select user.Username;

                return query.ToArray();            
            }
        }

        // Inherited from RoleProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool IsUserInRole(string username, string roleName)
        {
            VerifyInitialized();

            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "roleName");

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "username");

            using (var db = ConnectToDatabase())
            {
                var roleRef = db.Load<UserRoleRef>(UserRoleRef.ToRavenId(username, roleName));
                return roleRef != null;
            }
        }

        // Inherited from RoleProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            VerifyInitialized();

            if (usernames.Any(x => string.IsNullOrWhiteSpace(x)))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "usernames");

            if (roleNames.Any(x => string.IsNullOrWhiteSpace(x)))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "roleNames");

            using (var db = ConnectToDatabase())
            {
                var query = from u in usernames
                            from r in roleNames
                            select new { Id = UserRoleRef.ToRavenId(u, r), Username = u, Role = r };

                var roles = db.Load<RoleEntity>(query.Select(x => RoleEntity.ToRavenId(x.Role)));
                if (roles.Any(x => x == null))
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.SimpleRoleProvider_NoRoleFound));

                // Prefetch all data from the database.
                var rolesToRemove = db.Load<UserRoleRef>(query.Select( x => x.Id));

                // Ensure constraints apply.
                foreach (string rolename in roleNames)
                {
                    var role = db.Load<RoleEntity>(RoleEntity.ToRavenId(rolename));
                    if (role == null)
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.SimpleRoleProvider_NoRoleFound, rolename));
                }
               
                foreach (string username in usernames)
                {
                    foreach (string rolename in roleNames)
                    {
                        var roleRef = db.Load<UserRoleRef>(UserRoleRef.ToRavenId(username, rolename));
                        if (roleRef == null)
                            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.SimpleRoleProvder_UserNotInRole, username, rolename));
                    }
                }

                // Remove the roles for the selected users.
                foreach (var role in rolesToRemove.Where(x => x != null))
                    db.Delete(role);

                db.SaveChanges();
            }
        }

        // Inherited from RoleProvider ==> Forwarded to previous provider if this provider hasn't been initialized
        public override bool RoleExists(string roleName)
        {
            VerifyInitialized();

            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException(Resources.Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace, "roleName");

            using (var db = ConnectToDatabase())
            {
                var role = db.Load<RoleEntity>(RoleEntity.ToRavenId(roleName));
                
                return role != null;
            }
        }
    }
}

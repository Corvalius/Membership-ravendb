using Raven.Abstractions.Util;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Client.Listeners;
using Raven.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition.Hosting;
using System.Configuration.Provider;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Security;

namespace Corvalius.Membership.Raven
{
    public static class Initializer
    {
        internal static IDocumentStore DocumentStore { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="M:InitializeDatabaseConnection"/> method has been initialized.
        /// </summary>
        /// <value>
        ///   <c>true</c> if initialized; otherwise, <c>false</c>.
        /// </value>
        public static bool Initialized { get; private set; }

        private static volatile bool preAppStartInitialized = false;

        internal static void PreAppStartInit()
        {
            if (Configuration.WebMatrixSimpleMembershipEnabled)
                throw new InvalidOperationException(Resources.SimpleMembership_CannotCoexistsWithWebMatrixProvider);

            // Allow use of <add key="EnableRavenDbSimpleMembershipKey" value="false" /> to disable registration of membership/role providers.
            if (Configuration.SimpleMembershipEnabled)
            {
                bool isRunningOutsideOfWebApplication = InspectCollectionReadOnlyness(System.Web.Security.Membership.Providers);
                if (isRunningOutsideOfWebApplication)
                {
                    if (!(System.Web.Security.Membership.Provider is SimpleMembershipProvider))
                        throw new InvalidOperationException("Default Membership providers cannot be setup programatically when running outside of ASP.Net. Setup the default on the app.config.");
                }
                else
                {
                    // called during PreAppStart, should also hook up the config for MembershipProviders?
                    // Replace the AspNetSqlMembershipProvider (which is the default that is registered in root web.config)
                    const string BuiltInMembershipProviderName = "AspNetSqlMembershipProvider";

                    var builtInMembership = System.Web.Security.Membership.Providers[BuiltInMembershipProviderName];
                    if (builtInMembership != null)
                    {
                        var simpleMembership = CreateDefaultSimpleMembershipProvider(BuiltInMembershipProviderName, currentDefault: builtInMembership);
                        System.Web.Security.Membership.Providers.Remove(BuiltInMembershipProviderName);
                        System.Web.Security.Membership.Providers.Add(simpleMembership);
                    }
                }

                // Allow use of <add key="EnableRavenDbSimpleRolesKey" value="false" /> to disable registration of role provider.
                if (Configuration.SimpleRolesEnabled)
                {
                    if (isRunningOutsideOfWebApplication)
                    {
                        if (!Roles.Enabled)
                            throw new InvalidOperationException("Roles cannot be enabled programatically when running outside of ASP.Net. Enable the roles manager from the app.config.");

                        if (!(Roles.Provider is SimpleRoleProvider))
                            throw new InvalidOperationException("Default roles providers cannot be setup programatically when running outside of ASP.Net. Setup the default on the app.config.");
                    }
                    else
                    {
                        Roles.Enabled = true;

                        const string BuiltInRolesProviderName = "AspNetSqlRoleProvider";
                        var builtInRoles = Roles.Providers[BuiltInRolesProviderName];
                        if (builtInRoles != null)
                        {
                            var simpleRoles = CreateDefaultSimpleRoleProvider(BuiltInRolesProviderName, currentDefault: builtInRoles);
                            Roles.Providers.Remove(BuiltInRolesProviderName);
                            Roles.Providers.Add(simpleRoles);
                        }
                    }
                }
                else
                {
                    if (Roles.Enabled)
                        Roles.Enabled = false;
                }
            }

            preAppStartInitialized = true;
        }

        private static SimpleRoleProvider CreateDefaultSimpleRoleProvider(string name, RoleProvider currentDefault)
        {
            var provider = new SimpleRoleProvider(previousProvider: currentDefault);
            NameValueCollection config = new NameValueCollection();
            provider.Initialize(name, config);

            return provider;
        }

        private static SimpleMembershipProvider CreateDefaultSimpleMembershipProvider(string name, MembershipProvider currentDefault)
        {
            var membership = new SimpleMembershipProvider(previousProvider: currentDefault);
            NameValueCollection config = new NameValueCollection();
            membership.Initialize(name, config);

            return membership;
        }

        private static bool InspectCollectionReadOnlyness(ProviderCollection collection)
        {
            try
            {
                // turn off read-only on Membership Providers collection
                FieldInfo field = typeof(ProviderCollection).GetField("_ReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
                return (bool)field.GetValue(collection);
            }
            catch
            {
                return false;
            }
        }

        public static void InitializeDatabaseConnection(IDocumentStore documentStore)
        {
            if ( !preAppStartInitialized )
                PreAppStartInit();                        

            var store = documentStore;

            store.Conventions.RegisterIdConvention<UserEntity>((dbName, db, user) => UserEntity.IdPrefix + EncodeNonAsciiCharacters(user.Name));
            store.Conventions.RegisterAsyncIdConvention<UserEntity>((dbName, db, user) => new CompletedTask<string>(UserEntity.IdPrefix + EncodeNonAsciiCharacters(user.Name)));

            store.Conventions.RegisterIdConvention<TokenEntity>((dbName, db, token) => token.ToRavenId());
            store.Conventions.RegisterAsyncIdConvention<TokenEntity>((dbName, db, token) => new CompletedTask<string>(token.ToRavenId()));

            store.Conventions.RegisterIdConvention<AccessTokenEntity>((dbName, db, token) => token.ToRavenId());
            store.Conventions.RegisterAsyncIdConvention<AccessTokenEntity>((dbName, db, token) => new CompletedTask<string>(token.ToRavenId()));

            store.Conventions.RegisterIdConvention<ProfileEntity>((dbName, db, profile) => ProfileEntity.IdPrefix + EncodeNonAsciiCharacters(profile.Name));
            store.Conventions.RegisterAsyncIdConvention<ProfileEntity>((dbName, db, profile) => new CompletedTask<string>(ProfileEntity.IdPrefix + EncodeNonAsciiCharacters(profile.Name)));

            store.Conventions.RegisterIdConvention<OAuthProfileEntity>((dbName, db, profile) => profile.ToRavenId());
            store.Conventions.RegisterAsyncIdConvention<OAuthProfileEntity>((dbName, db, profile) => new CompletedTask<string>(profile.ToRavenId()));

            store.Conventions.RegisterIdConvention<RoleEntity>((dbName, db, role) => RoleEntity.IdPrefix + EncodeNonAsciiCharacters(role.Name));
            store.Conventions.RegisterAsyncIdConvention<RoleEntity>((dbName, db, role) => new CompletedTask<string>(UserEntity.IdPrefix + EncodeNonAsciiCharacters(role.Name)));

            store.Conventions.RegisterIdConvention<UserRoleRef>((dbName, db, role) => role.ToRavenId());
            store.Conventions.RegisterAsyncIdConvention<UserRoleRef>((dbName, db, role) => new CompletedTask<string>(role.ToRavenId()));

            var catalog = new CompositionContainer(new AssemblyCatalog(typeof(Initializer).Assembly));
            IndexCreation.CreateIndexes(catalog, store);

            DocumentStore = store;
            Initialized = true;
        }

        public static void InitializeDatabaseConnection(string connectionString, string databaseName)
        {
            var store = new DocumentStore();
            store.ParseConnectionString(connectionString);
            store.DefaultDatabase = databaseName;
            store.Initialize();

            InitializeDatabaseConnection(store);
        }

        internal static string EncodeNonAsciiCharacters(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // This character is too big for ASCII
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        internal static string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m => { return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString(); });
        }
    }
}
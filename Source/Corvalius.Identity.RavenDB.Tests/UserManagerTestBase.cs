using Corvalius.Identity.DependencyInjection;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client;
using Raven.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Raven.Database.Config;

namespace Corvalius.Identity.RavenDB.Tests
{
    // Common functionality tests that all verifies user manager functionality regardless of store implementation
    public abstract class UserManagerTestBase<TUser, TRole> : UserManagerTestBase<TUser, TRole, string>
        where TUser : class
        where TRole : class
    { }

    public abstract class UserManagerTestBase<TUser, TRole, TKey> : RavenTestBase
    where TUser : class
    where TRole : class
    where TKey : IEquatable<TKey>
    {
        private readonly IdentityErrorDescriber _errorDescriber = new IdentityErrorDescriber();

        protected IDocumentStore Store;

        public UserManagerTestBase()
        {
            Store = this.NewDocumentStore();
        }

        protected override void ModifyConfiguration(InMemoryRavenConfiguration configuration)
        {
            configuration.Storage.Voron.AllowOn32Bits = true;

            base.ModifyConfiguration(configuration);
        }

        protected virtual bool ShouldSkipDbTests()
        {
            return false;
        }

        protected virtual void SetupIdentityServices(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddIdentity<TUser, TRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonLetterOrDigit = false;
                options.Password.RequireUppercase = false;
                options.User.AllowedUserNameCharacters = null;
            }).AddDefaultTokenProviders();

            AddUserStore(services, context);
            AddRoleStore(services, context);

            services.AddLogging();

            services.AddSingleton<ILogger<UserManager<TUser>>>(x => new TestLogger<UserManager<TUser>>());
            services.AddSingleton<ILogger<RoleManager<TRole>>>(x => new TestLogger<RoleManager<TRole>>());
        }

        protected virtual UserManager<TUser> CreateManager(object context = null, IServiceCollection services = null, Action<IServiceCollection> configureServices = null)
        {
            if (services == null)
            {
                services = new ServiceCollection();
            }
            if (context == null)
            {
                context = CreateTestContext();
            }
            SetupIdentityServices(services, context);
            if (configureServices != null)
            {
                configureServices(services);
            }
            return services.BuildServiceProvider().GetService<UserManager<TUser>>();
        }

        protected RoleManager<TRole> CreateRoleManager(object context = null, IServiceCollection services = null)
        {
            if (services == null)
            {
                services = new ServiceCollection();
            }
            if (context == null)
            {
                context = CreateTestContext();
            }
            SetupIdentityServices(services, context);
            return services.BuildServiceProvider().GetService<RoleManager<TRole>>();
        }

        protected abstract object CreateTestContext();

        protected abstract void AddUserStore(IServiceCollection services, object context = null);
        protected abstract void AddRoleStore(IServiceCollection services, object context = null);

        protected abstract void SetUserPasswordHash(TUser user, string hashedPassword);

        protected abstract TUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "", bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = null, bool useNamePrefixAsUserName = false);

        protected abstract TRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false);

        protected abstract Expression<Func<TUser, bool>> UserNameEqualsPredicate(string userName);
        protected abstract Expression<Func<TUser, bool>> UserNameStartsWithPredicate(string userName);

        protected abstract Expression<Func<TRole, bool>> RoleNameEqualsPredicate(string roleName);
        protected abstract Expression<Func<TRole, bool>> RoleNameStartsWithPredicate(string roleName);
    }
}

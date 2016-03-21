using Microsoft.AspNet.Builder.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Corvalius.Identity.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Raven.Tests.Helpers;
using Raven.Client.Document;
using Raven.Client;
using Xunit;
using Microsoft.AspNet.Identity;
using System.Security.Claims;
using Raven.Database.Config;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Extensions.Logging;

namespace Corvalius.Identity.RavenDB.Tests
{
    public class DefaultPocoTest : RavenTestBase
    {
        private readonly ApplicationBuilder _builder;        

        public DefaultPocoTest()
        {
            var services = new ServiceCollection();
            var store = this.NewDocumentStore();
            SetupIdentityServices(services, store);

            var provider = services.BuildServiceProvider();
            _builder = new ApplicationBuilder(provider);
        }

        protected override void ModifyConfiguration(InMemoryRavenConfiguration configuration)
        {
            configuration.Storage.Voron.AllowOn32Bits = true;

            base.ModifyConfiguration(configuration);
        }

        protected void SetupIdentityServices(IServiceCollection services, IDocumentStore store)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddRavenStores<IDocumentStore>(store);
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonLetterOrDigit = false;
                options.Password.RequireUppercase = false;
                options.User.AllowedUserNameCharacters = null;
            })
            .AddDefaultTokenProviders()
            .UseRavenStores<IDocumentStore>();

            services.AddSingleton<IUserStore<IdentityUser>>(x => new UserStore<IdentityUser>(store));
            services.AddSingleton<IRoleStore<IdentityRole>>(x => new RoleStore<IdentityRole>(store));

            services.AddLogging();

            services.AddSingleton<ILogger<UserManager<IdentityUser>>>(x => new TestLogger<UserManager<IdentityUser>>());
            services.AddSingleton<ILogger<RoleManager<IdentityRole>>>(x => new TestLogger<RoleManager<IdentityRole>>());
        }


        [Fact]
        public async Task EnsureStartupUsageWorks()
        {
            var userStore = _builder.ApplicationServices.GetRequiredService<IUserStore<IdentityUser>>();
            var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);

            const string userName = "admin";
            const string password = "1qaz@WSX";
            var user = new IdentityUser { UserName = userName };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
        }

        [Fact]
        public async Task CanIncludeUserClaimsTest()
        {
            // Arrange
            var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
            var dbContext = _builder.ApplicationServices.GetRequiredService<IDocumentStore>();

            var username = "user" + new Random().Next();
            var user = new IdentityUser() { UserName = username };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));

            for (var i = 0; i < 10; i++)
            {
                IdentityResultAssert.IsSuccess(await userManager.AddClaimAsync(user, new Claim(i.ToString(), "foo")));
            }

            throw new NotImplementedException();
            // user = dbContext.Users.Include(x => x.Claims).FirstOrDefault(x => x.UserName == username);

            // Assert
            Assert.NotNull(user);
            Assert.NotNull(user.Claims);
            Assert.Equal(10, user.Claims.Count());
        }

        [Fact]
        public async Task CanIncludeUserLoginsTest()
        {
            // Arrange
            var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
            var dbContext = _builder.ApplicationServices.GetRequiredService<IDocumentStore>();

            var username = "user" + new Random().Next();
            var user = new IdentityUser() { UserName = username };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));

            for (var i = 0; i < 10; i++)
            {
                IdentityResultAssert.IsSuccess(await userManager.AddLoginAsync(user, new UserLoginInfo("foo" + i, "bar" + i, "foo")));
            }

            throw new NotImplementedException();
            // user = dbContext.Users.Include(x => x.Logins).FirstOrDefault(x => x.UserName == username);

            // Assert
            Assert.NotNull(user);
            Assert.NotNull(user.Logins);
            Assert.Equal(10, user.Logins.Count());
        }

        [Fact]
        public async Task CanIncludeUserRolesTest()
        {
            // Arrange
            var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = _builder.ApplicationServices.GetRequiredService<RoleManager<IdentityRole>>();
            var dbContext = _builder.ApplicationServices.GetRequiredService<IDocumentStore>();

            const string roleName = "Admin";
            for (var i = 0; i < 10; i++)
            {
                IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(new IdentityRole(roleName + i)));
            }

            var username = "user" + new Random().Next();
            var user = new IdentityUser() { UserName = username };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));

            for (var i = 0; i < 10; i++)
            {
                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, roleName + i));
            }

            throw new NotImplementedException();
            // user = dbContext.Users.Include(x => x.Roles).FirstOrDefault(x => x.UserName == username);

            // Assert
            Assert.NotNull(user);
            Assert.NotNull(user.Roles);
            Assert.Equal(10, user.Roles.Count());

            for (var i = 0; i < 10; i++)
            {
                //var role = dbContext.Roles.Include(r => r.Users).FirstOrDefault(r => r.Name == (roleName + i));
                //Assert.NotNull(role);
                //Assert.NotNull(role.Users);
                //Assert.Equal(1, role.Users.Count());
            }
        }

        [Fact]
        public async Task CanIncludeRoleClaimsTest()
        {
            // Arrange
            var roleManager = _builder.ApplicationServices.GetRequiredService<RoleManager<IdentityRole>>();
            var dbContext = _builder.ApplicationServices.GetRequiredService<IDocumentStore>();

            var role = new IdentityRole("Admin");

            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));

            for (var i = 0; i < 10; i++)
            {
                IdentityResultAssert.IsSuccess(await roleManager.AddClaimAsync(role, new Claim("foo" + i, "bar" + i)));
            }

            throw new NotImplementedException();
            // role = dbContext.Roles.Include(x => x.Claims).FirstOrDefault(x => x.Name == "Admin");

            // Assert
            Assert.NotNull(role);
            Assert.NotNull(role.Claims);
            Assert.Equal(10, role.Claims.Count());
        }
    }
}

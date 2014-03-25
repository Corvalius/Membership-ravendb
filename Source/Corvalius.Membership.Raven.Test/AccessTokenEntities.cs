using Raven.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Corvalius.Membership.Raven.Test
{
    public class AccessTokenEntitiesTest : RavenTestBase
    {
        [Fact]
        public void EnsureCaseSensitiveTokens()
        {
            using (var store = NewDocumentStore())
            {
                // Arrange

                // Make sure the entire configuration is presented to the storage.
                Initializer.InitializeDatabaseConnection(store);

                // Act
                using (var db = store.OpenSession())
                {
                    var entity1 = new AccessTokenEntity("Aaa", "Token");
                    var entity2 = new AccessTokenEntity("aAA", "toKen");

                    db.Store(entity1);
                    db.Store(entity2);

                    db.SaveChanges();
                }

                // Assert
                using (var db = store.OpenSession())
                {
                    var e1 = db.Load<AccessTokenEntity>(AccessTokenEntity.ToRavenId("Token"));
                    var e2 = db.Load<AccessTokenEntity>(AccessTokenEntity.ToRavenId("toKen"));

                    Assert.NotNull(e1);
                    Assert.NotNull(e2);
                    Assert.NotSame(e1, e2);
                    Assert.NotEqual(e1.Token, e2.Token);
                    Assert.NotEqual(e1.User, e2.User);
                }
            }
        }
    }
}

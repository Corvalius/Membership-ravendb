using Raven.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Corvalius.Membership.Raven.Test
{
    public class TokenEntitiesTest : RavenTestBase
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
                    var entity1 = new TokenEntity("Aaa", "secret");
                    var entity2 = new TokenEntity("aAA", "non-secret");

                    db.Store(entity1);
                    db.Store(entity2);

                    db.SaveChanges();
                }

                // Assert
                using (var db = store.OpenSession())
                {
                    var e1 = db.Load<TokenEntity>(TokenEntity.ToRavenId("Aaa"));
                    var e2 = db.Load<TokenEntity>(TokenEntity.ToRavenId("aAA"));

                    Assert.NotNull(e1);
                    Assert.NotNull(e2);
                    Assert.NotSame(e1, e2);
                    Assert.NotEqual(e1.Secret, e2.Secret);
                }
            }
        }
    }
}
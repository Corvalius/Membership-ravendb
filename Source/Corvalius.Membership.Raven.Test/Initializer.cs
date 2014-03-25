using Raven.Client.Extensions;
using Raven.Client.Document;
using Raven.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Corvalius.Membership.Raven.Test
{
    public class InitializerTest : RavenTestBase
    {
        [Fact]
        public void EnsureInitializerCanInspectReadOnlyness()
        {
            using (var store = NewDocumentStore())
            {
                Initializer.InitializeDatabaseConnection(store);
            }
        }
    }
}

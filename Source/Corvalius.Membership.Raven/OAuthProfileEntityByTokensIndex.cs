using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    public class OAuthProfileEntityByTokensIndex : AbstractIndexCreationTask<OAuthProfileEntity, OAuthProfileEntity>
    {
        public OAuthProfileEntityByTokensIndex()
        {
            Map = profiles => from doc in profiles
                              select new { doc.ProviderUserId, doc.Provider, doc.UserId };

            // Used to ensure confirmation tokens are treated as a case sensitive value.
            Index(x => x.ProviderUserId, FieldIndexing.NotAnalyzed);
            Index(x => x.UserId, FieldIndexing.NotAnalyzed);
        }
    }
}
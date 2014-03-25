using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    public class ProfileEntityByAccountTokensIndex : AbstractIndexCreationTask<ProfileEntity, ProfileEntity>
    {
        public ProfileEntityByAccountTokensIndex()
        {
            Map = profiles => from doc in profiles
                              select new { doc.Name, doc.ConfirmationToken, doc.PasswordVerificationToken, doc.PasswordVerificationTokenExpirationDate };

            // Used to ensure confirmation tokens are treated as a case sensitive value.
            Index(x => x.Name, FieldIndexing.NotAnalyzed);
            Index(x => x.ConfirmationToken, FieldIndexing.NotAnalyzed);
            Index(x => x.PasswordVerificationToken, FieldIndexing.NotAnalyzed);
        }
    }
}
using Raven.Abstractions.Data;
using Raven.Client.Listeners;
using Raven.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    internal class TakeNewestConflictResolutionListener : IDocumentConflictListener
    {
        public bool TryResolveConflict(string key, JsonDocument[] conflictedDocs, out JsonDocument resolvedDocument)
        {
            var listOfConflictedDocsWithLastModified = conflictedDocs.Where(x => x != null)
                                                                     .Where(x => x.LastModified.HasValue);
            if (listOfConflictedDocsWithLastModified.Any())
            {
                var maxDate = listOfConflictedDocsWithLastModified.Max(x => x.LastModified.Value);
                resolvedDocument = listOfConflictedDocsWithLastModified.FirstOrDefault(x => x.LastModified == maxDate);
            }
            else
            {
                resolvedDocument = conflictedDocs.FirstOrDefault();
            }


            if (resolvedDocument != null)
            {
                resolvedDocument.Metadata.Remove("Raven-Replication-Conflict-Document");
                resolvedDocument.Metadata.Remove("Raven-Replication-Conflict");
                resolvedDocument.Metadata.Remove("@id");
                resolvedDocument.Metadata.Remove("@etag");

                Console.WriteLine(string.Format("Resolved Object Metadata: {0}", resolvedDocument.Metadata.ToString()));
                Console.WriteLine("Resolved conflicts with ID {0}", key);
            }

            return resolvedDocument != null;
        }
    }
}

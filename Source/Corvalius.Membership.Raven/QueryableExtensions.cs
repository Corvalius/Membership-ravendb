using Raven.Client;
using Raven.Client.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    internal static class QueryableExtensions
    {
        public static IEnumerable<T> GetStreamed<T>(this IRavenQueryable<T> query, IDocumentSession session)
        {
            using (var enumerator = session.Advanced.Stream(query))
            {
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current.Document;
                }
            }
        }
    }
}

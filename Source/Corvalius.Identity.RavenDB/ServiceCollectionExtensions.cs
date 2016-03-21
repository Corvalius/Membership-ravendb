using Microsoft.Extensions.DependencyInjection;
using Raven.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Corvalius.Identity.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRavenStores<TDocumentStore>(this IServiceCollection services, TDocumentStore store)
               where TDocumentStore : class, IDocumentStore
        {
            return services.AddInstance(typeof(TDocumentStore), store);
        }
    }
}

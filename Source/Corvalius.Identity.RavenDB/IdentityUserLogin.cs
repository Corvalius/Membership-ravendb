using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Corvalius.Identity.RavenDB
{
    /// <summary>
    /// Represents a login and its associated provider for a user.
    /// </summary>
    public class IdentityUserLogin
    {
        /// <summary>
        /// Gets or sets the login provider for the login (e.g. facebook, google)
        /// </summary>
        public string LoginProvider { get; set; }

        /// <summary>
        /// Gets or sets the unique provider identifier for this login.
        /// </summary>
        public string ProviderKey { get; set; }

        /// <summary>
        /// Gets or sets the friendly name used in a UI for this login.
        /// </summary>
        public string ProviderDisplayName { get; set; }
    }

    public class IdentityUserLoginRef
    {
        public string Id;
        public string Reference;
        
        public static string CreateReferenceId(string loginProvider, string providerKey)
        {
            return $"ref/{loginProvider}/{providerKey}";
        }
    }
}
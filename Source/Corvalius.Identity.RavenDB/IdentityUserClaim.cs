using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Corvalius.Identity.RavenDB
{
    /// <summary>
    /// Represents a claim that a user possesses. 
    /// </summary>
    public class IdentityUserClaim
    {
        /// <summary>
        /// Gets or sets the claim type for this claim.
        /// </summary>
        public string ClaimType { get; set; }

        /// <summary>
        /// Gets or sets the claim value for this claim.
        /// </summary>
        public string ClaimValue { get; set; }
    }
}

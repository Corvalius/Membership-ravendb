using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Corvalius.Identity.RavenDB
{    
    /// <summary>
    /// Represents the link between a user and a role.
    /// </summary>
    public class IdentityUserRole 
    {
        protected IdentityUserRole()
        {
            this.Roles = new List<string>();
        }

        public IdentityUserRole(string user) : this()
        {
            this.UserId = user;            
        }

        /// <summary>
        /// Gets or sets the primary key of the role that is linked to the user.
        /// </summary>
        public ICollection<string> Roles { get; set; }

        public string UserId { get; set; }            

        public static string CreateId(string userIdentity)
        {
            return $"{userIdentity}/roles";
        }
    }
}

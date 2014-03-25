using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    public class UserRoleRef
    {
        public const string IdPrefix = "authorization/user/";

        public string Username { get; set; }

        public string Role { get; set; }

        protected UserRoleRef()
        { }

        public UserRoleRef(string role, string username)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentNullException("role");

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException("username");

            this.Role = role;
            this.Username = username;            
        }

        public static string ToRavenId(UserRoleRef userRole)
        {
            return IdPrefix + userRole.Username + "/" + userRole.Role;
        }

        public static string ToRavenId(string username, string role)
        {
            return IdPrefix + username + "/" + role;
        }

        public string ToRavenId()
        {
            return IdPrefix + Username + "/" + Role;
        }
    }
}

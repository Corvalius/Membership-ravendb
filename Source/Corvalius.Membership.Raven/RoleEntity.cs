using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    internal class RoleEntity
    {
        public const string IdPrefix = "authorization/role/";

        public string Id { get; set; }

        public string Name { get; set; }

        public RoleEntity()
        { }

        public RoleEntity(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentNullException("role");

            this.Name = role;
        }

        public static string ToRavenId(string id)
        {
            return IdPrefix + id;
        }

        public static string ToRavenId(RoleEntity role)
        {
            return IdPrefix + role.Name;
        }

        public string ToRavenId()
        {
            return IdPrefix + Id;
        }
    }
}
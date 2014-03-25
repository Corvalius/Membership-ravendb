using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    public class UserMapIdEntity
    {
        public const string IdPrefix = "authorization/reverse-map/user/";

        public string Id { get; set; }

        public string Name { get; set; }

        public UserMapIdEntity()
        {
            this.Id = IdPrefix;
        }

        public static string ToRavenId(int id)
        {
            return IdPrefix + id;
        }

        public string ToRavenId()
        {
            return IdPrefix + Id;
        }

        public int GetIdAsInteger()
        {
            if (Id.Length <= IdPrefix.Length)
                throw new InvalidOperationException("There's no id assigned to the object yet.");

            string id = Id.Substring(IdPrefix.Length);
            return int.Parse(id);
        }
    }

    public class UserEntity
    {
        public const string IdPrefix = "authorization/user/";

        /// <summary>
        /// The entity id is the username, that way we can easily do a .Load() ensuring an instant access to the object and avoiding stale indexes.
        /// </summary>
        public string Name { get; set; }

        public IList<string> Roles { get; set; }

        public int ReverseId { get; set; }

        public UserEntity()
        {
            this.Name = string.Empty;
            this.Roles = new List<string>();
        }

        public UserEntity(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException("username");

            this.Name = username;
            this.Roles = new List<string>();
        }

        public UserEntity(string username, IEnumerable<string> roles)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException("username");
            if (roles == null)
                throw new ArgumentNullException("roles");

            this.Name = username;
            this.Roles = roles.ToList();
        }

        public static string ToRavenId(string name)
        {
            return IdPrefix + name;
        }

        public string ToRavenId()
        {
            return IdPrefix + Name;
        }
    }
}
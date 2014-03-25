using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    public class AccessTokenEntity
    {
        public const string IdPrefix = "authorization/access-token/";

        public string User { get; set; }

        public string Token { get; set; }

        public AccessTokenEntity()
        {
            this.Token = string.Empty;
            this.User = string.Empty;
        }

        public AccessTokenEntity(string user, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException("token");
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException("user");

            this.Token = token;
            this.User = user;
        }

        public string ToRavenId()
        {
            return IdPrefix + Convert.ToBase64String(CultureInfo.InvariantCulture.CompareInfo.GetSortKey(Token, CompareOptions.StringSort).KeyData);
        }

        public static string ToRavenId(string key)
        {
            return IdPrefix + Convert.ToBase64String(CultureInfo.InvariantCulture.CompareInfo.GetSortKey(key, CompareOptions.StringSort).KeyData);
        }
    }
}

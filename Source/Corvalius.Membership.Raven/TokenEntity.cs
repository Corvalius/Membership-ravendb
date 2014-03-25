using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    public class TokenEntity
    {
        public const string IdPrefix = "authorization/request-token/";

        public string Token { get; set; }

        public string Secret { get; set; }

        public TokenEntity()
        {
            this.Token = string.Empty;
            this.Secret = string.Empty;
        }

        public TokenEntity(string token, string secret)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException("token");
            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentNullException("secret");

            this.Token = token;
            this.Secret = secret;
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
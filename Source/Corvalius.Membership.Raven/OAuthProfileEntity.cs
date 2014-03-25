using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    public class OAuthProfileEntity
    {
        public const string IdPrefix = "membership/oauth-profile/";

        /// <summary>
        /// Gets the provider name.
        /// </summary>
        public string ProviderUserId { get; private set; }

        /// <summary>
        /// Gets the provider user id.
        /// </summary>
        public string Provider { get; private set; }

        /// <summary>
        /// Gets the local account username associated to this entity.
        /// </summary>
        public string UserId { get; set; }

        [Obsolete("Constructor not intended to be called by the application.")]
        public OAuthProfileEntity()
        {
            ProviderUserId = string.Empty;
            Provider = string.Empty;
            UserId = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuthAccount"/> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="providerUserId">The provider user id.</param>
        public OAuthProfileEntity(string provider, string providerUserId)
        {
            if (String.IsNullOrEmpty(provider))
            {
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture, Resources.Argument_Cannot_Be_Null_Or_Empty, "provider"), "provider");
            }

            if (String.IsNullOrEmpty(providerUserId))
            {
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture, Resources.Argument_Cannot_Be_Null_Or_Empty, "providerUserId"), "providerUserId");
            }

            ProviderUserId = providerUserId;
            Provider = provider;
            UserId = string.Empty;
        }

        public string ToRavenId()
        {
            return IdPrefix + Provider + "/" + ProviderUserId;
        }

        public static string ToRavenId(string provider, string providerUserId)
        {
            return IdPrefix + provider + "/" + providerUserId;
        }
    }
}
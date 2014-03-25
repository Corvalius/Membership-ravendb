// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// Code under license: http://www.apache.org/licenses/LICENSE-2.0

using Corvalius.Membership.Raven;
using Microsoft.Internal.Web.Utils;
using System;
using System.Globalization;

// This code has been imported here for the purpose of syntax compatibility with the WebMatrix SimpleMembershipProvider.
namespace Corvalius.Membership.Raven
{
    /// <summary>
    /// Represents an OpenAuth and OpenID account.
    /// </summary>
    public class OAuthAccount
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OAuthAccount"/> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="providerUserId">The provider user id.</param>
        public OAuthAccount(string provider, string providerUserId)
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

            Provider = provider;
            ProviderUserId = providerUserId;
        }

        /// <summary>
        /// Gets the provider name.
        /// </summary>
        public string Provider { get; private set; }

        /// <summary>
        /// Gets the provider user id.
        /// </summary>
        public string ProviderUserId { get; private set; }
    }
}
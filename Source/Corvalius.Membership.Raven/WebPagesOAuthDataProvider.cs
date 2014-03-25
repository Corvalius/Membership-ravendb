// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using Corvalius.Membership.Raven;
using DotNetOpenAuth.AspNet;
using System;
using System.Web.Security;

// This code has been imported here for the purpose of syntax compatibility with the WebMatrix SimpleMembershipProvider.
namespace Corvalius.Membership.Raven
{
    internal class WebPagesOAuthDataProvider : IOpenAuthDataProvider
    {
        private static ExtendedMembershipProvider VerifyProvider()
        {
            var provider = System.Web.Security.Membership.Provider as ExtendedMembershipProvider;
            if (provider == null)
            {
                throw new InvalidOperationException();
            }
            return provider;
        }

        public string GetUserNameFromOpenAuth(string openAuthProvider, string openAuthId)
        {
            ExtendedMembershipProvider provider = VerifyProvider();

            int userId = provider.GetUserIdFromOAuth(openAuthProvider, openAuthId);
            if (userId == -1)
            {
                return null;
            }

            return provider.GetUserNameFromId(userId);
        }
    }
}
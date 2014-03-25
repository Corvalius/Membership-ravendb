using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;
using DotNetOpenAuth.OpenId.Extensions.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    public class SimpleCustomTokenManager : IConsumerTokenManager
    {
        private static string TokenSecret { get; set; }

        public SimpleCustomTokenManager(string consumerKey, string consumerSecret, string tokenSecret)
        {
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
            TokenSecret = tokenSecret;
        }

        public string ConsumerKey { get; private set; }

        public string ConsumerSecret { get; private set; }

        public string GetTokenSecret(string token)
        {
            return TokenSecret;
        }

        public void StoreNewRequestToken(UnauthorizedTokenRequest request,
            ITokenSecretContainingMessage response)
        {
            TokenSecret = response.TokenSecret;
        }

        public void ExpireRequestTokenAndStoreNewAccessToken(
            string consumerKey,
            string requestToken,
            string accessToken,
            string accessTokenSecret)
        {
            TokenSecret = accessTokenSecret;
        }

        public TokenType GetTokenType(string token)
        {
            throw new NotImplementedException();
        }

        public void StoreOpenIdAuthorizedRequestToken(string consumerKey,
            AuthorizationApprovedResponse authorization)
        {
            TokenSecret = String.Empty;
        }
    }
}

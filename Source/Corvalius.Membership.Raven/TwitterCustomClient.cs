using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;
using DotNetOpenAuth.OpenId.Extensions.OAuth;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    public class TwitterCustomClient : OAuthClient
    {
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }

        /// <summary>
        /// The description of Twitter's OAuth protocol URIs for use with their "Sign in with Twitter" feature.
        /// </summary>
        public static readonly ServiceProviderDescription TwitterServiceDescription = new ServiceProviderDescription
        {
            RequestTokenEndpoint =
                new MessageReceivingEndpoint(
                    "https://api.twitter.com/oauth/request_token",
                    HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            UserAuthorizationEndpoint =
                new MessageReceivingEndpoint(
                    "https://api.twitter.com/oauth/authenticate",
                    HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            AccessTokenEndpoint =
                new MessageReceivingEndpoint(
                    "https://api.twitter.com/oauth/access_token",
                    HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                    
            TamperProtectionElements = new ITamperProtectionChannelBindingElement[] { new HmacSha1SigningBindingElement() },
        };

        public TwitterCustomClient(string consumerKey, string consumerSecret) :
            base("twitter", TwitterServiceDescription, consumerKey, consumerSecret) 
        {
            this.ConsumerKey = consumerKey;
            this.ConsumerSecret = consumerSecret;
        }

        /// Check if authentication did succeed after user is redirected back from the service provider.
        /// Also get some user basic data.
        protected override AuthenticationResult VerifyAuthenticationCore(AuthorizedTokenResponse response)
        {
            string accessToken = response.AccessToken;
            string accessSecret = (response as ITokenSecretContainingMessage).TokenSecret;
            string userId = response.ExtraData["user_id"];
            string userName = response.ExtraData["screen_name"];

            var extraData = new Dictionary<string, string>()
                            {
                                {"accesstoken", accessToken},
                                {"accesssecret", accessSecret}
                            };

            var twitterWebConsumer = new WebConsumer(TwitterServiceDescription, new SimpleCustomTokenManager(this.ConsumerKey, this.ConsumerSecret, accessSecret));
            var endpoint = new MessageReceivingEndpoint(
               "https://api.twitter.com/1.1/users/show.json?user_id="+userId,
               HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest);

            var profileResponse = twitterWebConsumer.PrepareAuthorizedRequestAndSend(endpoint, accessToken);
            if (profileResponse.Status == HttpStatusCode.OK)
            {
                using (var responseStream = profileResponse.ResponseStream)
                {
                    var reader = new StreamReader(responseStream);
                    var json = JObject.Parse(reader.ReadToEnd());

                    extraData.Add("name", (string)json["name"]);
                    extraData.Add("avatar_url", (string)json["profile_image_url"]);
                }
            }


            return new AuthenticationResult(
                isSuccessful: true,
                provider: ProviderName,
                providerUserId: userId,
                userName: userName,
                extraData: extraData);
        }
    }
}

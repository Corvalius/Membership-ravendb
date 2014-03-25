using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Corvalius.Membership.Raven.Sample.Mvc4
{
    public static class AuthConfig
    {
        private static IDictionary<string, string> providerIcons = new Dictionary<string, string>();

        public static string GetClientIcon(string providerName)
        {
            return providerIcons[providerName];
        }

        public static void RegisterProviders()
        {
            // To let users of this site log in using their accounts from other sites such as Microsoft, Facebook, and Twitter,
            // you must update this site. For more information visit http://go.microsoft.com/fwlink/?LinkID=252166all

            /*  NOTE: you MUST replace of the xxxxxxxxxxxxxxx's with a valid appId and appSecret for your application
                      in order to be able to connect!  Also, passing the extra data is only necessary if you want to
                      show the images for the OpenID provider.
            */

            // Facebook
            OAuthWebSecurity.RegisterOAuthClient(BuiltInOAuthClient.Facebook, consumerKey: "283932845017408", consumerSecret: "aa3aa2446e8691a758d0889f2406909f");
            providerIcons["facebook"] = "../Content/images/facebook.png";

            // Facebook
            OAuthWebSecurity.RegisterOAuthClient(BuiltInOAuthClient.Github, consumerKey: "6f9c542c997b5c380ba4", consumerSecret: "4575971d2ccc00a1a41b1afb8a26e910f1c06183");
            providerIcons["github"] = "../Content/images/github.png";

            //LinkedIn
            OAuthWebSecurity.RegisterOAuthClient(BuiltInOAuthClient.LinkedIn, consumerKey: "1aho1a3s5zk1", consumerSecret: "f3B9DPGMUrFk7CBk");
            providerIcons["linkedin"] = "../Content/images/linkedin.png";

            //Twitter
            OAuthWebSecurity.RegisterOAuthClient(BuiltInOAuthClient.Twitter, consumerKey: "zS70aeHzSnCDJA0zK4TZlA", consumerSecret: "Z3olNRfRdREEcY8DKii4TLjNeZAQp70krjVzCvxmc");
            providerIcons["twitter"] = "../Content/images/twitter.png";

            //displayName: "Facebook",
            //extraData: facebookExtraData);
        }
    }
}
using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.Messaging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Corvalius.Membership.Raven
{
    public class LinkedInCustomClient : OAuth2Client
    {
        private const string AuthorizationEndpoint = "https://www.linkedin.com/uas/oauth2/authorization";
        private const string TokenEndpoint = "https://www.linkedin.com/uas/oauth2/accessToken";
        private const string TokenPostFormat = "grant_type=authorization_code&code={0}&redirect_uri={1}&client_id={2}&client_secret={3}";

        private readonly string applicationId;
        private readonly string applicationSecret;

        public LinkedInCustomClient(string appId, string appSecret)
            :base("linkedin")
        {
            if (string.IsNullOrEmpty(appId))
                throw new ArgumentException("appId");

            if (string.IsNullOrEmpty(appSecret))
                throw new ArgumentException("appSecret");

            this.applicationId = appId;
            this.applicationSecret = appSecret;
        }

        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            var builder = new UriBuilder(AuthorizationEndpoint);
            builder.AppendQueryArgument("response_type", "code");
            builder.AppendQueryArgument("client_id", applicationId);
            builder.AppendQueryArgument("redirect_uri", returnUrl.AbsoluteUri);
            builder.AppendQueryArgument("state", RandomStr());
            builder.AppendQueryArgument("scope", "r_basicprofile r_emailaddress");

            return builder.Uri;
        }

        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            var builder = new UriBuilder(TokenEndpoint);
            builder.AppendQueryArgument("grant_type", "authorization_code");
            builder.AppendQueryArgument("code", authorizationCode);
            builder.AppendQueryArgument("redirect_uri", returnUrl.AbsoluteUri);
            builder.AppendQueryArgument("client_id", applicationId);
            builder.AppendQueryArgument("client_secret", applicationSecret);

            var result = new HttpClient().PostAsync(builder.Uri, new StringContent("")).Result;

            if (result.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    dynamic content = JObject.Parse(result.Content.ReadAsStringAsync().Result);
                    var token = content.access_token;
                    return token;
                }
                catch (Exception e)
                {
                    throw new UriFormatException("Unexpected format", e);
                }
            }
            else
            {
                throw new HttpRequestException("Could not query access token. Status code: "+result.StatusCode.ToString());
            }
        }

        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            try
            {
                var result = new HttpClient().GetAsync("https://api.linkedin.com/v1/people/~:(id,first-name,last-name,picture-url)?oauth2_access_token=" + accessToken).Result;

                if (result.StatusCode == HttpStatusCode.OK)
                {
                    var content = result.Content.ReadAsStringAsync().Result;
                    XDocument document = XDocument.Parse(content);
                    var userData = new Dictionary<string, string>();

                    userData.Add("id", (string)document.Root.Element("id").Value);
                    userData.Add("name", (string)document.Root.Element("first-name").Value + " " + (string)document.Root.Element("last-name").Value);
                    userData.Add("avatar_url", (string)document.Root.Element("picture-url").Value);

                    var emailResult = new HttpClient().GetAsync("https://api.linkedin.com/v1/people/~/email-address?oauth2_access_token=" + accessToken).Result;
                    if (emailResult.StatusCode == HttpStatusCode.OK)
                    {
                        var emailContent = emailResult.Content.ReadAsStringAsync().Result;
                        XDocument emailDocument = XDocument.Parse(emailContent);
                        userData.Add("email", (string)emailDocument.Element("email-address").Value);
                    }

                    return userData;
                }
                else
                {
                    throw new AuthenticationException("Could not fetch user data. Status code: " + result.StatusCode.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new AuthenticationException("Could not fetch user data: " + ex.Message);
            }
        }

        public static string RandomStr()
        {
            string rStr = Path.GetRandomFileName();
            rStr = rStr.Replace(".", ""); // For Removing the .
            return rStr;
        }
    }
}

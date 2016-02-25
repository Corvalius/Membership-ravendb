using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Web.Script.Serialization;
using System.Collections.Concurrent;

namespace Corvalius.Membership.Raven.Sample.Mvc4.App_Start
{
    public class WindowsClient : OAuth2Client
    {
        private const string AuthorizationEndpoint = "http://localhost:5678/authenticate/request";
        private const string TokenEndpoint = "http://localhost:5678/authenticate/verify";
        private const string TokenParameter = "sid={0}";


        public WindowsClient()
            : base("windows")
        {

        }

        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            var builder = new UriBuilder(AuthorizationEndpoint);
            builder.AppendQueryArgument("redirectUri", returnUrl.AbsoluteUri);

            return builder.Uri;
        }

        public override void RequestAuthentication(HttpContextBase context, Uri returnUrl)
        {
            base.RequestAuthentication(context, returnUrl);
        }

        public override AuthenticationResult VerifyAuthentication(HttpContextBase context, Uri returnPageUrl)
        {           
            string code = context.Request.QueryString["code"];
            if (string.IsNullOrEmpty(code))
            {
                return AuthenticationResult.Failed;
            }

            string accessToken = this.QueryAccessToken(returnPageUrl, code);
            if (accessToken == null)
            {
                return AuthenticationResult.Failed;
            }

            IDictionary<string, string> userData = this.GetUserData(accessToken);
            if (userData == null)
            {
                return AuthenticationResult.Failed;
            }

            string id = userData["id"];
            string name;

            // Some oAuth providers do not return value for the 'username' attribute. 
            // In that case, try the 'name' attribute. If it's still unavailable, fall back to 'id'
            if (!userData.TryGetValue("username", out name) && !userData.TryGetValue("name", out name))
            {
                name = id;
            }

            // add the access token to the user data dictionary just in case page developers want to use it
            userData["accesstoken"] = accessToken;

            return new AuthenticationResult(
                isSuccessful: true, provider: this.ProviderName, providerUserId: id, userName: name, extraData: userData);
        }


        private ConcurrentDictionary<string, Dictionary<string, string>> UserData = new ConcurrentDictionary<string, Dictionary<string, string>>();

        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            Dictionary<string, string> userData;
            if (!UserData.TryGetValue(accessToken, out userData))
            {
                userData = new Dictionary<string, string>();
            }
            return userData;
        }

        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            var ub = new UriBuilder(TokenEndpoint);
            ub.Query = string.Format(TokenParameter, authorizationCode);

            try
            {
                using (var httpClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true }))
                using (var response = httpClient.GetAsync(ub.ToString()).Result)
                {
                    string responseBody = response.Content.ReadAsStringAsync().Result;

                    dynamic msg = new JavaScriptSerializer().Deserialize<dynamic>(responseBody);
                    if (msg["result"] == "ok")
                    {
                        var userData = new Dictionary<string, string>();
                        foreach( var item in (Dictionary<string, object>) msg ) 
                            userData[item.Key] = item.Value.ToString();

                        // Make sure we don't kill membership.
                        var username = userData["username"].Replace('\\', '/');
                        
                        userData["id"] = username;
                        userData["username"] = username;

                        this.UserData[username] = userData;

                        return username;
                    }
                        
                }
            }
            catch { }
            return null;
        }
    }
}
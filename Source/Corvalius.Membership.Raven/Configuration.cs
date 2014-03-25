using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    internal class Configuration
    {
        private static string _loginUrl = GetLoginUrl();

        public static bool WebMatrixSimpleMembershipEnabled
        {
            get
            {
                string settingValue = ConfigurationManager.AppSettings["enableSimpleMembership"];
                bool enabled;
                if (!String.IsNullOrEmpty(settingValue) && Boolean.TryParse(settingValue, out enabled))
                {
                    return enabled;
                }

                // WebMatrix Simple Membership is nowhere to be found.
                return false;
            }
        }

        public static bool SimpleMembershipEnabled
        {
            get
            {
                string settingValue = ConfigurationManager.AppSettings[SimpleMembershipProvider.EnableRavenDbSimpleMembershipKey];
                bool enabled;
                if (!String.IsNullOrEmpty(settingValue) && Boolean.TryParse(settingValue, out enabled))
                {
                    return enabled;
                }

                // Simple Membership is enabled by default, but attempts to delegate to the current provider if not initialized.
                return true;
            }
        }

        public static bool SimpleRolesEnabled
        {
            get
            {
                string settingValue = ConfigurationManager.AppSettings[SimpleRoleProvider.EnableRavenDbSimpleRolesKey];
                bool enabled;
                if (!String.IsNullOrEmpty(settingValue) && Boolean.TryParse(settingValue, out enabled))
                {
                    return enabled;
                }

                // Simple Membership is enabled by default, but attempts to delegate to the current provider if not initialized.
                return true;
            }
        }

        public static string LoginUrl
        {
            get { return _loginUrl; }
        }

        private static string GetLoginUrl()
        {
            return ConfigurationManager.AppSettings[FormsAuthenticationSettings.LoginUrlKey] ?? FormsAuthenticationSettings.DefaultLoginUrl;
        }

        /// <summary>
        /// Defines key names for use in a web.config &lt;appSettings&gt; section to override default settings.
        /// </summary>
        public static class FormsAuthenticationSettings
        {
            [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Justification = "The term Login is used more frequently in ASP.Net")]
            public static readonly string LoginUrlKey = "loginUrl";

            [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Justification = "The term Login is used more frequently in ASP.Net")]
            public static readonly string DefaultLoginUrl = "~/Account/Login";

            [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Justification = "The term Login is used more frequently in ASP.Net")]
            public static readonly string PreserveLoginUrlKey = "PreserveLoginUrl";
        }
    }
}
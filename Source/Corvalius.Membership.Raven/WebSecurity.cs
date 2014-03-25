using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.WebPages;

// This code has been imported here for the purpose of syntax compatibility with the WebMatrix SimpleMembershipProvider.
namespace Corvalius.Membership.Raven
{
    public static class WebSecurity
    {
        /// <summary>
        /// Gets a value indicating whether the <see cref="Initializer.InitializeDatabaseConnection"/> method has been initialized.
        /// </summary>
        /// <value>
        ///   <c>true</c> if initialized; otherwise, <c>false</c>.
        /// </value>
        public static bool Initialized
        {
            get { return Initializer.Initialized; }
        }

        public static int CurrentUserId
        {
            get { return GetUserId(CurrentUserName); }
        }

        public static string CurrentUserName
        {
            get { return Context.User.Identity.Name; }
        }

        public static bool HasUserId
        {
            get { return CurrentUserId != -1; }
        }

        public static bool IsAuthenticated
        {
            get { return Request.IsAuthenticated; }
        }

        internal static HttpContextBase Context
        {
            get { return new HttpContextWrapper(HttpContext.Current); }
        }

        internal static HttpRequestBase Request
        {
            get { return Context.Request; }
        }

        internal static HttpResponseBase Response
        {
            get { return Context.Response; }
        }

        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Justification = "Login is used more consistently in ASP.Net")]
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "This is a helper class, and we are not removing optional parameters from methods in helper classes")]
        public static bool Login(string userName, string password, bool persistCookie = false)
        {
            VerifyProvider();
            bool success = System.Web.Security.Membership.ValidateUser(userName, password);
            if (success)
            {
                FormsAuthentication.SetAuthCookie(userName, persistCookie);
            }
            return success;
        }

        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Logout", Justification = "Login is used more consistently in ASP.Net")]
        public static void Logout()
        {
            VerifyProvider();
            FormsAuthentication.SignOut();
        }

        public static bool ChangePassword(string userName, string currentPassword, string newPassword)
        {
            VerifyProvider();
            bool success = false;
            try
            {
                var currentUser = System.Web.Security.Membership.GetUser(userName, true /* userIsOnline */);
                success = currentUser.ChangePassword(currentPassword, newPassword);
            }
            catch (ArgumentException)
            {
                // An argument exception is thrown if the new password does not meet the provider's requirements
            }

            return success;
        }

        public static bool ConfirmAccount(string accountConfirmationToken)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this
            return provider.ConfirmAccount(accountConfirmationToken);
        }

        public static bool ConfirmAccount(string userName, string accountConfirmationToken)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this
            return provider.ConfirmAccount(userName, accountConfirmationToken);
        }

        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "This is a helper class, and we are not removing optional parameters from methods in helper classes")]
        public static string CreateAccount(string userName, string password, bool requireConfirmationToken = false)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.CreateAccount(userName, password, requireConfirmationToken);
        }

        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "This is a helper class, and we are not removing optional parameters from methods in helper classes")]
        public static string CreateUserAndAccount(string userName, string password, object propertyValues = null, bool requireConfirmationToken = false)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            IDictionary<string, object> values = null;
            if (propertyValues != null)
            {
                values = new RouteValueDictionary(propertyValues);
            }

            return provider.CreateUserAndAccount(userName, password, requireConfirmationToken, values);
        }

        public static bool DeleteUserAndAccount(string username)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            if (!provider.DeleteAccount(username))
                return false;

            return provider.DeleteUser(username, true);
        }

        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "This is a helper class, and we are not removing optional parameters from methods in helper classes")]
        public static string GeneratePasswordResetToken(string userName, int tokenExpirationInMinutesFromNow = 1440)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.GeneratePasswordResetToken(userName, tokenExpirationInMinutesFromNow);
        }

        public static bool UserExists(string userName)
        {
            VerifyProvider();
            return System.Web.Security.Membership.GetUser(userName) != null;
        }

        public static int GetUserId(string userName)
        {
            VerifyProvider();
            MembershipUser user = System.Web.Security.Membership.GetUser(userName);
            if (user == null)
            {
                return -1;
            }

            // REVIEW: This cast is breaking the abstraction for the membershipprovider, we basically assume that userids are ints
            return (int)user.ProviderUserKey;
        }

        public static string GetUserNameFromId(int userId)
        {  
            var provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.GetUserNameFromId(userId);
        }

        public static int GetUserIdFromPasswordResetToken(string token)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.GetUserIdFromPasswordResetToken(token);
        }

        public static bool IsCurrentUser(string userName)
        {
            VerifyProvider();
            return String.Equals(CurrentUserName, userName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsConfirmed(string userName)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.IsConfirmed(userName);
        }

        // Make sure the logged on user is same as the one specified by the id
        private static bool IsUserLoggedOn(int userId)
        {
            VerifyProvider();
            return CurrentUserId == userId;
        }

        // Make sure the user was authenticated
        public static void RequireAuthenticatedUser()
        {
            VerifyProvider();
            var user = Context.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                Response.SetStatus(HttpStatusCode.Unauthorized);
            }
        }

        // Make sure the user was authenticated
        public static void RequireUser(int userId)
        {
            VerifyProvider();
            if (!IsUserLoggedOn(userId))
            {
                Response.SetStatus(HttpStatusCode.Unauthorized);
            }
        }

        public static void RequireUser(string userName)
        {
            VerifyProvider();
            if (!String.Equals(CurrentUserName, userName, StringComparison.OrdinalIgnoreCase))
            {
                Response.SetStatus(HttpStatusCode.Unauthorized);
            }
        }

        public static void RequireRoles(params string[] roles)
        {
            VerifyProvider();
            foreach (string role in roles)
            {
                if (!Roles.IsUserInRole(CurrentUserName, role))
                {
                    Response.SetStatus(HttpStatusCode.Unauthorized);
                    return;
                }
            }
        }

        public static bool ResetPassword(string passwordResetToken, string newPassword)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this
            return provider.ResetPasswordWithToken(passwordResetToken, newPassword);
        }

        public static bool IsAccountLockedOut(string userName, int allowedPasswordAttempts, int intervalInSeconds)
        {
            VerifyProvider();
            return IsAccountLockedOut(userName, allowedPasswordAttempts, TimeSpan.FromSeconds(intervalInSeconds));
        }

        public static bool IsAccountLockedOut(string userName, int allowedPasswordAttempts, TimeSpan interval)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return IsAccountLockedOutInternal(provider, userName, allowedPasswordAttempts, interval);
        }

        internal static bool IsAccountLockedOutInternal(ExtendedMembershipProvider provider, string userName, int allowedPasswordAttempts, TimeSpan interval)
        {
            return (provider.GetUser(userName, false) != null &&
                    provider.GetPasswordFailuresSinceLastSuccess(userName) > allowedPasswordAttempts &&
                    provider.GetLastPasswordFailureDate(userName).Add(interval) > DateTime.UtcNow);
        }

        public static int GetPasswordFailuresSinceLastSuccess(string userName)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.GetPasswordFailuresSinceLastSuccess(userName);
        }

        public static DateTime GetCreateDate(string userName)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.GetCreateDate(userName);
        }

        public static DateTime GetPasswordChangedDate(string userName)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.GetPasswordChangedDate(userName);
        }

        public static DateTime GetLastPasswordFailureDate(string userName)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.GetLastPasswordFailureDate(userName);
        }

        private static ExtendedMembershipProvider VerifyProvider()
        {
            ExtendedMembershipProvider provider = System.Web.Security.Membership.Provider as ExtendedMembershipProvider;
            if (provider == null)
            {
                throw new InvalidOperationException(Resources.Security_NoExtendedMembershipProvider);
            }
            provider.VerifyInitialized(); // Have the provider verify that it's initialized (only our SimpleMembershipProvider does anything here)
            return provider;
        }

        public static string GenerateAccessToken(string username, TimeSpan expiration)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.GenerateAccessToken(username, expiration);
        }

        public static bool VerifyAccessToken(string username, string token)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.VerifyAccessToken(username, token);
        }

        public static bool HasLocalAccount(int userId)
        {
            ExtendedMembershipProvider provider = VerifyProvider();
            Debug.Assert(provider != null); // VerifyProvider checks this

            return provider.HasLocalAccount(userId);
        }
    }
}
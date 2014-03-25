using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    internal static class Resources
    {
        public static readonly string Argument_Cannot_Be_Null_Or_Empty = @"Argument cannot be null or empty: ""{0}"" ";
        public static readonly string Argument_Cannot_Be_Null_Or_Empty_Or_Whitespace = @"Argument cannot be null, empty or whitespace: ""{0}"" ";

        public static readonly string Security_NoUserFound = @"No user found was found that has the name ""{0}"".";
        public static readonly string Security_NoAccountFound = @"No account exists for ""{0}"".";
        public static readonly string Security_InitializeMustBeCalledFirst = @"You must call the ""Initializer.InitializeDatabaseConnection"" method before you call any other method of the ""Initializer"" class. This call should be placed in an _AppStart.cshtml file in the root of your site.";
        public static readonly string Security_NoExtendedMembershipProvider = @"To call this method, the ""Membership.Provider"" property must be an instance of ""ExtendedMembershipProvider"".";

        public static readonly string SimpleMembership_CannotCoexistsWithWebMatrixProvider = "Provider cannot coexists with the WebMatrix provider. Set EnableSimpleMembershipKey to false in the web.config file.";
        public static readonly string SimpleMembership_ProviderUnrecognizedAttribute = @"Provider unrecognized attribute: ""{0}"" ";

        public static readonly string SimpleMembership_PasswordTooLong = @"The membership password is too long. (Maximum length is 128 characters).";        

        public static readonly string ServiceProviderNotFound = @"";
        public static readonly string HttpContextNotAvailable = @"";
        public static readonly string InvalidServiceProviderName = @"";
        public static readonly string ServiceProviderNameExists = @"";

        public static readonly string SimpleRoleProvider_NoRoleFound = @"The role cannot be found.";
        public static readonly string SimpleRoleProvder_UserNotInRole = @"The user '{0}' is not in the role '{1}'";
        public static readonly string SimpleRoleProvider_RoleExists = @"The role '{0}' already exists.";
        public static readonly string SimpleRoleProvder_RolePopulated = "The role is already populated.";
        public static readonly string SimpleRoleProvder_UserAlreadyInRole = "The user is already in one of the roles.";        
    }
}
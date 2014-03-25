using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    /// <summary>
    /// Represents built in OAuth clients.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OAuth", Justification = "OAuth is a brand name.")]
    public enum BuiltInOAuthClient
    {
        /// <summary>
        /// Represents Twitter OAuth client
        /// </summary>
        Twitter,

        /// <summary>
        /// Represents Facebook OAuth client
        /// </summary>
        Facebook,

        /// <summary>
        /// Represents LinkedIn OAuth client
        /// </summary>
        LinkedIn,

        /// <summary>
        /// Represents WindowsLive OAuth client
        /// </summary>
        WindowsLive,

        /// <summary>
        /// Represents Github OAuth client
        /// </summary>
        Github,
    }
}
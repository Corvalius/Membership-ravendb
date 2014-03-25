using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    public class ProfileEntity
    {
        public const string IdPrefix = "membership/profile/";

        /// <summary>
        /// The entity id is the username, that way we can easily do a .Load() ensuring an instant access to the object and avoiding stale indexes.
        /// </summary>
        public string Name { get; set; }

        public DateTime? CreateDate { get; set; }

        public string ConfirmationToken { get; set; }

        public bool IsConfirmed { get; set; }

        public string Password { get; set; }

        public DateTime PasswordChangedDate { get; set; }

        public DateTime? LastPasswordFailureDate { get; set; }

        public int PasswordFailuresSinceLastSuccess { get; set; }

        public string PasswordSalt { get; set; }

        public string PasswordVerificationToken { get; set; }

        public DateTime? PasswordVerificationTokenExpirationDate { get; set; }

        public static string ToRavenId(string name)
        {
            return IdPrefix + name;
        }

        public string ToRavenId()
        {
            return IdPrefix + Name;
        }
    }
}
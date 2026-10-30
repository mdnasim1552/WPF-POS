using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.Models
{
    public class AppUser
    {
        public int AppUserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        // PBKDF2 password hash, never store the original password.
        public string PasswordHash { get; set; } = string.Empty;

        // Admin, Manager, Cashier
        public string Role { get; set; } = "Cashier";

        public bool IsActive { get; set; } = true;

        public int FailedLoginAttempts { get; set; }

        public DateTime? LockoutEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }
    }
}

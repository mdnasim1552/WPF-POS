using PosManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.Services
{
    public class CurrentUserService
    {
        public int? UserId { get; private set; }

        public string Username { get; private set; } = string.Empty;

        public string FullName { get; private set; } = string.Empty;

        public string Role { get; private set; } = string.Empty;

        public bool IsAuthenticated => UserId.HasValue;

        public bool IsAdmin => Role == "Admin";

        public bool IsManager => Role == "Manager";

        public bool IsCashier => Role == "Cashier";

        public void SignIn(AppUser user)
        {
            UserId = user.AppUserId;
            Username = user.Username;
            FullName = user.FullName;
            Role = user.Role;
        }

        public void SignOut()
        {
            UserId = null;
            Username = string.Empty;
            FullName = string.Empty;
            Role = string.Empty;
        }
    }
}

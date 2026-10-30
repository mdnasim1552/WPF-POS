using Microsoft.EntityFrameworkCore;
using PosManagement.Data;
using PosManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.Services
{
    public class AuthenticationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly PasswordService _passwordService;
        private readonly CurrentUserService _currentUser;

        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 5;

        public AuthenticationService(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            PasswordService passwordService,
            CurrentUserService currentUser)
        {
            _dbFactory = dbFactory;
            _passwordService = passwordService;
            _currentUser = currentUser;
        }

        public async Task<bool> HasUsersAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.AppUsers.AnyAsync();
        }

        public async Task<(bool Success, string Message)> CreateFirstAdminAsync(
            string username,
            string fullName,
            string password)
        {
            username = username.Trim();
            fullName = fullName.Trim();

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(fullName))
            {
                return (false, "Username and full name are required.");
            }

            if (password.Length < 8)
                return (false, "Password must contain at least 8 characters.");

            await using var db = await _dbFactory.CreateDbContextAsync();

            // Only allow initial setup when the user table is empty.
            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            if (await db.AppUsers.AnyAsync())
                return (false, "Initial setup has already been completed.");

            db.AppUsers.Add(new AppUser
            {
                Username = username,
                FullName = fullName,
                PasswordHash = _passwordService.HashPassword(password),
                Role = "Admin",
                IsActive = true
            });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Administrator account created successfully.");
        }

        public async Task<(bool Success, string Message)> LoginAsync(
            string username,
            string password)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.AppUsers.FirstOrDefaultAsync(
                u => u.Username == username.Trim());

            if (user == null)
                return (false, "Invalid username or password.");

            if (!user.IsActive)
                return (false, "This account is inactive. Contact an administrator.");

            if (user.LockoutEnd.HasValue &&
                user.LockoutEnd.Value > DateTime.Now)
            {
                return (false,
                    $"Account locked until {user.LockoutEnd.Value:HH:mm}.");
            }

            if (user.LockoutEnd.HasValue &&
                user.LockoutEnd.Value <= DateTime.Now)
            {
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
            }

            if (!_passwordService.VerifyPassword(password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutEnd = DateTime.Now.AddMinutes(LockoutMinutes);
                    user.FailedLoginAttempts = 0;
                }

                await db.SaveChangesAsync();

                return (false, "Invalid username or password.");
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.Now;

            await db.SaveChangesAsync();

            _currentUser.SignIn(user);

            return (true, "Login successful.");
        }

        public void Logout()
        {
            _currentUser.SignOut();
        }
    }
}

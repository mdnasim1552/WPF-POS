using Microsoft.EntityFrameworkCore;
using PosManagement.Data;
using PosManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.Services
{
    public class UserManagementService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly PasswordService _passwordService;
        private readonly CurrentUserService _currentUser;

        public UserManagementService(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            PasswordService passwordService,
            CurrentUserService currentUser)
        {
            _dbFactory = dbFactory;
            _passwordService = passwordService;
            _currentUser = currentUser;
        }

        private void EnsureAdmin()
        {
            if (!_currentUser.IsAdmin)
                throw new UnauthorizedAccessException(
                    "Only administrators can manage users.");
        }

        public async Task<List<AppUser>> GetUsersAsync()
        {
            EnsureAdmin();

            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.AppUsers
                .AsNoTracking()
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task CreateUserAsync(
            string username,
            string fullName,
            string password,
            string role)
        {
            EnsureAdmin();

            username = username.Trim();
            fullName = fullName.Trim();
            role = role.Trim();

            ValidateUser(username, fullName, role);

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new InvalidOperationException(
                    "Password must contain at least 8 characters.");

            await using var db = await _dbFactory.CreateDbContextAsync();

            bool exists = await db.AppUsers
                .AnyAsync(u => u.Username == username);

            if (exists)
                throw new InvalidOperationException(
                    "This username already exists.");

            var user = new AppUser
            {
                Username = username,
                FullName = fullName,
                PasswordHash = _passwordService.HashPassword(password),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.AppUsers.Add(user);
            await db.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(
            int userId,
            string username,
            string fullName,
            string role,
            bool isActive)
        {
            EnsureAdmin();

            username = username.Trim();
            fullName = fullName.Trim();
            role = role.Trim();

            ValidateUser(username, fullName, role);

            if (_currentUser.UserId == userId &&
                (!isActive || role != "Admin"))
            {
                throw new InvalidOperationException(
                    "You cannot deactivate or remove the Admin role from your own account.");
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.AppUsers
                .FirstOrDefaultAsync(u => u.AppUserId == userId);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            bool duplicate = await db.AppUsers.AnyAsync(u =>
                u.AppUserId != userId &&
                u.Username == username);

            if (duplicate)
                throw new InvalidOperationException(
                    "This username already exists.");

            user.Username = username;
            user.FullName = fullName;
            user.Role = role;
            user.IsActive = isActive;

            await db.SaveChangesAsync();
        }

        public async Task ResetPasswordAsync(
            int userId,
            string newPassword)
        {
            EnsureAdmin();

            if (string.IsNullOrWhiteSpace(newPassword) ||
                newPassword.Length < 8)
            {
                throw new InvalidOperationException(
                    "Password must contain at least 8 characters.");
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.AppUsers
                .FirstOrDefaultAsync(u => u.AppUserId == userId);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            user.PasswordHash = _passwordService.HashPassword(newPassword);
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            await db.SaveChangesAsync();
        }

        private static void ValidateUser(
            string username,
            string fullName,
            string role)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
                throw new InvalidOperationException(
                    "Username is required and must be at most 50 characters.");

            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100)
                throw new InvalidOperationException(
                    "Full name is required and must be at most 100 characters.");

            if (role is not ("Admin" or "Manager" or "Cashier"))
                throw new InvalidOperationException("Invalid user role.");
        }
    }
}

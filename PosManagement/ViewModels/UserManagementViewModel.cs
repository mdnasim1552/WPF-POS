using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosManagement.Models;
using PosManagement.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace PosManagement.ViewModels
{
    public partial class UserManagementViewModel : ObservableObject
    {
        private readonly UserManagementService _userService;

        public ObservableCollection<AppUser> Users { get; } = new();

        public string[] Roles { get; } =
        {
        "Admin",
        "Manager",
        "Cashier"
    };

        [ObservableProperty]
        private AppUser? selectedUser;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string fullName = string.Empty;

        [ObservableProperty]
        private string selectedRole = "Cashier";

        [ObservableProperty]
        private bool isActive = true;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        public UserManagementViewModel(UserManagementService userService)
        {
            _userService = userService;
        }

        [RelayCommand]
        public async Task LoadUsersAsync()
        {
            try
            {
                IsBusy = true;

                Users.Clear();

                var users = await _userService.GetUsersAsync();

                foreach (var user in users)
                    Users.Add(user);

                StatusMessage = $"{Users.Count} user(s) loaded.";
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSelectedUserChanged(AppUser? value)
        {
            if (value == null)
                return;

            Username = value.Username;
            FullName = value.FullName;
            SelectedRole = value.Role;
            IsActive = value.IsActive;
            Password = string.Empty;
        }

        [RelayCommand]
        private void NewUser()
        {
            SelectedUser = null;
            Username = string.Empty;
            FullName = string.Empty;
            SelectedRole = "Cashier";
            IsActive = true;
            Password = string.Empty;
            StatusMessage = "Enter the new user's details.";
        }

        [RelayCommand]
        private async Task SaveUserAsync()
        {
            try
            {
                IsBusy = true;

                if (SelectedUser == null)
                {
                    await _userService.CreateUserAsync(
                        Username,
                        FullName,
                        Password,
                        SelectedRole);

                    StatusMessage = "User created successfully.";
                }
                else
                {
                    await _userService.UpdateUserAsync(
                        SelectedUser.AppUserId,
                        Username,
                        FullName,
                        SelectedRole,
                        IsActive);

                    StatusMessage = "User updated successfully.";
                }

                await LoadUsersAsync();
                ClearPassword();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            try
            {
                if (SelectedUser == null)
                {
                    ShowError("Select a user first.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Password) ||
                    Password.Length < 8)
                {
                    ShowError("Enter a password with at least 8 characters.");
                    return;
                }

                await _userService.ResetPasswordAsync(
                    SelectedUser.AppUserId,
                    Password);

                ClearPassword();

                StatusMessage = "Password reset successfully.";
                MessageBox.Show(
                    StatusMessage,
                    "User Management",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ClearPassword()
        {
            Password = string.Empty;
        }

        private void ShowError(string message)
        {
            StatusMessage = message;

            MessageBox.Show(
                message,
                "User Management",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}

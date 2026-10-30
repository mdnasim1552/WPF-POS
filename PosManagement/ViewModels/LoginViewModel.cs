using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosManagement.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthenticationService _authenticationService;

        public LoginViewModel(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string fullName = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private bool isFirstSetup;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        public event Action? LoginSucceeded;
        public System.Windows.Visibility SetupVisibility =>
            IsFirstSetup
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;

        public System.Windows.Visibility LoginVisibility =>
            IsFirstSetup
                ? System.Windows.Visibility.Collapsed
                : System.Windows.Visibility.Visible;

        public async Task InitializeAsync()
        {
            IsFirstSetup = !await _authenticationService.HasUsersAsync();

            StatusMessage = IsFirstSetup
                ? "Create the first administrator account."
                : "Sign in to continue.";
        }
        partial void OnIsFirstSetupChanged(bool value)
        {
            OnPropertyChanged(nameof(SetupVisibility));
            OnPropertyChanged(nameof(LoginVisibility));
        }
        [RelayCommand]
        private async Task CreateAdminAsync()
        {
            if (IsBusy)
                return;

            if (Password != ConfirmPassword)
            {
                StatusMessage = "Passwords do not match.";
                return;
            }

            IsBusy = true;

            try
            {
                var result = await _authenticationService.CreateFirstAdminAsync(
                    Username,
                    FullName,
                    Password);

                StatusMessage = result.Message;

                if (result.Success)
                {
                    IsFirstSetup = false;
                    Password = string.Empty;
                    ConfirmPassword = string.Empty;

                    StatusMessage =
                        "Administrator created. Please log in.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Setup failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrEmpty(Password))
            {
                StatusMessage = "Enter your username and password.";
                return;
            }

            IsBusy = true;

            try
            {
                var result = await _authenticationService.LoginAsync(
                    Username,
                    Password);

                StatusMessage = result.Message;

                if (result.Success)
                {
                    Password = string.Empty;
                    LoginSucceeded?.Invoke();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

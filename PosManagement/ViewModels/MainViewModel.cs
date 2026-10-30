using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PosManagement.Services;
using PosManagement.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PosManagement.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _services;
        private readonly CurrentUserService _currentUser;

        [ObservableProperty]
        private UserControl? currentView;

        [ObservableProperty]
        private string currentPageTitle = "POS Sales";

        public string CurrentUserName => _currentUser.FullName;

        public string CurrentUserRole => _currentUser.Role;

        // Role permissions
        public bool IsAdmin => _currentUser.IsAdmin;

        public bool CanManageProducts =>
            _currentUser.IsAdmin || _currentUser.IsManager;

        public bool CanViewSalesHistory =>
            _currentUser.IsAdmin ||
            _currentUser.IsManager ||
            _currentUser.IsCashier;

        public bool CanProcessReturns =>
            _currentUser.IsAdmin ||
            _currentUser.IsManager ||
            _currentUser.IsCashier;

        public bool CanViewReturnHistory =>
            _currentUser.IsAdmin || _currentUser.IsManager;

        public MainViewModel(
            IServiceProvider services,
            CurrentUserService currentUser)
        {
            _services = services;
            _currentUser = currentUser;

            // Initial page after successful login
            ShowPos();
        }

        [RelayCommand]
        private void ShowPos()
        {
            if (!_currentUser.IsAuthenticated)
                return;

            NavigateTo<PosView>("POS Sales");
        }

        [RelayCommand]
        private void ShowProducts()
        {
            if (!CanManageProducts)
                return;

            NavigateTo<ProductsView>("Products");
        }

        [RelayCommand]
        private void ShowSalesHistory()
        {
            if (!CanViewSalesHistory)
                return;

            NavigateTo<SalesHistoryView>("Sales History");
        }

        [RelayCommand]
        private void ShowReturns()
        {
            if (!CanProcessReturns)
                return;

            NavigateTo<ReturnsView>("Return / Refund");
        }

        [RelayCommand]
        private void ShowReturnHistory()
        {
            if (!CanViewReturnHistory)
                return;

            NavigateTo<ReturnHistoryView>("Return History");
        }

        [RelayCommand]
        private void ShowUserManagement()
        {
            if (!IsAdmin)
                return;

            NavigateTo<UserManagementView>("User Management");
        }
        [RelayCommand]
        private void Logout()
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var app = Application.Current;
            var currentMainWindow = app.MainWindow;

            // Prevent the application from shutting down when MainWindow closes.
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Clear the logged-in user's session.
            _currentUser.SignOut();

            // Close the current POS window.
            currentMainWindow?.Close();

            // Open the existing login window.
            var loginWindow = _services.GetRequiredService<LoginWindow>();
            bool? loginResult = loginWindow.ShowDialog();

            if (loginResult == true && _currentUser.IsAuthenticated)
            {
                // Refresh user-dependent properties after the next login.
                OnPropertyChanged(nameof(CurrentUserName));
                OnPropertyChanged(nameof(CurrentUserRole));
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(CanManageProducts));
                OnPropertyChanged(nameof(CanViewSalesHistory));
                OnPropertyChanged(nameof(CanProcessReturns));
                OnPropertyChanged(nameof(CanViewReturnHistory));

                // Navigate to POS for the newly logged-in user.
                ShowPos();

                // Open a fresh main window.
                var mainWindow = _services.GetRequiredService<MainWindow>();

                app.MainWindow = mainWindow;
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
            }
            else
            {
                // Login was cancelled or unsuccessful.
                app.Shutdown();
            }
        }
        private void NavigateTo<TView>(string title)
            where TView : UserControl
        {
            CurrentView = _services.GetRequiredService<TView>();
            CurrentPageTitle = title;
        }
    }
}

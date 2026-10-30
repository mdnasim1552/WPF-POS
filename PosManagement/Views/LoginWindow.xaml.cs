using PosManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PosManagement.Views
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;

            _viewModel.LoginSucceeded += OnLoginSucceeded;

            Loaded += async (_, _) =>
                await _viewModel.InitializeAsync();
        }

        private void PasswordBox_PasswordChanged(
            object sender,
            RoutedEventArgs e)
        {
            _viewModel.Password = PasswordBox.Password;
        }

        private void ConfirmPasswordBox_PasswordChanged(
            object sender,
            RoutedEventArgs e)
        {
            _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
        }

        private void LoginPasswordBox_PasswordChanged(
            object sender,
            RoutedEventArgs e)
        {
            _viewModel.Password = LoginPasswordBox.Password;
        }

        private void OnLoginSucceeded()
        {
            DialogResult = true;
            Close();
        }
        protected override void OnClosed(EventArgs e)
        {
            _viewModel.LoginSucceeded -= OnLoginSucceeded;
            base.OnClosed(e);
        }
    }
}

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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PosManagement.Views
{
    /// <summary>
    /// Interaction logic for UserManagementView.xaml
    /// </summary>
    public partial class UserManagementView : UserControl
    {
        private readonly UserManagementViewModel _viewModel;

        public UserManagementView(UserManagementViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;
        }

        private async void UserControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await _viewModel.LoadUsersAsync();
        }

        private void PasswordBox_PasswordChanged(
            object sender,
            RoutedEventArgs e)
        {
            _viewModel.Password = PasswordBox.Password;
        }
    }
}

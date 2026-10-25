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
    /// Interaction logic for PosView.xaml
    /// </summary>
    public partial class PosView : UserControl
    {
        private readonly PosViewModel _viewModel;

        public PosView(PosViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;

            Loaded += async (_, _) =>
                await _viewModel.LoadProductsAsync();
        }

        private void SearchBox_KeyDown(
            object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            _viewModel.HandleBarcodeScan(SearchBox.Text);
            SearchBox.Clear();
            e.Handled = true;
        }
    }
}

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
    /// Interaction logic for PosWindow.xaml
    /// </summary>
    public partial class PosWindow : Window
    {
        private readonly PosViewModel _viewModel;

        public PosWindow(PosViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (_, _) =>
                await _viewModel.LoadProductsAsync();
        }
    }
}

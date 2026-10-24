using PosManagement.ViewModels;
using PosManagement.Views;
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

namespace PosManagement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PosViewModel _posViewModel;

        public MainWindow(
            ProductsViewModel viewModel,
            PosViewModel posViewModel)
        {
            InitializeComponent();

            _posViewModel = posViewModel;

            // Keep your existing product-management setup.
            DataContext = viewModel;

            Loaded += async (_, _) =>
                await viewModel.LoadProductsAsync();
        }

        private void OpenPos_Click(object sender, RoutedEventArgs e)
        {
            var window = new PosWindow(_posViewModel)
            {
                Owner = this
            };

            window.ShowDialog();
        }
    }
}
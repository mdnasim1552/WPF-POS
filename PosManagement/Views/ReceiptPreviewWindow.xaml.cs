using System.Windows;
using System.Windows.Documents;

namespace PosManagement.Views
{
    /// <summary>
    /// Interaction logic for ReceiptPreviewWindow.xaml
    /// </summary>
    public partial class ReceiptPreviewWindow : Window
    {
        private readonly FlowDocument _document;

        public ReceiptPreviewWindow(FlowDocument document)
        {
            InitializeComponent();

            _document = document;

            ReceiptReader.Document = _document;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new System.Windows.Controls.PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource paginator = _document;

                printDialog.PrintDocument(
                    paginator.DocumentPaginator,
                    "POS Receipt");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

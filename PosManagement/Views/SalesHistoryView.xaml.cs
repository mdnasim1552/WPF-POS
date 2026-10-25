using Microsoft.EntityFrameworkCore;
using PosManagement.Data;
using PosManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for SalesHistoryView.xaml
    /// </summary>
    public partial class SalesHistoryView : UserControl
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public ObservableCollection<Sale> Sales { get; } = new();

        public SalesHistoryView(
            IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            InitializeComponent();

            _dbFactory = dbFactory;
            DataContext = this;

            Loaded += async (_, _) => await LoadSalesAsync();
        }

        private async Task LoadSalesAsync()
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();

                var records = await db.Sales
                    .AsNoTracking()
                    .OrderByDescending(s => s.SaleDate)
                    .Take(500)
                    .ToListAsync();

                Sales.Clear();

                foreach (var sale in records)
                    Sales.Add(sale);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load sales: {ex.Message}",
                    "Sales History",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadSalesAsync();
        }

        private async void PrintReceipt_Click(
            object sender, RoutedEventArgs e)
        {
            if (SalesGrid.SelectedItem is not Sale selectedSale)
            {
                MessageBox.Show("Select an invoice first.");
                return;
            }

            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();

                var sale = await db.Sales
                    .AsNoTracking()
                    .Include(s => s.Items)
                    .FirstOrDefaultAsync(
                        s => s.SaleId == selectedSale.SaleId);

                if (sale == null)
                {
                    MessageBox.Show("Invoice not found.");
                    return;
                }

                PrintSaleReceipt(sale);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not print receipt: {ex.Message}",
                    "Receipt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PrintSaleReceipt(Sale sale)
        {
            var document = new FlowDocument
            {
                PagePadding = new Thickness(35),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12
            };

            document.Blocks.Add(new Paragraph(
                new Run("          POS SALES RECEIPT"))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            });

            document.Blocks.Add(new Paragraph(
                new Run($"Invoice: {sale.InvoiceNumber}")));

            document.Blocks.Add(new Paragraph(
                new Run($"Date: {sale.SaleDate:dd-MMM-yyyy HH:mm}")));

            document.Blocks.Add(new Paragraph(
                new Run($"Payment: {sale.PaymentMethod}")));

            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            var group = new TableRowGroup();
            table.RowGroups.Add(group);

            var header = new TableRow();
            header.Cells.Add(new TableCell(new Paragraph(new Run("Item"))));
            header.Cells.Add(new TableCell(new Paragraph(new Run("Qty"))));
            header.Cells.Add(new TableCell(new Paragraph(new Run("Total"))));
            group.Rows.Add(header);

            foreach (var item in sale.Items)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.ProductName))));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Quantity.ToString()))));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.LineTotal.ToString("N2")))));
                group.Rows.Add(row);
            }

            document.Blocks.Add(table);
            document.Blocks.Add(new Paragraph(new Run($"Subtotal: {sale.Subtotal:N2}")));
            document.Blocks.Add(new Paragraph(new Run($"Discount: {sale.Discount:N2}")));
            document.Blocks.Add(new Paragraph(new Run($"Tax rate: {sale.TaxRate:N2}%")));
            document.Blocks.Add(new Paragraph(new Run($"Tax: {sale.TaxAmount:N2}")));
            document.Blocks.Add(new Paragraph(new Run($"TOTAL: {sale.Total:N2}"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold
            });
            document.Blocks.Add(new Paragraph(new Run($"Received: {sale.AmountReceived:N2}")));
            document.Blocks.Add(new Paragraph(new Run($"Change: {sale.ChangeAmount:N2}")));
            document.Blocks.Add(new Paragraph(new Run("Thank you for shopping!"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            });

            var printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                document.PageWidth = printDialog.PrintableAreaWidth;
                document.PageHeight = printDialog.PrintableAreaHeight;

                IDocumentPaginatorSource paginator = document;
                printDialog.PrintDocument(
                    paginator.DocumentPaginator,
                    $"Receipt {sale.InvoiceNumber}");
            }
        }
    }
}

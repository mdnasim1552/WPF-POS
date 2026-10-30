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
    /// Interaction logic for ReturnHistoryView.xaml
    /// </summary>
    public partial class ReturnHistoryView : UserControl
    {
        private readonly ReturnHistoryViewModel _viewModel;

        public ReturnHistoryView(ReturnHistoryViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;

            Loaded += async (_, _) =>
                await _viewModel.LoadReturnsCommand.ExecuteAsync(null);
        }

        private void PrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            var selected = _viewModel.SelectedReturn;

            if (selected == null)
            {
                MessageBox.Show(
                    "Please select a return record first.",
                    "Print Receipt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var document = new FlowDocument
            {
                PagePadding = new Thickness(40),
                FontFamily = new FontFamily("Arial"),
                FontSize = 12,
                ColumnWidth = double.PositiveInfinity
            };

            document.Blocks.Add(new Paragraph(
                new Run("RETURN / REFUND RECEIPT"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            document.Blocks.Add(new Paragraph(new Run(
                $"Return ID: {selected.SaleReturnId}")));

            document.Blocks.Add(new Paragraph(new Run(
                $"Original Invoice: {selected.InvoiceNumber}")));

            document.Blocks.Add(new Paragraph(new Run(
                $"Return Date: {selected.ReturnDate:dd-MMM-yyyy HH:mm}")));

            document.Blocks.Add(new Paragraph(new Run(
                $"Refund Method: {selected.RefundMethod}")));

            document.Blocks.Add(new Paragraph(new Run(
                $"Reason: {selected.Reason}")));

            document.Blocks.Add(new Paragraph(new Run("Returned Products"))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 15, 0, 8)
            });

            var table = new Table
            {
                CellSpacing = 0,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5)
            };

            table.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(0.7, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.3, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.3, GridUnitType.Star) });

            var group = new TableRowGroup();
            table.RowGroups.Add(group);

            var header = new TableRow();
            group.Rows.Add(header);

            AddCell(header, "Product", true);
            AddCell(header, "Qty", true);
            AddCell(header, "Unit Price", true);
            AddCell(header, "Refund", true);

            foreach (var item in selected.Items)
            {
                var row = new TableRow();
                group.Rows.Add(row);

                AddCell(row, item.ProductName);
                AddCell(row, item.Quantity.ToString());
                AddCell(row, item.UnitPrice.ToString("N2"));
                AddCell(row, item.RefundAmount.ToString("N2"));
            }

            document.Blocks.Add(table);

            document.Blocks.Add(new Paragraph(new Run(
                $"Total Refund: {selected.RefundAmount:N2}"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            });

            document.Blocks.Add(new Paragraph(new Run(
                "This document is a record of the product return and refund."))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 30, 0, 0)
            });

            var printDialog = new PrintDialog();

            if (printDialog.ShowDialog() != true)
                return;

            document.PageWidth = printDialog.PrintableAreaWidth;
            document.PageHeight = printDialog.PrintableAreaHeight;

            IDocumentPaginatorSource paginator = document;

            printDialog.PrintDocument(
                paginator.DocumentPaginator,
                $"Return-{selected.SaleReturnId}");
        }

        private static void AddCell(
            TableRow row,
            string text,
            bool isHeader = false)
        {
            var paragraph = new Paragraph(new Run(text))
            {
                Margin = new Thickness(5),
                FontWeight = isHeader
                    ? FontWeights.Bold
                    : FontWeights.Normal
            };

            row.Cells.Add(new TableCell(paragraph)
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5)
            });
        }
    }
}

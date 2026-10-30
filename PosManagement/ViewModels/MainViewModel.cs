using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PosManagement.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace PosManagement.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _services;

        [ObservableProperty]
        private UserControl? currentView;

        [ObservableProperty]
        private string currentPageTitle = "POS Sales";

        public MainViewModel(IServiceProvider services)
        {
            _services = services;

            ShowPos();
        }

        [RelayCommand]
        private void ShowProducts()
        {
            NavigateTo<ProductsView>("Products");
        }

        [RelayCommand]
        private void ShowPos()
        {
            NavigateTo<PosView>("POS Sales");
        }

        [RelayCommand]
        private void ShowSalesHistory()
        {
            NavigateTo<SalesHistoryView>("Sales History");
        }
        [RelayCommand]
        private void ShowReturns()
        {
            NavigateTo<ReturnsView>("Return / Refund");
        }
        [RelayCommand]
        private void ShowReturnHistory()
        {
            NavigateTo<ReturnHistoryView>("Return History");
        }
        private void NavigateTo<TView>(string title)
            where TView : UserControl
        {
            CurrentView = _services.GetRequiredService<TView>();
            CurrentPageTitle = title;
        }
    }
}

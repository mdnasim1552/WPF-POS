using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PosManagement.Data;
using PosManagement.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;

namespace PosManagement
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                _host = Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        // Change this connection string if necessary.
                        var connectionString =
                            @"Server=DESKTOP-94E2O8D;Database=PosManagementDb;Trusted_Connection=True;TrustServerCertificate=True;";

                        services.AddDbContextFactory<ApplicationDbContext>(
                            options => options.UseSqlServer(connectionString));

                        services.AddTransient<ProductsViewModel>();
                        services.AddTransient<PosViewModel>();
                        services.AddTransient<MainWindow>();
                    })
                    .Build();

                await _host.StartAsync();

                var mainWindow =
                    _host.Services.GetRequiredService<MainWindow>();

                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Application startup failed:\n\n{ex.Message}",
                    "POS Management",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(-1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host is not null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }

            base.OnExit(e);
        }
    }

}

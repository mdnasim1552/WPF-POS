using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PosManagement.Data;
using PosManagement.Services;
using PosManagement.ViewModels;
using PosManagement.Views;
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

                        // ViewModels: preserve page state between navigation.
                        services.AddSingleton<MainViewModel>();
                        services.AddSingleton<ProductsViewModel>();
                        services.AddSingleton<PosViewModel>();
                        services.AddSingleton<ReturnsViewModel>();
                        services.AddSingleton<ReturnHistoryViewModel>();
                        services.AddTransient<LoginViewModel>();
                        services.AddSingleton<UserManagementViewModel>();
                        services.AddSingleton<SalesHistoryViewModel>();

                        services.AddTransient<ReturnHistoryView>();
                        services.AddTransient<ProductsView>();
                        services.AddTransient<PosView>();
                        services.AddTransient<SalesHistoryView>();
                        services.AddTransient<ReturnsView>();
                        services.AddTransient<LoginWindow>();
                        services.AddTransient<MainWindow>();
                        services.AddTransient<UserManagementView>();
                        services.AddTransient<ReceiptPreviewWindow>();

                        services.AddSingleton<PasswordService>();
                        services.AddSingleton<CurrentUserService>();
                        services.AddSingleton<AuthenticationService>();
                        services.AddSingleton<UserManagementService>();
                        services.AddSingleton<ProductImageService>();

                    })
                    .Build();

                await _host.StartAsync();

                var loginWindow =
    _host.Services.GetRequiredService<LoginWindow>();

                bool? loginResult = loginWindow.ShowDialog();

                if (loginResult == true)
                {
                    var mainWindow =
                        _host.Services.GetRequiredService<MainWindow>();

                    MainWindow = mainWindow;

                    // Set shutdown mode after successful login.
                    ShutdownMode = ShutdownMode.OnMainWindowClose;

                    mainWindow.Show();
                }
                else
                {
                    Shutdown();
                }
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

using System;
using System.Windows;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Configuration;
using CarRentalManagment.ViewModels;
using CarRentalManagment.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, configurationBuilder) =>
                {
                    configurationBuilder.SetBasePath(AppContext.BaseDirectory);
                    configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<DatabaseOptions>(context.Configuration.GetSection("Database"));

                    services.AddMemoryCache();
                    services.AddSingleton<IDatabaseService, DatabaseService>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<IAuthService, AuthService>();
                    services.AddSingleton<IUserSession, UserSession>();
                    services.AddSingleton<IVehicleService, VehicleService>();
                    services.AddSingleton<IVehiclePhotoService, VehiclePhotoService>();
                    services.AddSingleton<IReservationService, ReservationService>();
                    services.AddSingleton<IFeedbackService, FeedbackService>();
                    services.AddSingleton<IViolationService, ViolationService>();
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<ILocalizationService, LocalizationService>();

                    services.AddSingleton<MainWindowViewModel>();
                    services.AddTransient<LoginSignupViewModel>();
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<VehiclesViewModel>();
                    services.AddTransient<VehicleListViewModel>();
                    services.AddTransient<VehicleDetailsViewModel>();
                    services.AddTransient<ReservationListViewModel>();
                    services.AddTransient<ReservationsViewModel>();
                    services.AddTransient<CreateReservationViewModel>();
                    services.AddTransient<FeedbackViewModel>();
                    services.AddTransient<ReportViolationViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<AddVehicleViewModel>();

                    services.AddSingleton(provider =>
                    {
                        var window = new MainWindow
                        {
                            DataContext = provider.GetRequiredService<MainWindowViewModel>()
                        };

                        return window;
                    });
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();
        }

        public IServiceProvider Services => _host.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync().ConfigureAwait(true);

            var settingsService = _host.Services.GetRequiredService<ISettingsService>();
            var themeService = _host.Services.GetRequiredService<IThemeService>();
            var localizationService = _host.Services.GetRequiredService<ILocalizationService>();

            var preferences = await settingsService.LoadAsync().ConfigureAwait(true);
            themeService.ApplyTheme(preferences.Theme);
            localizationService.ApplyLanguage(preferences.Language);

            var navigationService = _host.Services.GetRequiredService<INavigationService>();
            navigationService.Register(() => _host.Services.GetRequiredService<LoginSignupViewModel>());
            navigationService.Register(() => _host.Services.GetRequiredService<MainViewModel>());

            navigationService.NavigateTo<LoginSignupViewModel>();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync().ConfigureAwait(true);
            _host.Dispose();

            base.OnExit(e);
        }
    }
}

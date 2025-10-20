using System;
using System.Windows;
using CarRentalManagment.Models;
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
                    services.AddSingleton<NavigationStore>();
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
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<SignupViewModel>();
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
                    services.AddTransient<EditVehicleViewModel>();
                    services.AddTransient<AdminVehicleManagementViewModel>();
                    services.AddTransient<AdminReservationManagementViewModel>();
                    services.AddTransient<AdminLocationManagementViewModel>();
                    services.AddTransient<LocationEditorViewModel>();
                    services.AddTransient<AdminDiscountManagementViewModel>();

                    // Register MainViewModel with explicit factory dependencies
                    services.AddTransient<MainViewModel>(provider => new MainViewModel(
                        provider.GetRequiredService<INavigationService>(),
                        provider.GetRequiredService<IUserSession>(),
                        provider.GetRequiredService<ILocalizationService>(),
                        () => provider.GetRequiredService<VehiclesViewModel>(),
                        () => provider.GetRequiredService<ReservationsViewModel>(),
                        () => provider.GetRequiredService<FeedbackViewModel>(),
                        () => provider.GetRequiredService<ReportViolationViewModel>(),
                        () => provider.GetRequiredService<SettingsViewModel>(),
                        () => provider.GetRequiredService<AdminReservationManagementViewModel>(),
                        () => provider.GetRequiredService<AdminVehicleManagementViewModel>(),
                        () => provider.GetRequiredService<AdminLocationManagementViewModel>(),
                        () => provider.GetRequiredService<AdminDiscountManagementViewModel>(),
                        provider.GetRequiredService<ILoggerFactory>()
                    ));

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
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();
        }

        public IServiceProvider Services => _host.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync().ConfigureAwait(true);

            // Ensure admin user exists
            await EnsureAdminUserExistsAsync().ConfigureAwait(true);

            var settingsService = _host.Services.GetRequiredService<ISettingsService>();
            var themeService = _host.Services.GetRequiredService<IThemeService>();
            var localizationService = _host.Services.GetRequiredService<ILocalizationService>();

            var preferences = await settingsService.LoadAsync().ConfigureAwait(true);
            themeService.ApplyTheme(preferences.Theme);
            localizationService.ApplyLanguage(preferences.Language);

            var navigationService = _host.Services.GetRequiredService<INavigationService>();
            navigationService.Register(() => _host.Services.GetRequiredService<LoginSignupViewModel>());
            navigationService.Register(() => _host.Services.GetRequiredService<LoginViewModel>());
            navigationService.Register(() => _host.Services.GetRequiredService<SignupViewModel>());
            navigationService.Register(() => _host.Services.GetRequiredService<MainViewModel>());

            navigationService.NavigateTo<LoginViewModel>();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private async System.Threading.Tasks.Task EnsureAdminUserExistsAsync()
        {
            try
            {
                var authService = _host.Services.GetRequiredService<IAuthService>();
                var logger = _host.Services.GetRequiredService<ILogger<App>>();

                const string adminEmail = "admin@carrental.com";
                const string adminPassword = "Admin123!";

                // Check if admin user already exists
                var emailExists = await authService.EmailExistsAsync(adminEmail).ConfigureAwait(true);

                if (!emailExists)
                {
                    logger.LogInformation("Admin user does not exist. Creating default admin user...");

                    var adminUser = new User
                    {
                        Username = "admin",
                        FirstName = "Admin",
                        LastName = "User",
                        Email = adminEmail,
                        CreatedAt = DateTime.UtcNow
                    };

                    var success = await authService.SignUpAsync(adminUser, adminPassword).ConfigureAwait(true);

                    if (success)
                    {
                        logger.LogInformation("Admin user created successfully!");
                        logger.LogInformation("Admin login credentials:");
                        logger.LogInformation("  Email: {Email}", adminEmail);
                        logger.LogInformation("  Password: {Password}", adminPassword);
                    }
                    else
                    {
                        logger.LogWarning("Failed to create admin user");
                    }
                }
                else
                {
                    logger.LogInformation("Admin user already exists");
                }
            }
            catch (Exception ex)
            {
                var logger = _host.Services.GetRequiredService<ILogger<App>>();
                logger.LogError(ex, "Failed to ensure admin user exists. The application will continue, but admin features may not be available.");
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync().ConfigureAwait(true);
            _host.Dispose();

            base.OnExit(e);
        }
    }
}

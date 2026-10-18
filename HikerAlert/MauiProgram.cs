using HikerAlertApp.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HikerAlert;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        RegisterServices(builder.Services);

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Platform adapters — the MAUI half of each Core abstraction.
        services.AddSingleton<IConnectivityProvider, MauiConnectivityProvider>();
        services.AddSingleton<ILocationProvider, MauiLocationProvider>();
        services.AddSingleton<IEmergencyDialer, MauiEmergencyDialer>();
        services.AddSingleton<ISosEndpointProvider, PreferenceSosEndpointProvider>();
        services.AddSingleton<IClock>(SystemClock.Instance);

        // Persistence — JSON files under the app data directory.
        var dataDirectory = FileSystem.AppDataDirectory;

        services.AddSingleton<IUserAccountStore>(_ =>
            new JsonFileUserAccountStore(Path.Combine(dataDirectory, "accounts.json")));

        services.AddSingleton<ISosAlertQueue>(_ =>
            new JsonFileSosAlertQueue(Path.Combine(dataDirectory, "pending-alerts.json")));

        // Core services.
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<LoginAttemptTracker>();
        services.AddSingleton<HikerProfileService>();
        services.AddSingleton<SignupService>();
        services.AddSingleton<AuthenticationService>();
        services.AddSingleton<AccountDeletionService>();
        services.AddSingleton<SosAlertFactory>();
        services.AddSingleton<SosDispatchService>();
        services.AddSingleton<RangerService>();

        // Relay chain (FR09): HTTPS first, then simulated Bluetooth.
        services.AddSingleton<HttpAlertRelay>();
        services.AddSingleton<SimulatedBluetoothRelay>();
        services.AddSingleton<IAlertRelay>(provider => new CompositeAlertRelay(
            provider.GetRequiredService<HttpAlertRelay>(),
            provider.GetRequiredService<SimulatedBluetoothRelay>()));

        services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(15) });

        // Pages.
        services.AddTransient<LoginPage>();
        services.AddTransient<SignUpPage>();
        services.AddTransient<MainPage>();
        services.AddTransient<HikingTrailsPage>();
        services.AddTransient<ToolsPage>();
    }
}

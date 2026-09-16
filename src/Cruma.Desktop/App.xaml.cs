using System.IO;
using System.Windows;
using Cruma.Desktop.Data;
using Cruma.Desktop.Platform;
using Cruma.Ui;
using Cruma.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Cruma.Desktop;

/// <summary>Kompoziční kořen desktopu (DEP-004): Windows implementace rozhraní Cruma.Ui nad lokálním úložištěm a synchronizací.</summary>
public partial class App : Application
{
    private ServiceProvider? services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appData = CrumaAppData.Resolve();
        var options = DesktopOptions.Load();
        var collection = new ServiceCollection();
        collection.AddSingleton(appData);
        collection.AddSingleton(options);
        collection.AddSingleton(TimeProvider.System);
        collection.AddLogging(logging => ConfigureLogging(logging, appData));
        collection.AddWpfBlazorWebView();
#if DEBUG
        collection.AddBlazorWebViewDeveloperTools();
#endif
        collection.AddSingleton<ProtectedTokenStore>();
        collection.AddSingleton<DesktopRuntime>();
        collection.AddSingleton<ICapabilities, DesktopCapabilities>();
        collection.AddSingleton<IConnectivity, DesktopConnectivity>();
        collection.AddSingleton<ISyncStatusView, DesktopSyncStatusView>();
        collection.AddSingleton<IPendingNotes, NoPendingNotes>();
        collection.AddSingleton<IPreferences, FilePreferences>();
        collection.AddSingleton<IAppUpdates, VelopackUpdates>();
        collection.AddSingleton<INoteData, DesktopNoteData>();
        collection.AddSingleton<ICatalogData, DesktopCatalogData>();
        collection.AddSingleton<ISearchData, DesktopSearchData>();
        collection.AddSingleton<ISessionData, DesktopSessionData>();
        collection.AddCrumaUi();
        services = collection.BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Cruma.Desktop started, data folder {DataFolder}", appData.Root);

        // Neošetřená výjimka se před pádem aplikace zapíše do logu (ERR-003).
        DispatcherUnhandledException += (_, args) => logger.LogError(args.Exception, "Unhandled exception on UI thread");
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            logger.LogError(args.ExceptionObject as Exception, "Unhandled exception");

        // Stav služeb, na které reaguje UI, musí existovat dřív než okno.
        services.GetRequiredService<IConnectivity>();
        services.GetRequiredService<ISyncStatusView>();
        await services.GetRequiredService<DesktopRuntime>().StartAsync();

        var window = new MainWindow(services, appData);
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (services is not null)
        {
            services.GetRequiredService<DesktopRuntime>().DisposeAsync().AsTask().GetAwaiter().GetResult();
            services.Dispose();
        }

        NLog.LogManager.Shutdown();
        base.OnExit(e);
    }

    // Logování: NLog za ILogger, soubory v logs\ datové složky (LOG-003, LOG-004, PER-006).
    private static void ConfigureLogging(ILoggingBuilder logging, CrumaAppData appData)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "nlog.config");
        var configuration = new NLog.Config.XmlLoggingConfiguration(configPath);
        configuration.Variables["logDirectory"] = appData.LogDirectory;

        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddNLog(configuration);
    }
}

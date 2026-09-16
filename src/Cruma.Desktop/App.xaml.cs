using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Cruma.Desktop;

/// <summary>Kompoziční kořen desktopu.</summary>
public partial class App : Application
{
    private ServiceProvider? services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appData = CrumaAppData.Resolve();
        var collection = new ServiceCollection();
        collection.AddSingleton(appData);
        collection.AddLogging(logging => ConfigureLogging(logging, appData));
        collection.AddWpfBlazorWebView();
#if DEBUG
        collection.AddBlazorWebViewDeveloperTools();
#endif
        services = collection.BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Cruma.Desktop started, data folder {DataFolder}", appData.Root);

        // Neošetřená výjimka se před pádem aplikace zapíše do logu (ERR-003).
        DispatcherUnhandledException += (_, args) => logger.LogError(args.Exception, "Unhandled exception on UI thread");
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            logger.LogError(args.ExceptionObject as Exception, "Unhandled exception");

        var window = new MainWindow(services, appData);
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        services?.Dispose();
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

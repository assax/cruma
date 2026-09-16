using Cruma.Ui;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");

ConfigureLogging(builder);

var host = builder.Build();
host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Cruma.Web").LogInformation("Cruma.Web started");
await host.RunAsync();

// Logování: NLog za ILogger, v prohlížeči jen konzole, výchozí úroveň Information (LOG-003, LOG-004).
// WebAssembly nemá souborový systém pro nlog.config, proto konfigurace v kódu.
static void ConfigureLogging(WebAssemblyHostBuilder builder)
{
    var configuration = new LoggingConfiguration();
    var console = new ConsoleTarget("console")
    {
        Layout = "${level:uppercase=true} ${logger} ${message} ${exception:format=tostring}",
    };
    configuration.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, console);

    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
    builder.Logging.AddNLog(configuration);
}

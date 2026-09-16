using Cruma.Api.Client;
using Cruma.Ui;
using Cruma.Ui.Services;
using Cruma.Web.Data;
using Cruma.Web.Platform;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

ConfigureLogging(builder);

// Kompoziční kořen tenkého klienta (DEP-004): implementace rozhraní Cruma.Ui nad API a prohlížečem.
// Singletony: připojení a fronta se spouštějí z kořene a komponenty musí vidět stejné instance.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(_ =>
{
    var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    // Instance klienta (jedno otevření aplikace) – server podle ní spojuje po sobě jdoucí uložení do verze (N-3).
    http.DefaultRequestHeaders.Add("X-Cruma-Client", "web");
    http.DefaultRequestHeaders.Add("X-Cruma-Client-Instance", Guid.CreateVersion7().ToString());
    return http;
});
builder.Services.AddSingleton<CrumaApiClient>();
builder.Services.AddSingleton<BrowserModule>();
builder.Services.AddSingleton<BrowserConnectivity>();
builder.Services.AddSingleton<IConnectivity>(provider => provider.GetRequiredService<BrowserConnectivity>());
builder.Services.AddSingleton<IPreferences, BrowserPreferences>();
builder.Services.AddSingleton<ICapabilities, WebCapabilities>();
builder.Services.AddSingleton<WriteQueue>();
builder.Services.AddSingleton<IPendingNotes>(provider => provider.GetRequiredService<WriteQueue>());
builder.Services.AddSingleton<INoteData, WebNoteData>();
builder.Services.AddSingleton<ICatalogData, WebCatalogData>();
builder.Services.AddSingleton<ISearchData, WebSearchData>();
builder.Services.AddSingleton<ISessionData, WebSessionData>();
builder.Services.AddCrumaUi();

var host = builder.Build();

var browser = host.Services.GetRequiredService<BrowserModule>();
await host.Services.GetRequiredService<BrowserConnectivity>().StartAsync(browser);
await host.Services.GetRequiredService<WriteQueue>().StartAsync();

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

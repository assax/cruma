using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Logování: NLog za ILogger, konfigurace v nlog.config (LOG-003).
builder.Logging.ClearProviders();
builder.Host.UseNLog();

var app = builder.Build();

// Statické soubory klienta Cruma.Web (výjimka DEP-004 – jen hostování, žádné typy z Cruma.Web).
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Logger.LogInformation("Cruma.Server started in {Environment}", app.Environment.EnvironmentName);

await app.RunAsync();

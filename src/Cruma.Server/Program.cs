using Cruma.Server.Audit;
using Cruma.Server.Identity;
using Cruma.Server.Infrastructure;
using Cruma.Server.Notes;
using Cruma.Server.Search;
using Cruma.Server.Sync;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Logování: NLog za ILogger, konfigurace v nlog.config (LOG-003).
builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services
    .AddInfrastructureModule(builder.Configuration)
    .AddIdentityModule(builder.Configuration, builder.Environment)
    .AddAuditModule()
    .AddNotesModule(builder.Configuration)
    .AddSearchModule()
    .AddSyncModule(builder.Configuration);

// Za reverzní proxy (produkce) server věří hlavičkám X-Forwarded-* – jen když je to v konfiguraci zapnuté,
// protože server je pak dostupný výhradně přes proxy v síti kontejnerů (SEC-008).
var behindProxy = builder.Configuration.GetValue<bool>("Cruma:BehindProxy");
if (behindProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

// Migrace databáze se při startu neaplikují – jsou samostatným krokem nasazení (PER-003, OPS-005):
// `Cruma.Server migrate` je aplikuje a skončí.
if (args.Contains("migrate", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<CrumaDbContext>().Database;
    var pending = (await database.GetPendingMigrationsAsync()).ToList();
    await database.MigrateAsync();
    app.Logger.LogInformation("Database migrations applied: {Migrations}", pending.Count == 0 ? "none" : string.Join(", ", pending));
    return;
}

if (behindProxy)
{
    app.UseForwardedHeaders();
}

app.UseRequestContext();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Statické soubory klienta Cruma.Web (výjimka DEP-004 – jen hostování, žádné typy z Cruma.Web).
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Instalátor a aktualizační feed desktopu jako statické soubory bez přihlášení (I1-D-5, plan.md N-6).
app.UseDesktopFeed(builder.Configuration);

app.UseAuthentication();
app.UseAuthorization();

app.MapInfrastructureEndpoints();
app.MapIdentityEndpoints();
app.MapNotesEndpoints();
app.MapSyncEndpoints();
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Logger.LogInformation("Cruma.Server started in {Environment}", app.Environment.EnvironmentName);

await app.RunAsync();

/// <summary>Vstupní bod serveru; veřejný kvůli integračním testům (WebApplicationFactory).</summary>
public partial class Program;

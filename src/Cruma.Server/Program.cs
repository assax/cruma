using Cruma.Server.Audit;
using Cruma.Server.Identity;
using Cruma.Server.Infrastructure;
using Cruma.Server.Notes;
using Cruma.Server.Search;
using Cruma.Server.Sync;
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

var app = builder.Build();

// Migrace databáze se při startu neaplikují – jsou samostatným krokem nasazení (PER-003, OPS-005).

app.UseRequestContext();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Statické soubory klienta Cruma.Web (výjimka DEP-004 – jen hostování, žádné typy z Cruma.Web).
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

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

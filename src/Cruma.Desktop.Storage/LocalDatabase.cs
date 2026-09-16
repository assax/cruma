using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cruma.Desktop.Storage;

/// <summary>
/// Otevření lokální databáze uživatele: soubor <c>cruma-&lt;userId&gt;.db</c> v datové složce aplikace
/// (coding-conventions.md §1, PER-006). Čekající migrace se aplikují při startu – předtím se soubor zkopíruje do zálohy
/// vedle něj (desktop-pattern.md §2; desktop nemá samostatný krok nasazení).
/// </summary>
public sealed class LocalDatabase
{
    private LocalDatabase(string path) => Path = path;

    public string Path { get; }

    public static string FileName(Guid userId) => $"cruma-{userId}.db";

    public static async Task<LocalDatabase> OpenAsync(string dataFolder, Guid userId, TimeProvider time, ILogger logger, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(dataFolder);
        var database = new LocalDatabase(System.IO.Path.Combine(dataFolder, FileName(userId)));

        await using var db = database.CreateContext();
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            if (File.Exists(database.Path))
            {
                var backup = $"{database.Path}.{time.GetUtcNow():yyyyMMddHHmmss}.bak";
                File.Copy(database.Path, backup, overwrite: true);
                logger.LogInformation("Local database backed up to {BackupFile} before {MigrationCount} migrations", System.IO.Path.GetFileName(backup), pending.Count);
            }

            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Local database migrations applied: {Migrations}", string.Join(", ", pending));
        }

        return database;
    }

    public CrumaLocalDbContext CreateContext() => new(CrumaLocalDbContext.OptionsFor(Path));
}

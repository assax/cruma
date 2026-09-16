using Cruma.Server.Infrastructure;
using Cruma.Server.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Cruma.Server.Tests;

/// <summary>
/// Jeden PostgreSQL v kontejneru pro celou sadu (TST-003). Migrace se aplikují jednou – tak jako samostatný krok
/// nasazení (PER-003); testy se od sebe oddělují vlastními uživateli.
/// </summary>
[SetUpFixture]
public sealed class TestDatabase
{
    private static PostgreSqlContainer? container;

    public static string ConnectionString { get; private set; } = string.Empty;

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        container = new PostgreSqlBuilder(PostgreSqlImage.FromCompose()).Build();
        await container.StartAsync();
        ConnectionString = container.GetConnectionString();

        var options = new DbContextOptionsBuilder<CrumaDbContext>();
        InfrastructureModule.ConfigureDbContext(options, ConnectionString);
        await using var db = new CrumaDbContext(options.Options, new TestUsers.NoUser());
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    /// <summary>Vytvoří v kontejneru prázdnou databázi a vrátí připojení k ní.</summary>
    public static async Task<string> CreateEmptyDatabaseAsync(string name)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
        await command.ExecuteNonQueryAsync();
        return new NpgsqlConnectionStringBuilder(ConnectionString) { Database = name }.ConnectionString;
    }
}

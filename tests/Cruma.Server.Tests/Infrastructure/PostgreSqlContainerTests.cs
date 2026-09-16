using Npgsql;

namespace Cruma.Server.Tests.Infrastructure;

/// <summary>
/// Ověřuje, že integrační testy běží proti skutečnému PostgreSQL přes Testcontainers – lokálně nad Podmanem
/// i v CI (plan.md E-1, frame R-4, TST-003).
/// </summary>
public class PostgreSqlContainerTests
{
    [Test]
    public void FromCompose_ImageTag_IsExplicitVersion()
    {
        Assert.That(PostgreSqlImage.FromCompose(), Does.Match(@"postgres:\d+\.\d+").And.Not.Contains("latest"));
    }

    [Test]
    public async Task Connection_ToContainer_ExecutesQuery()
    {
        await using var connection = new NpgsqlConnection(TestDatabase.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("select current_setting('server_version_num')::int", connection);

        var versionNumber = (int)(await command.ExecuteScalarAsync() ?? 0);

        Assert.That(versionNumber, Is.GreaterThanOrEqualTo(180000));
    }
}

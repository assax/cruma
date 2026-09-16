using System.Text.Json.Nodes;

namespace Cruma.Content;

/// <summary>
/// Aplikuje migrace schématu v pořadí od verze dokumentu po cílovou verzi (content-document-pattern.md §1.4,
/// CNT-003). Migrace jsou jen dopředné; dokument novější než cílová verze se nechá beze změny (CNT-005).
/// </summary>
public sealed class ContentMigrator
{
    private readonly Dictionary<int, IContentMigration> migrations;

    public ContentMigrator(IEnumerable<IContentMigration> migrations, int targetVersion)
    {
        this.migrations = new Dictionary<int, IContentMigration>();
        foreach (var migration in migrations)
        {
            if (!this.migrations.TryAdd(migration.FromVersion, migration))
            {
                throw new ArgumentException($"Migrace z verze {migration.FromVersion} je registrovaná vícekrát.", nameof(migrations));
            }
        }

        for (var version = 1; version < targetVersion; version++)
        {
            if (!this.migrations.ContainsKey(version))
            {
                throw new ArgumentException($"Chybí migrace z verze {version} na {version + 1}.", nameof(migrations));
            }
        }

        TargetVersion = targetVersion;
    }

    /// <summary>Migrace schématu aplikace; pro verzi 1 žádné neexistují.</summary>
    public static ContentMigrator Default { get; } = new([], ContentDocument.CurrentSchemaVersion);

    public int TargetVersion { get; }

    /// <summary>Vrátí dokument v cílové verzi. Předaný objekt se nemění.</summary>
    public JsonObject Migrate(JsonObject document)
    {
        var result = (JsonObject)document.DeepClone();
        for (var version = ReadSchemaVersion(result); version < TargetVersion; version++)
        {
            migrations[version].Migrate(result);
            result["schemaVersion"] = version + 1;
        }

        return result;
    }

    internal static int ReadSchemaVersion(JsonObject document)
    {
        if (document["schemaVersion"] is JsonValue value && value.TryGetValue<int>(out var version) && version >= 1)
        {
            return version;
        }

        throw new ContentFormatException("Dokument nemá platnou verzi schématu (CNT-003).");
    }
}

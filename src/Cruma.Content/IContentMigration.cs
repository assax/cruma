using System.Text.Json.Nodes;

namespace Cruma.Content;

/// <summary>
/// Migrace dokumentu o jednu verzi schématu výš: <c>Document(vN) → Document(vN+1)</c>. Musí být čistá
/// a deterministická (CNT-003); verzi schématu v dokumentu nastaví <see cref="ContentMigrator"/>.
/// </summary>
public interface IContentMigration
{
    /// <summary>Verze schématu, ze které migrace vychází.</summary>
    int FromVersion { get; }

    /// <summary>Upraví předaný dokument (vlastní kopie migrátoru) na tvar verze <see cref="FromVersion"/> + 1.</summary>
    void Migrate(JsonObject document);
}

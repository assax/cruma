using System.Text.Json.Nodes;

namespace Cruma.Content.Tests;

public class ContentMigratorTests
{
    [Test]
    public void Parse_OlderSchemaVersion_AppliesMigrationsInOrder()
    {
        var applied = new List<int>();
        var migrator = new ContentMigrator([new RecordingMigration(2, applied), new RecordingMigration(1, applied)], targetVersion: 3);

        var document = ContentDocument.Parse(TestDocuments.Fixture("document-v1-all-elements.json"), migrator);

        Assert.That(applied, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(document.SchemaVersion, Is.EqualTo(3));
        Assert.That(document.ToJson()["migratedFrom"]?.AsArray().Select(node => node!.GetValue<int>()), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Migrate_SameInput_ProducesSameOutput()
    {
        var migrator = new ContentMigrator([new RecordingMigration(1, [])], targetVersion: 2);
        var source = JsonNode.Parse(TestDocuments.Fixture("document-v1-all-elements.json"))!.AsObject();

        var first = migrator.Migrate(source);
        var second = migrator.Migrate(source);

        Assert.That(JsonNode.DeepEquals(first, second), Is.True);
        Assert.That(source["schemaVersion"]!.GetValue<int>(), Is.EqualTo(1), "vstup se nemění");
    }

    [Test]
    public void Migrate_DocumentAtTargetVersion_AppliesNothing()
    {
        var applied = new List<int>();
        var migrator = new ContentMigrator([new RecordingMigration(1, applied)], targetVersion: 2);

        migrator.Migrate(new JsonObject { ["type"] = "doc", ["schemaVersion"] = 2 });

        Assert.That(applied, Is.Empty);
    }

    [Test]
    public void Parse_NewerSchemaVersion_IsKeptUnchanged()
    {
        const string json = """{"type":"doc","schemaVersion":7,"content":[{"type":"novelty","attrs":{"id":"x"}}]}""";

        var document = ContentDocument.Parse(json);

        Assert.That(document.SchemaVersion, Is.EqualTo(7));
        Assert.That(JsonNode.DeepEquals(document.ToJson(), JsonNode.Parse(json)), Is.True);
    }

    [Test]
    public void Constructor_MissingMigrationStep_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ContentMigrator([new RecordingMigration(1, [])], targetVersion: 3));
    }

    [Test]
    public void Constructor_DuplicateMigration_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ContentMigrator([new RecordingMigration(1, []), new RecordingMigration(1, [])], targetVersion: 2));
    }

    private sealed class RecordingMigration(int fromVersion, List<int> applied) : IContentMigration
    {
        public int FromVersion => fromVersion;

        public void Migrate(JsonObject document)
        {
            applied.Add(fromVersion);
            var trail = document["migratedFrom"] as JsonArray ?? [];
            trail.Add(fromVersion);
            document["migratedFrom"] = trail;
        }
    }
}

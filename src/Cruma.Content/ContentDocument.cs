using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cruma.Content;

/// <summary>
/// Obsah poznámky: JSON strom schématu Cruma s verzí schématu (CNT-001, CNT-003). Neznámé vlastnosti kořene
/// i neznámé bloky se zachovávají (CNT-005).
/// </summary>
public sealed class ContentDocument
{
    /// <summary>Aktuální verze schématu dokumentu.</summary>
    public const int CurrentSchemaVersion = 1;

    private const string SchemaVersionProperty = "schemaVersion";
    private const string ContentProperty = "content";

    private readonly JsonObject root;

    private ContentDocument(JsonObject root, int schemaVersion, ImmutableArray<ContentBlock> blocks)
    {
        this.root = root;
        SchemaVersion = schemaVersion;
        Blocks = blocks;
    }

    public int SchemaVersion { get; }

    public ImmutableArray<ContentBlock> Blocks { get; }

    /// <summary>Prázdný dokument aktuální verze schématu.</summary>
    public static ContentDocument Empty { get; } = Create([]);

    public static ContentDocument Create(IEnumerable<ContentBlock> blocks) =>
        FromRoot(new JsonObject { ["type"] = ContentTypes.Document, [SchemaVersionProperty] = CurrentSchemaVersion }, blocks);

    /// <summary>
    /// Načte dokument z JSON a aplikuje migrace schématu (CNT-003). Dokument novější verze, než zná tato aplikace,
    /// se nemigruje a zachová se beze změny.
    /// </summary>
    public static ContentDocument Parse(string json, ContentMigrator? migrator = null)
    {
        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new ContentFormatException($"Dokument není platný JSON: {exception.Message}");
        }

        return FromJson(parsed, migrator);
    }

    /// <inheritdoc cref="Parse(string, ContentMigrator?)"/>
    public static ContentDocument FromJson(JsonNode? json, ContentMigrator? migrator = null)
    {
        if (json is not JsonObject source)
        {
            throw new ContentFormatException("Dokument musí být JSON objekt.");
        }

        var migrated = (migrator ?? ContentMigrator.Default).Migrate((JsonObject)source.DeepClone());
        var blocks = migrated[ContentProperty] switch
        {
            null => [],
            JsonArray array => array.Select(ContentBlock.FromJson).ToImmutableArray(),
            _ => throw new ContentFormatException("Obsah dokumentu musí být pole bloků."),
        };

        return new ContentDocument(migrated, ContentMigrator.ReadSchemaVersion(migrated), blocks);
    }

    /// <summary>Stejný dokument (verze schématu, vlastnosti kořene) s jinými bloky.</summary>
    public ContentDocument WithBlocks(IEnumerable<ContentBlock> blocks) => FromRoot(root, blocks);

    public ContentBlock? FindBlock(string id) => Blocks.FirstOrDefault(block => block.Id == id);

    public JsonObject ToJson()
    {
        var copy = (JsonObject)root.DeepClone();
        copy[ContentProperty] = new JsonArray(Blocks.Select(block => (JsonNode)block.ToJson()).ToArray());
        return copy;
    }

    public string ToJsonString() => ToJson().ToJsonString();

    /// <summary>Obsahová shoda celého dokumentu.</summary>
    public bool ContentEquals(ContentDocument? other) => other is not null && JsonNode.DeepEquals(ToJson(), other.ToJson());

    public override string ToString() => ToJsonString();

    private static ContentDocument FromRoot(JsonObject root, IEnumerable<ContentBlock> blocks)
    {
        var copy = (JsonObject)root.DeepClone();
        copy.Remove(ContentProperty);
        var list = blocks.ToImmutableArray();
        var duplicate = list.GroupBy(block => block.Id, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ContentFormatException($"Identifikátor bloku '{duplicate.Key}' je v dokumentu vícekrát (CNT-002).");
        }

        return new ContentDocument(copy, ContentMigrator.ReadSchemaVersion(copy), list);
    }
}

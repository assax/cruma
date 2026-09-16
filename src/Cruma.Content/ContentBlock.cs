using System.Text.Json.Nodes;

namespace Cruma.Content;

/// <summary>
/// Blok nejvyšší úrovně dokumentu (content-document-pattern.md §1.1). Drží původní JSON uzel, takže neznámý typ
/// i neznámé atributy projdou načtením a uložením beze změny (CNT-005). Instance je neměnná – uzel se kopíruje.
/// </summary>
public sealed class ContentBlock
{
    private readonly JsonObject node;

    private ContentBlock(JsonObject node, string id, string type)
    {
        this.node = node;
        Id = id;
        Type = type;
    }

    /// <summary>Stabilní identifikátor bloku (CNT-002), uložený v <c>attrs.id</c>.</summary>
    public string Id { get; }

    public string Type { get; }

    /// <summary>Typ je v registru této verze aplikace.</summary>
    public bool IsKnownType => ContentTypeRegistry.IsKnownBlock(Type);

    /// <summary>Vytvoří blok z JSON uzlu; uzel se zkopíruje.</summary>
    public static ContentBlock FromJson(JsonNode? json)
    {
        if (json is not JsonObject source)
        {
            throw new ContentFormatException("Blok dokumentu musí být JSON objekt.");
        }

        var copy = (JsonObject)source.DeepClone();
        var type = ReadString(copy, "type") ?? throw new ContentFormatException("Blok dokumentu nemá typ.");
        var id = copy["attrs"] is JsonObject attrs ? ReadString(attrs, "id") : null;
        if (string.IsNullOrEmpty(id))
        {
            throw new ContentFormatException($"Blok typu '{type}' nemá identifikátor bloku.");
        }

        return new ContentBlock(copy, id, type);
    }

    /// <summary>Kopie JSON uzlu bloku.</summary>
    public JsonObject ToJson() => (JsonObject)node.DeepClone();

    /// <summary>Stejný blok s jiným identifikátorem (vložený nebo duplikovaný blok – CNT-002).</summary>
    public ContentBlock WithId(string newId)
    {
        var copy = ToJson();
        var attrs = copy["attrs"] as JsonObject ?? new JsonObject();
        copy["attrs"] = attrs;
        attrs["id"] = newId;
        return FromJson(copy);
    }

    /// <summary>Obsahová shoda bloků včetně ID, atributů a vnořeného obsahu.</summary>
    public bool ContentEquals(ContentBlock? other) => other is not null && JsonNode.DeepEquals(node, other.node);

    public override string ToString() => node.ToJsonString();

    internal JsonObject Node => node;

    private static string? ReadString(JsonObject json, string property) =>
        json[property] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}

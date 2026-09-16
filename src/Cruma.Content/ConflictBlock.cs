using System.Text.Json.Nodes;

namespace Cruma.Content;

/// <summary>Strana konfliktu.</summary>
public enum ConflictSide
{
    /// <summary>Poslední verze na serveru.</summary>
    Current,

    /// <summary>Příchozí změna klienta.</summary>
    Incoming,
}

/// <summary>Původ varianty konfliktu: typ klienta a čas změny.</summary>
public sealed record ConflictOrigin(string ClientType, DateTimeOffset ChangedAtUtc);

/// <summary>
/// Blok konfliktu (versioning-and-sync-pattern.md §3.3): obaluje dvě varianty téhož bloku, každou s původem.
/// Blok konfliktu nese identifikátor původního bloku, takže po vyřešení blok ID nezmění.
/// </summary>
public sealed record ConflictBlock(
    string BlockId,
    ContentBlock Current,
    ConflictOrigin CurrentOrigin,
    ContentBlock Incoming,
    ConflictOrigin IncomingOrigin)
{
    public ContentBlock ToBlock() =>
        ContentBlock.FromJson(new JsonObject
        {
            ["type"] = ContentTypes.Conflict,
            ["attrs"] = new JsonObject { ["id"] = BlockId },
            ["content"] = new JsonArray(Variant(ConflictSide.Current, Current, CurrentOrigin), Variant(ConflictSide.Incoming, Incoming, IncomingOrigin)),
        });

    public ContentBlock Variant(ConflictSide side) => side == ConflictSide.Current ? Current : Incoming;

    /// <summary>Přečte blok konfliktu; pro jiný typ bloku vrátí <c>null</c>.</summary>
    public static ConflictBlock? TryRead(ContentBlock block)
    {
        if (block.Type != ContentTypes.Conflict)
        {
            return null;
        }

        if (block.Node["content"] is not JsonArray { Count: 2 } variants)
        {
            throw new ContentFormatException($"Blok konfliktu '{block.Id}' nemá právě dvě varianty.");
        }

        var (current, currentOrigin) = ReadVariant(variants[0], ConflictSide.Current, block.Id);
        var (incoming, incomingOrigin) = ReadVariant(variants[1], ConflictSide.Incoming, block.Id);
        return new ConflictBlock(block.Id, current, currentOrigin, incoming, incomingOrigin);
    }

    private static JsonObject Variant(ConflictSide side, ContentBlock block, ConflictOrigin origin) => new()
    {
        ["type"] = ContentTypes.ConflictVariant,
        ["attrs"] = new JsonObject
        {
            ["side"] = SideName(side),
            ["clientType"] = origin.ClientType,
            ["changedAtUtc"] = origin.ChangedAtUtc.ToUniversalTime().ToString("O"),
        },
        ["content"] = new JsonArray(block.ToJson()),
    };

    private static (ContentBlock Block, ConflictOrigin Origin) ReadVariant(JsonNode? node, ConflictSide side, string conflictId)
    {
        if (node is not JsonObject variant
            || variant["attrs"] is not JsonObject attrs
            || (string?)attrs["side"] != SideName(side)
            || variant["content"] is not JsonArray { Count: 1 } content)
        {
            throw new ContentFormatException($"Varianta '{SideName(side)}' bloku konfliktu '{conflictId}' je poškozená.");
        }

        var origin = new ConflictOrigin(
            (string?)attrs["clientType"] ?? string.Empty,
            DateTimeOffset.Parse((string?)attrs["changedAtUtc"] ?? string.Empty, System.Globalization.CultureInfo.InvariantCulture));
        return (ContentBlock.FromJson(content[0]), origin);
    }

    private static string SideName(ConflictSide side) => side == ConflictSide.Current ? "current" : "incoming";
}

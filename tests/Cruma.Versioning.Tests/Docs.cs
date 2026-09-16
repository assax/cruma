using System.Text.Json.Nodes;
using Cruma.Content;

namespace Cruma.Versioning.Tests;

/// <summary>
/// Zápis dokumentů pro tabulkové scénáře merge: bloky oddělené mezerou, blok je <c>id:text</c>.
/// Blok konfliktu se zapisuje <c>id:!serverový|příchozí</c>, neznámý typ bloku <c>id:?text</c>.
/// Příklad: <c>"a:Úvod b:!server|klient"</c>.
/// </summary>
internal static class Docs
{
    public static readonly ConflictOrigin CurrentOrigin = new("web", new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero));
    public static readonly ConflictOrigin IncomingOrigin = new("desktop", new DateTimeOffset(2026, 9, 1, 11, 0, 0, TimeSpan.Zero));

    public static ContentDocument Parse(string notation) =>
        ContentDocument.Create(notation.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(ParseBlock));

    public static string Format(ContentDocument document) => string.Join(' ', document.Blocks.Select(FormatBlock));

    public static ContentBlock Block(string id, string text, string type = ContentTypes.Paragraph) =>
        ContentBlock.FromJson(new JsonObject
        {
            ["type"] = type,
            ["attrs"] = new JsonObject { ["id"] = id },
            ["content"] = new JsonArray(new JsonObject { ["type"] = ContentTypes.Text, ["text"] = text }),
        });

    private static ContentBlock ParseBlock(string token)
    {
        var separator = token.IndexOf(':');
        var id = token[..separator];
        var body = token[(separator + 1)..];

        if (body.StartsWith('!'))
        {
            var variants = body[1..].Split('|');
            return new ConflictBlock(id, Block(id, variants[0]), CurrentOrigin, Block(id, variants[1]), IncomingOrigin).ToBlock();
        }

        return body.StartsWith('?') ? Block(id, body[1..], "futureWidget") : Block(id, body);
    }

    private static string FormatBlock(ContentBlock block)
    {
        if (ConflictBlock.TryRead(block) is { } conflict)
        {
            return $"{block.Id}:!{Text(conflict.Current)}|{Text(conflict.Incoming)}";
        }

        return block.IsKnownType ? $"{block.Id}:{Text(block)}" : $"{block.Id}:?{Text(block)}";
    }

    private static string Text(ContentBlock block) => PlainTextExtractor.Extract(null, ContentDocument.Create([block]));
}

using System.Text;
using System.Text.Json.Nodes;

namespace Cruma.Content;

/// <summary>
/// Jediná extrakce prostého textu poznámky pro vyhledávání a kontext AI (CNT-007, content-document-pattern.md §4):
/// název, pak text uzlů v pořadí čtení. Bloky a položky seznamů oddělí nový řádek, zalomení mezera. Z bloku
/// konfliktu se berou obě varianty, z neznámého bloku všechen text, který obsahuje.
/// </summary>
public static class PlainTextExtractor
{
    public static string Extract(string? title, ContentDocument document)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(title))
        {
            builder.Append(title.Trim());
        }

        foreach (var block in document.Blocks)
        {
            AppendNode(builder, block.Node);
        }

        return builder.ToString();
    }

    private static void AppendNode(StringBuilder builder, JsonNode? node)
    {
        if (node is not JsonObject json)
        {
            return;
        }

        switch ((string?)json["type"])
        {
            case ContentTypes.Text:
                builder.Append((string?)json["text"]);
                return;
            case ContentTypes.HardBreak:
                builder.Append(' ');
                return;
        }

        if (json["content"] is not JsonArray children)
        {
            return;
        }

        // Každý uzel s obsahem (odstavec, seznam, položka, varianta konfliktu) začíná na novém řádku.
        StartLine(builder);
        foreach (var child in children)
        {
            AppendNode(builder, child);
        }
    }

    private static void StartLine(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[^1] != '\n')
        {
            builder.Append('\n');
        }
    }
}

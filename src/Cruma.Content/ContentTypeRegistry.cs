namespace Cruma.Content;

/// <summary>Druh registrovaného typu.</summary>
public enum ContentTypeKind
{
    Block,
    Node,
    Mark,
}

/// <summary>Registrovaný typ uzlu nebo značky a verze schématu, která ho zavedla.</summary>
public sealed record ContentTypeDefinition(string Name, ContentTypeKind Kind, int IntroducedInSchemaVersion);

/// <summary>
/// Registr typů bloků, uzlů a značek (content-document-pattern.md §1.3). Typ mimo registr je pro tuto verzi
/// aplikace neznámý a musí se zachovat beze změny (CNT-005).
/// </summary>
public static class ContentTypeRegistry
{
    private static readonly Dictionary<string, ContentTypeDefinition> Types = new ContentTypeDefinition[]
    {
        new(ContentTypes.Paragraph, ContentTypeKind.Block, 1),
        new(ContentTypes.Heading, ContentTypeKind.Block, 1),
        new(ContentTypes.BulletList, ContentTypeKind.Block, 1),
        new(ContentTypes.OrderedList, ContentTypeKind.Block, 1),
        new(ContentTypes.TaskList, ContentTypeKind.Block, 1),
        new(ContentTypes.Conflict, ContentTypeKind.Block, 1),
        new(ContentTypes.ListItem, ContentTypeKind.Node, 1),
        new(ContentTypes.TaskItem, ContentTypeKind.Node, 1),
        new(ContentTypes.ConflictVariant, ContentTypeKind.Node, 1),
        new(ContentTypes.Text, ContentTypeKind.Node, 1),
        new(ContentTypes.HardBreak, ContentTypeKind.Node, 1),
        new(ContentTypes.Bold, ContentTypeKind.Mark, 1),
        new(ContentTypes.Italic, ContentTypeKind.Mark, 1),
        new(ContentTypes.Underline, ContentTypeKind.Mark, 1),
        new(ContentTypes.Strike, ContentTypeKind.Mark, 1),
        new(ContentTypes.Link, ContentTypeKind.Mark, 1),
        new(ContentTypes.Highlight, ContentTypeKind.Mark, 1),
    }.ToDictionary(definition => definition.Name, StringComparer.Ordinal);

    public static IReadOnlyCollection<ContentTypeDefinition> All => Types.Values;

    public static ContentTypeDefinition? Find(string name) => Types.GetValueOrDefault(name);

    public static bool IsKnownBlock(string type) => Find(type)?.Kind == ContentTypeKind.Block;
}

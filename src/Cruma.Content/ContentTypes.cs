namespace Cruma.Content;

/// <summary>Názvy typů uzlů a značek schématu Cruma (CNT-001). Tvar odpovídá uzlovému modelu ProseMirror/TipTap.</summary>
public static class ContentTypes
{
    public const string Document = "doc";

    // Bloky nejvyšší úrovně (FR-7 akc. 1).
    public const string Paragraph = "paragraph";
    public const string Heading = "heading";
    public const string BulletList = "bulletList";
    public const string OrderedList = "orderedList";
    public const string TaskList = "taskList";

    /// <summary>Blok konfliktu se dvěma variantami (versioning-and-sync-pattern.md §3.3).</summary>
    public const string Conflict = "conflict";

    // Vnořené uzly.
    public const string ListItem = "listItem";
    public const string TaskItem = "taskItem";
    public const string ConflictVariant = "conflictVariant";
    public const string Text = "text";
    public const string HardBreak = "hardBreak";

    // Značky.
    public const string Bold = "bold";
    public const string Italic = "italic";
    public const string Underline = "underline";
    public const string Strike = "strike";
    public const string Link = "link";
    public const string Highlight = "highlight";
}

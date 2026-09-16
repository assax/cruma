namespace Cruma.Notes;

/// <summary>Výběry poznámek pro přehled, archiv, koš a filtr štítku (FR-2 akc. 3, FR-4 akc. 2, FR-5).</summary>
public static class NoteViews
{
    /// <summary>Běžný přehled: aktivní poznámky, připnuté před nepřipnutými, jinak v pořadí vstupu.</summary>
    public static IReadOnlyList<Note> Overview(IEnumerable<Note> notes) => PinnedFirst(notes.Where(note => note.State == NoteState.Active));

    public static IReadOnlyList<Note> Archive(IEnumerable<Note> notes) => PinnedFirst(notes.Where(note => note.State == NoteState.Archived));

    public static IReadOnlyList<Note> Trash(IEnumerable<Note> notes) => notes.Where(note => note.State == NoteState.Trashed).ToList();

    /// <summary>Aktivní poznámky se štítkem ze všech kategorií.</summary>
    public static IReadOnlyList<Note> WithTag(IEnumerable<Note> notes, Guid tagId) =>
        Overview(notes.Where(note => note.TagIds.Contains(tagId)));

    private static List<Note> PinnedFirst(IEnumerable<Note> notes) => notes.OrderByDescending(note => note.IsPinned).ToList();
}

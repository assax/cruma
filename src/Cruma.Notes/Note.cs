using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Cruma.Notes;

/// <summary>
/// Vlastnosti poznámky (FR-2): volitelný název, právě jedna kategorie, štítky, barva karty, připnutí a stav.
/// Obsah poznámky je dokument z Cruma.Content a verzuje se spolu s těmito vlastnostmi (Cruma.Versioning).
/// Instance je neměnná; změna vrací novou instanci.
/// </summary>
public sealed partial record Note
{
    private Note(Guid id, Guid categoryId)
    {
        Id = id;
        CategoryId = categoryId;
        TagIds = [];
    }

    public Guid Id { get; }

    /// <summary>Název; poznámka bez názvu je platná (FR-2 akc. 2).</summary>
    public string? Title { get; private init; }

    public Guid CategoryId { get; private init; }

    public ImmutableSortedSet<Guid> TagIds { get; private init; }

    /// <summary>Klíč barvy karty (např. <c>yellow</c>); hodnoty pro světlý a tmavý režim dává téma UI (UI-006).</summary>
    public string? Color { get; private init; }

    public bool IsPinned { get; private init; }

    public NoteState State { get; private init; }

    /// <summary>
    /// Nová poznámka: nevyžaduje nic kromě obsahu a vždy patří do výchozí kategorie (FR-1 akc. 2, 3; UI-005).
    /// </summary>
    public static Note Create(Guid id, Category defaultCategory)
    {
        if (!defaultCategory.IsDefault)
        {
            throw new NotesRuleException(NotesErrorCodes.DefaultCategoryRequired, "Nová poznámka patří do výchozí kategorie.");
        }

        return new Note(id, defaultCategory.Id);
    }

    /// <summary>Obnoví poznámku z uložených dat nebo ze synchronizace.</summary>
    public static Note Restore(Guid id, string? title, Guid categoryId, IEnumerable<Guid> tagIds, string? color, bool isPinned, NoteState state) =>
        new Note(id, categoryId).WithTitle(title).WithColor(color) with
        {
            TagIds = [.. tagIds],
            IsPinned = isPinned,
            State = state,
        };

    /// <summary>Nastaví název; prázdný nebo bílý název znamená bez názvu.</summary>
    public Note WithTitle(string? title) => this with { Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim() };

    public Note MoveToCategory(Guid categoryId) => this with { CategoryId = categoryId };

    public Note AddTag(Guid tagId) => this with { TagIds = TagIds.Add(tagId) };

    public Note RemoveTag(Guid tagId) => this with { TagIds = TagIds.Remove(tagId) };

    public Note WithTags(IEnumerable<Guid> tagIds) => this with { TagIds = [.. tagIds] };

    /// <summary>Nastaví barvu karty; <c>null</c> barvu zruší.</summary>
    public Note WithColor(string? color)
    {
        if (color is not null && !ColorKey().IsMatch(color))
        {
            throw new NotesRuleException(NotesErrorCodes.InvalidColor, "Barva karty musí být klíč barvy z tématu.");
        }

        return this with { Color = color };
    }

    public Note Pin() => this with { IsPinned = true };

    public Note Unpin() => this with { IsPinned = false };

    /// <summary>Stav nastavený synchronizací nebo slučováním, bez kontroly přechodu.</summary>
    public Note WithState(NoteState state) => this with { State = state };

    public Note Archive() => Transition(NoteState.Active, NoteState.Archived);

    public Note Unarchive() => Transition(NoteState.Archived, NoteState.Active);

    /// <summary>Přesun do koše z aktivního stavu i z archivu (FR-5).</summary>
    public Note MoveToTrash() => State == NoteState.Trashed ? this : this with { State = NoteState.Trashed };

    /// <summary>Obnovení z koše vrátí poznámku mezi aktivní (FR-5 akc. 2).</summary>
    public Note RestoreFromTrash() => Transition(NoteState.Trashed, NoteState.Active);

    /// <summary>Trvale smazat jde jen poznámku v koši, explicitní akcí (FR-5 akc. 3, VER-008).</summary>
    public void EnsureCanBeDeletedPermanently()
    {
        if (State != NoteState.Trashed)
        {
            throw new NotesRuleException(NotesErrorCodes.NoteNotInTrash, "Trvale smazat jde jen poznámku v koši.");
        }
    }

    public bool Equals(Note? other) =>
        other is not null
        && Id == other.Id
        && Title == other.Title
        && CategoryId == other.CategoryId
        && TagIds.SetEquals(other.TagIds)
        && Color == other.Color
        && IsPinned == other.IsPinned
        && State == other.State;

    public override int GetHashCode() => HashCode.Combine(Id, Title, CategoryId, TagIds.Count, Color, IsPinned, State);

    private Note Transition(NoteState from, NoteState to) =>
        State == from
            ? this with { State = to }
            : throw new NotesRuleException(NotesErrorCodes.InvalidStateTransition, $"Přechod ze stavu {State} do {to} není povolený.");

    [GeneratedRegex("^[a-z][a-z0-9-]{0,31}$")]
    private static partial Regex ColorKey();
}

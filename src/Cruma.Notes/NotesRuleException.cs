namespace Cruma.Notes;

/// <summary>Porušení pravidla domény poznámek. <see cref="Code"/> je stabilní strojový kód (snake_case, ERR-001).</summary>
public sealed class NotesRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>Kódy pravidel domény poznámek.</summary>
public static class NotesErrorCodes
{
    public const string DefaultCategoryRequired = "default_category_required";
    public const string DefaultCategoryCannotBeDeleted = "default_category_cannot_be_deleted";
    public const string NameRequired = "name_required";
    public const string InvalidColor = "invalid_color";
    public const string InvalidStateTransition = "invalid_state_transition";
    public const string NoteNotInTrash = "note_not_in_trash";
}

namespace Cruma.Notes;

internal static class NameRules
{
    public static string Require(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? throw new NotesRuleException(NotesErrorCodes.NameRequired, "Název nesmí být prázdný.")
            : name.Trim();
}

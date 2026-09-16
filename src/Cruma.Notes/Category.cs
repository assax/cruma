namespace Cruma.Notes;

/// <summary>
/// Kategorie poznámek (FR-3). Kategorie jsou jednoúrovňové – model nemá rodiče, takže vnořit nejdou (FR-3 akc. 2).
/// Výchozí kategorii zakládá server při založení uživatele (FR-3 akc. 5).
/// </summary>
public sealed record Category
{
    private Category(Guid id, string name, bool isDefault)
    {
        Id = id;
        Name = name;
        IsDefault = isDefault;
    }

    public Guid Id { get; }

    public string Name { get; private init; }

    public bool IsDefault { get; }

    public static Category Create(Guid id, string name) => new(id, NameRules.Require(name), isDefault: false);

    public static Category CreateDefault(Guid id, string name) => new(id, NameRules.Require(name), isDefault: true);

    /// <summary>Obnoví kategorii z uložených dat.</summary>
    public static Category Restore(Guid id, string name, bool isDefault) => new(id, NameRules.Require(name), isDefault);

    public Category Rename(string name) => this with { Name = NameRules.Require(name) };

    /// <summary>
    /// Smaže kategorii: poznámky z ní přesune do výchozí kategorie a vrátí je (FR-3 akc. 3). Výchozí kategorii
    /// smazat nejde (FR-3 akc. 4).
    /// </summary>
    public IReadOnlyList<Note> Delete(Category defaultCategory, IEnumerable<Note> notes)
    {
        if (IsDefault)
        {
            throw new NotesRuleException(NotesErrorCodes.DefaultCategoryCannotBeDeleted, "Výchozí kategorii nejde smazat.");
        }

        if (!defaultCategory.IsDefault)
        {
            throw new NotesRuleException(NotesErrorCodes.DefaultCategoryRequired, "Poznámky se přesouvají jen do výchozí kategorie.");
        }

        return notes.Where(note => note.CategoryId == Id).Select(note => note.MoveToCategory(defaultCategory.Id)).ToList();
    }
}

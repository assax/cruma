namespace Cruma.Notes;

/// <summary>Štítek (FR-4) – nezávislý na kategorii, jeden štítek mohou mít poznámky v různých kategoriích.</summary>
public sealed record Tag
{
    private Tag(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }

    public string Name { get; private init; }

    public static Tag Create(Guid id, string name) => new(id, NameRules.Require(name));

    public Tag Rename(string name) => this with { Name = NameRules.Require(name) };

    /// <summary>Smaže štítek: odebere ho z poznámek a vrátí změněné poznámky; poznámky zůstanou (FR-4 akc. 3).</summary>
    public IReadOnlyList<Note> Delete(IEnumerable<Note> notes) =>
        notes.Where(note => note.TagIds.Contains(Id)).Select(note => note.RemoveTag(Id)).ToList();
}

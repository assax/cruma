using Cruma.Notes;

namespace Cruma.Versioning.Tests;

internal static class TestNotes
{
    public static readonly Guid NoteId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Category DefaultCategory = Category.CreateDefault(Guid.Parse("00000000-0000-0000-0000-0000000000d1"), "Poznámky");
    public static readonly Guid Work = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    public static readonly Guid Private = Guid.Parse("00000000-0000-0000-0000-0000000000c2");
    public static readonly Guid TagA = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    public static readonly Guid TagB = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    public static readonly Guid TagC = Guid.Parse("00000000-0000-0000-0000-0000000000a3");

    public static readonly DateTimeOffset T0 = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    public static Note NewNote() => Note.Create(NoteId, DefaultCategory);
}

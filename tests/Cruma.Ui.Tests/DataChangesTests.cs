using Bunit;
using Cruma.Notes;
using Cruma.Ui.Components.Notes;
using Cruma.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Cruma.Ui.Tests;

/// <summary>Změny dat mimo stránku (synchronizace, jiné zařízení) se projeví bez přechodu na jinou stránku.</summary>
public class DataChangesTests : UiTestContext
{
    [Test]
    public void NoteList_DataChangedElsewhere_ReloadsWithoutNavigation()
    {
        var first = Note(Guid.Parse("00000000-0000-0000-0000-000000000031"), "první");
        var second = Note(Guid.Parse("00000000-0000-0000-0000-000000000032"), "ze synchronizace");
        NoteData.SetupSequence(notes => notes.ListAsync(NoteState.Active, null, null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<PageResult<NoteItem>>.Success(new PageResult<NoteItem>([first], 1)))
            .ReturnsAsync(DataResult<PageResult<NoteItem>>.Success(new PageResult<NoteItem>([second, first], 2)));
        var cut = Render<NoteListView>(parameters => parameters.Add(view => view.State, NoteState.Active));
        Assert.That(cut.FindAll(".note-card"), Has.Count.EqualTo(1));

        Services.GetRequiredService<IDataChanges>().NotifyChanged();

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".note-card"), Has.Count.EqualTo(2)));
    }

    [Test]
    public void NoteCard_Color_IsAppliedAsThemeTokenBackground()
    {
        var colored = Note(Guid.Parse("00000000-0000-0000-0000-000000000033"), "barevná") is var note
            ? note with { Metadata = note.Metadata.WithColor("yellow") }
            : null!;

        var cut = Render<NoteCard>(parameters => parameters.Add(card => card.Note, colored));

        Assert.That(cut.Find(".note-card").GetAttribute("style"), Is.EqualTo("background: var(--cruma-card-yellow);"));
    }
}

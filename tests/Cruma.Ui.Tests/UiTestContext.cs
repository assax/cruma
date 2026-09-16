using Bunit;
using Cruma.Content;
using Cruma.Notes;
using Cruma.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Cruma.Ui.Tests;

/// <summary>Společný základ testů komponent: služby z rozhraní Cruma.Ui jako Moq, JS interop editoru v režimu loose.</summary>
public abstract class UiTestContext : BunitContext
{
    protected static readonly Guid DefaultCategoryId = Guid.Parse("00000000-0000-0000-0000-0000000000d1");

    protected UiTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(NoteData.Object);
        Services.AddSingleton(CatalogData.Object);
        Services.AddSingleton(SearchData.Object);
        Services.AddSingleton(SessionData.Object);
        Services.AddSingleton(Connectivity.Object);
        Services.AddSingleton(Preferences.Object);
        Services.AddSingleton(Capabilities.Object);
        Services.AddSingleton(Pending.Object);
        Services.AddCrumaUi();

        Connectivity.SetupGet(connectivity => connectivity.IsOnline).Returns(true);
        Pending.SetupGet(pending => pending.Items).Returns([]);
        CatalogData.Setup(catalog => catalog.ListCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<IReadOnlyList<CategoryItem>>.Success([new CategoryItem(DefaultCategoryId, "Poznámky", true)]));
        CatalogData.Setup(catalog => catalog.ListTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<IReadOnlyList<TagItem>>.Success([]));
        NoteData.Setup(notes => notes.ListAsync(It.IsAny<NoteState>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<PageResult<NoteItem>>.Success(new PageResult<NoteItem>([], 0)));
    }

    protected Mock<INoteData> NoteData { get; } = new();

    protected Mock<ICatalogData> CatalogData { get; } = new();

    protected Mock<ISearchData> SearchData { get; } = new();

    protected Mock<ISessionData> SessionData { get; } = new();

    protected Mock<IConnectivity> Connectivity { get; } = new();

    protected Mock<IPreferences> Preferences { get; } = new();

    protected Mock<ICapabilities> Capabilities { get; } = new();

    protected Mock<IPendingNotes> Pending { get; } = new();

    protected static ContentBlock Paragraph(string id, string text) =>
        ContentBlock.FromJson(System.Text.Json.Nodes.JsonNode.Parse(
            $$"""{"type":"paragraph","attrs":{"id":"{{id}}"},"content":[{"type":"text","text":"{{text}}"}]}"""));

    protected static NoteItem Note(Guid id, string text, NoteState state = NoteState.Active, long version = 1, ContentDocument? document = null) =>
        new(id, version, Cruma.Notes.Note.Restore(id, null, DefaultCategoryId, [], null, false, state),
            document ?? ContentDocument.Create([Paragraph("a", text)]), false, DateTimeOffset.UnixEpoch);
}

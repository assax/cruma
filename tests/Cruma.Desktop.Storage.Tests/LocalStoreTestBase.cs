using System.Text.Json.Nodes;
using Cruma.Content;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Cruma.Desktop.Storage.Tests;

/// <summary>Lokální úložiště nad skutečným souborem SQLite v dočasné složce (testing-strategy.md §2).</summary>
public abstract class LocalStoreTestBase
{
    protected static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    protected static readonly Guid DefaultCategoryId = Guid.Parse("00000000-0000-0000-0000-0000000000d1");

    private static int counter;

    protected string Folder { get; private set; } = null!;

    protected FakeTimeProvider Clock { get; private set; } = null!;

    protected LocalDatabase Database { get; private set; } = null!;

    protected LocalNotesStore Store { get; private set; } = null!;

    [SetUp]
    public async Task OpenStoreAsync()
    {
        Folder = Path.Combine(Path.GetTempPath(), $"cruma-storage-{Environment.ProcessId}-{Interlocked.Increment(ref counter)}");
        Clock = new FakeTimeProvider(new DateTimeOffset(2030, 6, 1, 8, 0, 0, TimeSpan.Zero));
        Database = await LocalDatabase.OpenAsync(Folder, UserId, Clock, NullLogger.Instance);
        Store = new LocalNotesStore(Database, Clock, new LocalStoreOptions());

        // Výchozí kategorie přichází ze serveru při první synchronizaci (I1-D-3).
        await Store.ApplyPulledAsync(new Cruma.Sync.ChangeFeedEntry(1, Cruma.Sync.SyncEntityType.Category, DefaultCategoryId, null, false,
            Category: new Cruma.Sync.CategoryPayload("Poznámky", true)));
    }

    [TearDown]
    public void DeleteFolder()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(Folder, recursive: true);
    }

    protected static ContentDocument Doc(params string[] paragraphs) =>
        ContentDocument.Create(paragraphs.Select(notation =>
        {
            var separator = notation.IndexOf(':');
            return ContentBlock.FromJson(new JsonObject
            {
                ["type"] = "paragraph",
                ["attrs"] = new JsonObject { ["id"] = notation[..separator] },
                ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = notation[(separator + 1)..] }),
            });
        }));

    protected static Guid Id(int number) => new(number, 0x1ca1, 0x4000, [0, 0, 0, 0, 0, 0, 0, 5]);

    protected static LocalNoteChanges Changes(LocalNote note, ContentDocument? document = null, string? title = null, Guid? categoryId = null, IReadOnlyList<Guid>? tagIds = null) =>
        new(title ?? note.Metadata.Title, categoryId ?? note.Metadata.CategoryId, tagIds ?? [.. note.Metadata.TagIds], note.Metadata.Color, note.Metadata.IsPinned, document ?? note.Document);
}

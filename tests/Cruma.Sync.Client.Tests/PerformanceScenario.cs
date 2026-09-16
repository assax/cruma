using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Cruma.Content;
using Cruma.Server.Notes;
using Cruma.Server.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Cruma.Sync.Client.Tests;

/// <summary>
/// Výkonový scénář N-7 (T-55, NFR-7, S-I1-5): 50 000 poznámek s českým textem pro jednoho uživatele; měří vyhledávání
/// na serveru, první synchronizaci desktopu a vyhledávání na desktopu. Spouští se ručně:
/// <c>dotnet test tests/Cruma.Sync.Client.Tests --filter "Category=Performance"</c>. Výsledky posoudí autor.
/// </summary>
[Explicit("Výkonový scénář trvá minuty – spouští se ručně.")]
[Category("Performance")]
public class PerformanceScenario
{
    private const int NoteCount = 50_000;

    private static readonly string[] Words =
    [
        "certifikát", "serveru", "nasazení", "konfigurace", "databáze", "zálohování", "obnova", "síť", "uživatel", "přihlášení",
        "poznámka", "schůzka", "projekt", "úkol", "termín", "rozpočet", "faktura", "objednávka", "dodavatel", "zákazník",
        "aplikace", "rozhraní", "chyba", "oprava", "verze", "vydání", "testování", "dokumentace", "požadavek", "řešení",
        "příliš", "žluťoučký", "kůň", "úpěl", "ďábelské", "ódy", "čeština", "řeřicha", "šťastný", "účet",
        "Praha", "Brno", "Ostrava", "pondělí", "úterý", "středa", "čtvrtek", "pátek", "leden", "říjen",
    ];

    [Test]
    public async Task Scenario_FiftyThousandNotes()
    {
        using var server = new CrumaServerFactory();
        var user = await server.CreateUserAsync();
        var report = new StringBuilder();

        var generate = Stopwatch.StartNew();
        await GenerateAsync(server, user.UserId);
        report.AppendLine($"generování {NoteCount} poznámek: {generate.Elapsed.TotalSeconds:n1} s");

        foreach (var query in new[] { "certif", "zlutoucky kun", "projekt terminy", "rijen", "databaze zalohovani obnova" })
        {
            var watch = Stopwatch.StartNew();
            var found = await user.GetAsync<Cruma.Api.Contracts.PagedResponse<Cruma.Api.Contracts.NoteDto>>($"/api/v1/search?q={Uri.EscapeDataString(query)}&pageSize=50");
            report.AppendLine($"server hledání '{query}': {watch.ElapsedMilliseconds} ms, nalezeno {found.TotalCount}");
        }

        await using var desktop = await DesktopHarness.CreateAsync(server, user.UserId);
        var sync = Stopwatch.StartNew();
        var status = await desktop.Engine.SynchronizeAsync();
        report.AppendLine($"první synchronizace desktopu: {sync.Elapsed.TotalSeconds:n1} s, stav {status.State}");

        foreach (var query in new[] { "certif", "zlutoucky kun", "projekt terminy", "rijen", "databaze zalohovani obnova" })
        {
            var watch = Stopwatch.StartNew();
            var found = await desktop.Store.SearchNotesAsync(query, null, 0, 50);
            report.AppendLine($"desktop hledání '{query}': {watch.ElapsedMilliseconds} ms, nalezeno {found.TotalCount}");
        }

        var list = Stopwatch.StartNew();
        var overview = await desktop.Store.ListNotesAsync(Notes.NoteState.Active, null, null, 0, 50);
        report.AppendLine($"desktop přehled první stránky: {list.ElapsedMilliseconds} ms z {overview.TotalCount}");

        TestContext.Out.WriteLine(report.ToString());
        await File.WriteAllTextAsync(Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-report.txt"), report.ToString());
        Assert.That(status.State, Is.EqualTo(SyncState.Synced));
        Assert.That(overview.TotalCount, Is.EqualTo(NoteCount));
    }

    // Poznámky jdou stejnou zápisovou cestou jako z klientů (verze, index, feed, audit), paralelně v několika rozsazích.
    private static async Task GenerateAsync(CrumaServerFactory server, Guid userId)
    {
        var random = new Random(20260917);
        var documents = Enumerable.Range(0, NoteCount).Select(index => (Id: new Guid(index + 1, 0x7e7f, 0x4000, [0, 0, 0, 0, 0, 0, 0, 7]), Json: Document(random))).ToList();

        await Parallel.ForEachAsync(documents.Chunk(500), new ParallelOptions { MaxDegreeOfParallelism = 8 }, async (chunk, cancellationToken) =>
        {
            await TestUsers.AsUserAsync(server.Services, userId, async services =>
            {
                foreach (var (id, json) in chunk)
                {
                    await using var scope = services.CreateAsyncScope();
                    using var acting = scope.ServiceProvider.GetRequiredService<Cruma.Server.Infrastructure.CurrentUser>().ActAs(userId);
                    var result = await scope.ServiceProvider.GetRequiredService<INotesService>()
                        .CreateNoteAsync(new CreateNoteCommand(id, ContentDocument.Parse(json)), cancellationToken);
                    Assert.That(result.IsSuccess, Is.True);
                }

                return true;
            });
        });
    }

    private static string Document(Random random)
    {
        var blocks = new JsonArray();
        var paragraphs = random.Next(1, 6);
        for (var index = 0; index < paragraphs; index++)
        {
            var text = string.Join(' ', Enumerable.Range(0, random.Next(5, 40)).Select(_ => Words[random.Next(Words.Length)]));
            blocks.Add(new JsonObject
            {
                ["type"] = "paragraph",
                ["attrs"] = new JsonObject { ["id"] = $"b{index}" },
                ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = text }),
            });
        }

        return new JsonObject { ["type"] = "doc", ["schemaVersion"] = 1, ["content"] = blocks }.ToJsonString();
    }
}

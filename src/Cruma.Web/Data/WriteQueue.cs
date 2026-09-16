using System.Text.Json;
using Cruma.Api.Client;
using Cruma.Api.Contracts;
using Cruma.Content;
using Cruma.Ui.Services;
using Cruma.Web.Platform;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Cruma.Web.Data;

/// <summary>
/// Fronta nových poznámek bez připojení (FR-30, SYN-006): drží jen vytvoření, přežije zavření klienta (IndexedDB)
/// a po obnovení spojení se odešle sama. Opakované odeslání téže položky nevytvoří druhou poznámku, protože server
/// zná identifikátor poznámky od klienta.
/// </summary>
public sealed class WriteQueue(BrowserModule browser, CrumaApiClient api, BrowserConnectivity connectivity, TimeProvider time, ILogger<WriteQueue> logger)
    : IPendingNotes
{
    private readonly SemaphoreSlim replayLock = new(1, 1);
    private List<QueueItem> items = [];

    public IReadOnlyList<PendingNote> Items => [.. items.Select(item => new PendingNote(item.Id, item.Preview, item.CreatedAtUtc))];

    public event Action? Changed;

    public async Task StartAsync()
    {
        await RefreshAsync();
        connectivity.Changed += () => _ = ReplayAsync();
        await ReplayAsync();
        _ = RetryPeriodicallyAsync();
    }

    // Server se může vrátit, aniž by prohlížeč hlásil změnu sítě – fronta se proto zkouší i pravidelně.
    private async Task RetryPeriodicallyAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15), time);
        while (await timer.WaitForNextTickAsync())
        {
            await ReplayAsync();
        }
    }

    public async Task EnqueueAsync(Guid id, ContentDocument document)
    {
        var text = PlainTextExtractor.Extract(null, document);
        var item = new QueueItem(id, document.ToJsonString(), text.Length > 120 ? text[..120] + "…" : text, time.GetUtcNow());
        await (await browser.GetAsync()).InvokeVoidAsync("queuePut", item);
        await RefreshAsync();
    }

    /// <summary>Odešle frontu v pořadí vzniku; položka se odebere až po potvrzení serverem.</summary>
    public async Task ReplayAsync()
    {
        if (items.Count == 0 || !await replayLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            foreach (var item in items.ToList())
            {
                var result = await api.CreateNoteAsync(new CreateNoteRequest(item.Id, JsonDocument.Parse(item.DocumentJson).RootElement));
                if (result.Error is { IsNetworkFailure: true })
                {
                    connectivity.ReportServerReachable(false);
                    return;
                }

                connectivity.ReportServerReachable(true);
                if (!result.IsSuccess)
                {
                    // Položku, kterou server odmítl (např. neplatný dokument), nelze odeslat ani později; zůstane ve frontě
                    // viditelná, aby se obsah neztratil (NFR-4), a zaloguje se.
                    logger.LogWarning("Queued note {NoteId} was rejected with {Code}", item.Id, result.Error!.Code);
                    continue;
                }

                await (await browser.GetAsync()).InvokeVoidAsync("queueRemove", item.Id);
            }
        }
        finally
        {
            replayLock.Release();
            await RefreshAsync();
        }
    }

    private async Task RefreshAsync()
    {
        items = [.. await (await browser.GetAsync()).InvokeAsync<QueueItem[]>("queueList")];
        Changed?.Invoke();
    }

    public sealed record QueueItem(Guid Id, string DocumentJson, string Preview, DateTimeOffset CreatedAtUtc);
}

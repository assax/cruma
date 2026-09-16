using Cruma.Desktop.Storage;
using Cruma.Sync;
using Microsoft.Extensions.Logging;

namespace Cruma.Sync.Client;

/// <summary>Stav synchronizace (SYN-007, FR-27 akc. 3).</summary>
public enum SyncState
{
    Synced,
    Pending,
    Conflict,
    Error,
}

/// <summary>Stav pro UI: stav, důvod, čas poslední úspěšné relace a nutnost aktualizace (FR-37).</summary>
public sealed record SyncStatus(SyncState State, string? Detail, DateTimeOffset? LastSyncedAtUtc, bool UpdateRequired, bool Offline);

/// <summary>
/// Synchronizační engine desktopu (versioning-and-sync-pattern.md §5): handshake → push čekajících změn → pull
/// podle kurzoru → potvrzení. Běží mimo UI vlákno, relace jdou za sebou. Žádná chyba nesmaže ani nepřepíše lokální
/// změny (ERR-002); pod minimální verzí se synchronizace zastaví a změny zůstanou (FR-37 akc. 1, 2).
/// </summary>
public sealed class SyncEngine(LocalNotesStore store, ISyncTransport transport, SyncClientOptions options, TimeProvider time, ILogger<SyncEngine> logger)
    : IAsyncDisposable
{
    private readonly SemaphoreSlim session = new(1, 1);
    private readonly CancellationTokenSource stopping = new();
    private CancellationTokenSource? delayedRequest;
    private Task? loop;

    public SyncStatus Status { get; private set; } = new(SyncState.Pending, null, null, false, false);

    public event Action<SyncStatus>? StatusChanged;

    /// <summary>Spustí synchronizaci na pozadí: hned při startu a pak pravidelně (§5.2).</summary>
    public void Start() => loop ??= Task.Run(() => RunPeriodicallyAsync(stopping.Token));

    /// <summary>Vyžádá synchronizaci po lokální úpravě nebo po návratu připojení; více požadavků se sloučí.</summary>
    public void RequestSync(TimeSpan? delay = null)
    {
        var request = new CancellationTokenSource();
        Interlocked.Exchange(ref delayedRequest, request)?.Cancel();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay ?? options.AfterEditDelay, time, request.Token);
                await SynchronizeAsync(stopping.Token);
            }
            catch (OperationCanceledException)
            {
                // Požadavek nahradil novější nebo se aplikace ukončuje.
            }
        });
    }

    /// <summary>Jedna synchronizační relace. Probíhá-li jiná, počká na ni.</summary>
    public async Task<SyncStatus> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        await session.WaitAsync(cancellationToken);
        try
        {
            var status = await RunSessionAsync(cancellationToken);
            Publish(status);
            return status;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Neočekávaná chyba relace se zaloguje a ukáže jako chyba; čekající změny zůstávají (ERR-002, ERR-003).
            logger.LogError(exception, "Sync session failed");
            var failed = Status with { State = SyncState.Error, Detail = "Synchronizace selhala." };
            Publish(failed);
            return failed;
        }
        finally
        {
            session.Release();
        }
    }

    private async Task<SyncStatus> RunSessionAsync(CancellationToken cancellationToken)
    {
        var handshake = await transport.HandshakeAsync(new HandshakeRequest(options.AppVersion, SyncProtocol.CurrentVersion, options.ClientInstanceId), cancellationToken);
        if (!handshake.IsSuccess)
        {
            return await FailureStatusAsync(handshake.Failure, handshake.MinimumVersion, cancellationToken);
        }

        // Push před pull: server sloučí lokální práci s nejnovějším stavem (§5.1).
        var outgoing = await store.TakeOutgoingChangesAsync(cancellationToken);
        foreach (var batch in outgoing.Chunk(options.PushBatchSize))
        {
            var pushed = await transport.PushAsync(new PushRequest([.. batch.Select(change => change.Change)]), cancellationToken);
            if (!pushed.IsSuccess)
            {
                return await FailureStatusAsync(pushed.Failure, pushed.MinimumVersion, cancellationToken);
            }

            var results = pushed.Value!.Results.ToDictionary(result => result.ChangeId);
            foreach (var change in batch)
            {
                if (results.TryGetValue(change.Change.ChangeId, out var result))
                {
                    await store.ApplyResultAsync(change, result, cancellationToken);
                    if (result.Outcome == ChangeOutcome.Rejected)
                    {
                        logger.LogWarning("Sync change {ChangeId} rejected with {Code}", result.ChangeId, result.ErrorCode);
                    }
                }
            }
        }

        var cursor = await store.GetCursorAsync(cancellationToken);
        var pulledEntries = 0;
        while (true)
        {
            var pulled = await transport.PullAsync(new PullRequest(cursor, options.PullPageSize), cancellationToken);
            if (!pulled.IsSuccess)
            {
                return await FailureStatusAsync(pulled.Failure, pulled.MinimumVersion, cancellationToken);
            }

            await store.ApplyPulledPageAsync(pulled.Value!.Entries, cancellationToken);
            pulledEntries += pulled.Value.Entries.Count;
            cursor = pulled.Value.NextCursor;
            await store.SetCursorAsync(cursor, cancellationToken);
            if (!pulled.Value.HasMore)
            {
                break;
            }
        }

        logger.LogInformation("Sync session finished: pushed {Pushed} changes, pulled {Pulled} entries, cursor {Cursor}", outgoing.Count, pulledEntries, cursor);
        return await SummaryStatusAsync(offline: false, updateRequired: false, lastSynced: time.GetUtcNow(), cancellationToken);
    }

    private async Task<SyncStatus> FailureStatusAsync(TransportFailure failure, string? minimumVersion, CancellationToken cancellationToken)
    {
        switch (failure)
        {
            case TransportFailure.VersionUnsupported:
                logger.LogWarning("Sync stopped: client version {AppVersion} is below minimum {MinimumVersion}", options.AppVersion, minimumVersion);
                return (await SummaryStatusAsync(false, true, Status.LastSyncedAtUtc, cancellationToken)) with
                {
                    State = SyncState.Error,
                    Detail = $"Aplikace je zastaralá – aktualizujte ji (minimum {minimumVersion}). Změny zůstávají uložené.",
                };
            case TransportFailure.Unauthenticated:
                return (await SummaryStatusAsync(false, false, Status.LastSyncedAtUtc, cancellationToken)) with
                {
                    State = SyncState.Error,
                    Detail = "Přihlaste se pro synchronizaci.",
                };
            case TransportFailure.Network:
                return await SummaryStatusAsync(offline: true, updateRequired: false, Status.LastSyncedAtUtc, cancellationToken);
            default:
                return (await SummaryStatusAsync(false, false, Status.LastSyncedAtUtc, cancellationToken)) with
                {
                    State = SyncState.Error,
                    Detail = "Server synchronizaci nedokončil, zkusí se to znovu.",
                };
        }
    }

    private async Task<SyncStatus> SummaryStatusAsync(bool offline, bool updateRequired, DateTimeOffset? lastSynced, CancellationToken cancellationToken)
    {
        var (pending, rejected, conflicts) = await store.GetSyncSummaryAsync(cancellationToken);
        var state = rejected > 0 ? SyncState.Error : conflicts ? SyncState.Conflict : pending > 0 ? SyncState.Pending : SyncState.Synced;
        var detail = state switch
        {
            SyncState.Error => $"Server odmítl {rejected} změn – zůstávají uložené.",
            SyncState.Conflict => "Některé poznámky mají konflikt.",
            SyncState.Pending when offline => "Bez připojení – změny čekají.",
            SyncState.Pending => $"Čeká {pending} změn.",
            _ => offline ? "Bez připojení." : null,
        };
        return new SyncStatus(state, detail, lastSynced, updateRequired, offline);
    }

    private void Publish(SyncStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(status);
    }

    private async Task RunPeriodicallyAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(options.PeriodicInterval, time);
        do
        {
            await SynchronizeAsync(cancellationToken);
        }
        while (await timer.WaitForNextTickAsync(cancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        await stopping.CancelAsync();
        if (loop is not null)
        {
            try
            {
                await loop;
            }
            catch (OperationCanceledException)
            {
                // Očekávané ukončení smyčky.
            }
        }

        stopping.Dispose();
        session.Dispose();
    }
}

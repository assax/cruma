using System.Text.Json;
using System.Text.Json.Serialization;
using Cruma.Content;
using Cruma.Server.Audit;
using Cruma.Server.Infrastructure;
using Cruma.Server.Notes;
using Cruma.Server.Sync.Persistence;
using Cruma.Sync;
using Cruma.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cruma.Server.Sync;

/// <summary>Konfigurace synchronizace (sekce <c>Cruma:Sync</c>).</summary>
public sealed class SyncOptions
{
    public const string Section = "Cruma:Sync";

    /// <summary>Minimální podporovaná verze desktopu (plan.md N-6, FR-37).</summary>
    public string MinimumClientVersion { get; set; } = "0.0.0";

    public int MaxPullEntries { get; set; } = 500;

    internal Version MinimumVersion => Version.Parse(MinimumClientVersion);
}

/// <summary>
/// Synchronizace desktopu (versioning-and-sync-pattern.md §5, server-pattern.md §5): handshake s minimální verzí
/// (SYN-001), push s deduplikací podle identifikátoru změny (SYN-003) přes zápisovou cestu poznámek (N-1)
/// a pull podle kurzoru změnového feedu (N-2).
/// </summary>
internal sealed class SyncService(
    CrumaDbContext db,
    INotesService notes,
    IChangeFeed changeFeed,
    IAuditService audit,
    TimeProvider timeProvider,
    IOptions<SyncOptions> options)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task<HandshakeResponse> HandshakeAsync(HandshakeRequest request, CancellationToken cancellationToken)
    {
        var response = VersionCompatibility.Evaluate(request, options.Value.MinimumVersion);
        await audit.RecordNowAsync(
            new AuditEvent(
                AuditOperations.SyncSession,
                AuditObjectTypes.Device,
                request.ClientInstanceId.ToString(),
                response.Status == HandshakeStatus.Accepted ? AuditResult.Success : AuditResult.Failure,
                new Dictionary<string, string>
                {
                    ["appVersion"] = request.AppVersion,
                    ["protocolVersion"] = request.ProtocolVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                }),
            cancellationToken);
        return response;
    }

    public async Task<PushResponse> PushAsync(PushRequest request, CancellationToken cancellationToken)
    {
        var results = new List<ChangeResult>();
        foreach (var change in request.Changes)
        {
            results.Add(await PushOneAsync(change, cancellationToken));
        }

        return new PushResponse(results);
    }

    public async Task<PullResponse> PullAsync(PullRequest request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.MaxEntries, 1, options.Value.MaxPullEntries);
        var read = await changeFeed.ReadAfterAsync(Math.Max(request.Cursor, 0), take + 1, cancellationToken);
        var page = read.Take(take).ToList();

        // Víc záznamů téže entity v jedné stránce nese stejný aktuální stav – stačí poslední.
        var latest = page.GroupBy(entry => (entry.EntityType, entry.EntityId)).Select(group => group.Last()).OrderBy(entry => entry.Sequence);

        // Poznámky stránky se načtou jedním dotazem (první synchronizace desítek tisíc poznámek, NFR-7).
        var noteIds = latest.Where(entry => entry.EntityType == SyncEntityType.Note).Select(entry => entry.EntityId).ToList();
        var notesById = (await notes.GetNotesAsync(noteIds, cancellationToken)).ToDictionary(note => note.Id);

        var entries = new List<ChangeFeedEntry>();
        foreach (var entry in latest)
        {
            entries.Add(entry.EntityType == SyncEntityType.Note
                ? notesById.TryGetValue(entry.EntityId, out var note)
                    ? new ChangeFeedEntry(entry.Sequence, entry.EntityType, entry.EntityId, note.Version, false, Note: ToPayload(note))
                    : new ChangeFeedEntry(entry.Sequence, entry.EntityType, entry.EntityId, null, true)
                : await ToFeedEntryAsync(entry, cancellationToken));
        }

        return new PullResponse(entries, page.Count > 0 ? page[^1].Sequence : request.Cursor, read.Count > take);
    }

    private async Task<ChangeResult> PushOneAsync(SyncChange change, CancellationToken cancellationToken)
    {
        var processed = await db.Set<ProcessedChangeEntity>().AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.ChangeId == change.ChangeId, cancellationToken);
        if (processed is not null)
        {
            // Opakované odeslání téže změny vrátí původní výsledek bez dalšího účinku (SYN-003).
            return JsonSerializer.Deserialize<ChangeResult>(processed.ResultJson, Json)!;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var result = await ApplyAsync(change, cancellationToken);
        if (result.Outcome == ChangeOutcome.Rejected)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            await audit.RecordNowAsync(
                new AuditEvent(AuditOperations.SyncChangeRejected, change.EntityType.ToString().ToLowerInvariant(), change.EntityId.ToString(), AuditResult.Failure,
                    new Dictionary<string, string> { ["changeId"] = change.ChangeId.ToString(), ["code"] = result.ErrorCode ?? string.Empty }),
                cancellationToken);
            return result;
        }

        db.Set<ProcessedChangeEntity>().Add(new ProcessedChangeEntity
        {
            ChangeId = change.ChangeId,
            ResultJson = JsonSerializer.Serialize(result, Json),
            ProcessedAtUtc = timeProvider.GetUtcNow(),
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<ChangeResult> ApplyAsync(SyncChange change, CancellationToken cancellationToken)
    {
        switch (change.EntityType, change.Operation)
        {
            case (SyncEntityType.Note, SyncOperation.Delete):
                return ToResult(change, await notes.DeleteNotePermanentlyAsync(change.EntityId, cancellationToken));

            case (SyncEntityType.Note, SyncOperation.Upsert) when change.Note is { } payload:
                return await UpsertNoteAsync(change, payload, cancellationToken);

            case (SyncEntityType.Category, SyncOperation.Delete):
                return ToResult(change, await notes.DeleteCategoryAsync(change.EntityId, cancellationToken));

            case (SyncEntityType.Category, SyncOperation.Upsert) when change.Category is { } payload:
                return (await notes.GetCategoryAsync(change.EntityId, cancellationToken)).IsSuccess
                    ? ToResult(change, await notes.RenameCategoryAsync(change.EntityId, payload.Name, cancellationToken))
                    : ToResult(change, await notes.CreateCategoryAsync(change.EntityId, payload.Name, cancellationToken));

            case (SyncEntityType.Tag, SyncOperation.Delete):
                return ToResult(change, await notes.DeleteTagAsync(change.EntityId, cancellationToken));

            case (SyncEntityType.Tag, SyncOperation.Upsert) when change.Tag is { } payload:
                return (await notes.GetTagAsync(change.EntityId, cancellationToken)).IsSuccess
                    ? ToResult(change, await notes.RenameTagAsync(change.EntityId, payload.Name, cancellationToken))
                    : ToResult(change, await notes.CreateTagAsync(change.EntityId, payload.Name, cancellationToken));

            default:
                return Rejected(change, ErrorCodes.ValidationFailed);
        }
    }

    private async Task<ChangeResult> UpsertNoteAsync(SyncChange change, NotePayload payload, CancellationToken cancellationToken)
    {
        ContentDocument document;
        try
        {
            document = ContentDocument.Parse(payload.DocumentJson);
        }
        catch (ContentFormatException)
        {
            return Rejected(change, "document_invalid");
        }

        var metadata = new NoteMetadataInput(payload.Title, payload.CategoryId, payload.TagIds, payload.Color, payload.IsPinned, payload.State);

        if (change.BaseVersion is { } baseVersion)
        {
            return ToResult(change, await notes.SaveNoteAsync(new SaveNoteCommand(change.EntityId, baseVersion, document, metadata, change.ChangedAtUtc), cancellationToken));
        }

        var created = await notes.CreateNoteAsync(new CreateNoteCommand(change.EntityId, document, metadata, change.ChangedAtUtc), cancellationToken);
        if (created.IsSuccess && created.Value.Note.Version > 1)
        {
            // Poznámka už na serveru existuje a vyvíjela se: nová změna se sloučí proti první verzi, ze které klient vycházel.
            return ToResult(change, await notes.SaveNoteAsync(new SaveNoteCommand(change.EntityId, 1, document, metadata, change.ChangedAtUtc), cancellationToken));
        }

        return ToResult(change, created);
    }

    private async Task<ChangeFeedEntry> ToFeedEntryAsync(FeedEntry entry, CancellationToken cancellationToken)
    {
        switch (entry.EntityType)
        {
            case SyncEntityType.Note:
                var note = await notes.GetNoteAsync(entry.EntityId, cancellationToken);
                return note.IsSuccess
                    ? new ChangeFeedEntry(entry.Sequence, entry.EntityType, entry.EntityId, note.Value.Version, false, Note: ToPayload(note.Value))
                    : new ChangeFeedEntry(entry.Sequence, entry.EntityType, entry.EntityId, null, true);
            case SyncEntityType.Category:
                var category = await notes.GetCategoryAsync(entry.EntityId, cancellationToken);
                return category.IsSuccess
                    ? new ChangeFeedEntry(entry.Sequence, entry.EntityType, entry.EntityId, null, false, Category: new CategoryPayload(category.Value.Name, category.Value.IsDefault))
                    : new ChangeFeedEntry(entry.Sequence, entry.EntityType, entry.EntityId, null, true);
            default:
                var tag = await notes.GetTagAsync(entry.EntityId, cancellationToken);
                return tag.IsSuccess
                    ? new ChangeFeedEntry(entry.Sequence, entry.EntityType, entry.EntityId, null, false, Tag: new TagPayload(tag.Value.Name))
                    : new ChangeFeedEntry(entry.Sequence, entry.EntityType, entry.EntityId, null, true);
        }
    }

    private static NotePayload ToPayload(NoteView note) =>
        new(note.Metadata.Title, note.Metadata.CategoryId, [.. note.Metadata.TagIds], note.Metadata.Color, note.Metadata.IsPinned,
            note.Metadata.State, note.Document.ToJsonString());

    private static ChangeResult ToResult(SyncChange change, ServiceResult result)
    {
        if (!result.IsSuccess)
        {
            var rule = result.Error!.Extensions?.GetValueOrDefault("rule") as string;
            return Rejected(change, rule ?? result.Error.Code);
        }

        return result is ServiceResult<NoteWriteResult> { Value: var written }
            ? new ChangeResult(
                change.ChangeId,
                written.Outcome switch
                {
                    NoteMergeOutcome.Merged => ChangeOutcome.Merged,
                    NoteMergeOutcome.Conflict => ChangeOutcome.Conflict,
                    _ => ChangeOutcome.Applied,
                },
                written.Note.Version,
                OverwrittenFields: [.. written.OverwrittenValues.Select(value => new OverwrittenField(value.Field, value.OverwrittenText, value.WinningText))])
            : new ChangeResult(change.ChangeId, ChangeOutcome.Applied, null);
    }

    private static ChangeResult Rejected(SyncChange change, string code) => new(change.ChangeId, ChangeOutcome.Rejected, null, code);
}

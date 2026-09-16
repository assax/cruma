using System.Text.Json;
using System.Text.Json.Serialization;
using Cruma.Notes;

namespace Cruma.Sync.Tests;

/// <summary>Kontrakty protokolu projdou serializací JSON beze ztráty (webová výchozí nastavení, výčty jako text).</summary>
public class ContractSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly DateTimeOffset ChangedAt = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public void PushRequest_NoteChange_RoundTrips()
    {
        var request = new PushRequest(
        [
            new SyncChange(
                Guid.Parse("00000000-0000-0000-0000-0000000000f1"),
                SyncEntityType.Note,
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                SyncOperation.Upsert,
                BaseVersion: 3,
                ChangedAt,
                Note: new NotePayload("Název", Guid.Parse("00000000-0000-0000-0000-0000000000d1"),
                    [Guid.Parse("00000000-0000-0000-0000-0000000000a1")], "yellow", true, NoteState.Archived,
                    """{"type":"doc","schemaVersion":1,"content":[]}""")),
        ]);

        var json = JsonSerializer.Serialize(request, Options);
        var roundTrip = JsonSerializer.Deserialize<PushRequest>(json, Options)!;

        var original = request.Changes[0];
        var restored = roundTrip.Changes[0];
        Assert.That(restored with { Note = null }, Is.EqualTo(original with { Note = null }));
        Assert.That(restored.Note!.TagIds, Is.EqualTo(original.Note!.TagIds));
        Assert.That(restored.Note with { TagIds = [] }, Is.EqualTo(original.Note with { TagIds = [] }));
        Assert.That(json, Does.Contain("\"state\":\"Archived\""));
    }

    [Test]
    public void PushResponse_AllOutcomes_RoundTrip()
    {
        var response = new PushResponse(
        [
            new ChangeResult(Guid.Parse("00000000-0000-0000-0000-0000000000f1"), ChangeOutcome.Applied, 4),
            new ChangeResult(Guid.Parse("00000000-0000-0000-0000-0000000000f2"), ChangeOutcome.Merged, 7, OverwrittenFields: [new OverwrittenField("title", "web", "desktop")]),
            new ChangeResult(Guid.Parse("00000000-0000-0000-0000-0000000000f3"), ChangeOutcome.Conflict, 8),
            new ChangeResult(Guid.Parse("00000000-0000-0000-0000-0000000000f4"), ChangeOutcome.Rejected, null, "note_not_found"),
        ]);

        var roundTrip = JsonSerializer.Deserialize<PushResponse>(JsonSerializer.Serialize(response, Options), Options)!;

        Assert.That(roundTrip.Results.Select(result => (result.ChangeId, result.Outcome, result.Version, result.ErrorCode)),
            Is.EqualTo(response.Results.Select(result => (result.ChangeId, result.Outcome, result.Version, result.ErrorCode))));
        Assert.That(roundTrip.Results[1].OverwrittenFields, Is.EqualTo(response.Results[1].OverwrittenFields));
    }

    [Test]
    public void PullResponse_FeedEntries_RoundTrip()
    {
        var response = new PullResponse(
        [
            new ChangeFeedEntry(11, SyncEntityType.Category, Guid.Parse("00000000-0000-0000-0000-0000000000c1"), null, false, Category: new CategoryPayload("Práce", false)),
            new ChangeFeedEntry(12, SyncEntityType.Tag, Guid.Parse("00000000-0000-0000-0000-0000000000a1"), null, true),
        ],
            NextCursor: 12,
            HasMore: false);

        var roundTrip = JsonSerializer.Deserialize<PullResponse>(JsonSerializer.Serialize(response, Options), Options)!;

        Assert.That(roundTrip.Entries, Is.EqualTo(response.Entries));
        Assert.That(roundTrip.NextCursor, Is.EqualTo(12));
    }

    [Test]
    public void HandshakeRejected_RoundTrips()
    {
        var response = new HandshakeResponse(HandshakeStatus.Rejected, 1, "1.4.0", SyncProtocol.ClientVersionUnsupported);

        var roundTrip = JsonSerializer.Deserialize<HandshakeResponse>(JsonSerializer.Serialize(response, Options), Options);

        Assert.That(roundTrip, Is.EqualTo(response));
    }
}

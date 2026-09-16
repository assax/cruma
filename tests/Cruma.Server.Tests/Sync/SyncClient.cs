using System.Net;
using System.Net.Http.Json;
using Cruma.Api.Contracts;
using Cruma.Notes;
using Cruma.Server.Sync;
using Cruma.Sync;

namespace Cruma.Server.Tests.Sync;

/// <summary>Volání synchronizačního protokolu jako desktop.</summary>
internal static class SyncClient
{
    public static readonly DateTimeOffset ChangedAt = new(2030, 5, 1, 8, 0, 0, TimeSpan.Zero);

    public static HttpClient Desktop(this CrumaServerFactory factory, Guid userId, string appVersion = "1.0.0")
    {
        var client = factory.CreateClientFor(userId, Guid.Parse("22222222-0000-0000-0000-000000000001")).Http;
        client.DefaultRequestHeaders.Add(SyncModule.AppVersionHeader, appVersion);
        client.DefaultRequestHeaders.Add(SyncModule.ProtocolVersionHeader, SyncProtocol.CurrentVersion.ToString());
        return client;
    }

    public static async Task<PushResponse> PushAsync(this HttpClient client, params SyncChange[] changes) =>
        await Api.ReadAsync<PushResponse>(await client.PostAsJsonAsync("/api/sync/v1/push", new PushRequest(changes), Api.Json), HttpStatusCode.OK);

    public static async Task<PullResponse> PullAsync(this HttpClient client, long cursor, int max = 100) =>
        await Api.ReadAsync<PullResponse>(await client.PostAsJsonAsync("/api/sync/v1/pull", new PullRequest(cursor, max), Api.Json), HttpStatusCode.OK);

    public static SyncChange NoteUpsert(Guid noteId, long? baseVersion, Guid categoryId, params string[] paragraphs) =>
        NoteUpsert(Api.NewId(), noteId, baseVersion, categoryId, paragraphs);

    public static SyncChange NoteUpsert(Guid changeId, Guid noteId, long? baseVersion, Guid categoryId, params string[] paragraphs) =>
        new(changeId, SyncEntityType.Note, noteId, SyncOperation.Upsert, baseVersion, ChangedAt,
            Note: new NotePayload(null, categoryId, [], null, false, NoteState.Active, Api.Doc(paragraphs).GetRawText()));

    public static async Task<Guid> DefaultCategoryIdAsync(this CrumaServerFactory factory, Guid userId) =>
        (await factory.CreateClientFor(userId).DefaultCategoryAsync()).Id;

    public static string Text(NotePayload payload) =>
        Api.Text(new NoteDto(Guid.Empty, 0, null, Guid.Empty, [], null, false, NoteState.Active, false,
            System.Text.Json.JsonDocument.Parse(payload.DocumentJson).RootElement, default, default));
}

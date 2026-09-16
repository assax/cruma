using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cruma.Api.Contracts;
using Cruma.Content;
using Cruma.Notes;

namespace Cruma.Server.Tests;

/// <summary>Pomocné volání REST API v1 v testech.</summary>
public static class Api
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static int noteCounter;

    /// <summary>Deterministický identifikátor pro entity vytvořené klientem v rámci běhu testů.</summary>
    public static Guid NewId() => new(Interlocked.Increment(ref noteCounter), 0x7000, 0x4000, [0, 0, 0, 0, 0, 0, 0, 1]);

    /// <summary>Dokument z odstavců <c>id:text</c>.</summary>
    public static JsonElement Doc(params string[] paragraphs) =>
        JsonSerializer.SerializeToElement(ContentDocument.Create(paragraphs.Select(Paragraph)).ToJson());

    public static ContentBlock Paragraph(string notation)
    {
        var separator = notation.IndexOf(':');
        var id = notation[..separator];
        return ContentBlock.FromJson(new JsonObject
        {
            ["type"] = ContentTypes.Paragraph,
            ["attrs"] = new JsonObject { ["id"] = id },
            ["content"] = new JsonArray(new JsonObject { ["type"] = ContentTypes.Text, ["text"] = notation[(separator + 1)..] }),
        });
    }

    /// <summary>Odstavce dokumentu ve tvaru <c>id:text</c>, konflikt jako <c>id:!current|incoming</c>.</summary>
    public static string Text(NoteDto note) =>
        string.Join(' ', ContentDocument.Parse(note.Document.GetRawText()).Blocks.Select(block =>
            ConflictBlock.TryRead(block) is { } conflict
                ? $"{block.Id}:!{PlainTextExtractor.Extract(null, ContentDocument.Create([conflict.Current]))}|{PlainTextExtractor.Extract(null, ContentDocument.Create([conflict.Incoming]))}"
                : $"{block.Id}:{PlainTextExtractor.Extract(null, ContentDocument.Create([block]))}"));

    public static async Task<NoteWriteResponse> CreateNoteAsync(this UserClient client, params string[] paragraphs) =>
        await client.CreateNoteAsync(NewId(), paragraphs);

    public static async Task<NoteWriteResponse> CreateNoteAsync(this UserClient client, Guid id, params string[] paragraphs)
    {
        var response = await client.Http.PostAsJsonAsync("/api/v1/notes", new CreateNoteRequest(id, Doc(paragraphs)), Json);
        return await ReadAsync<NoteWriteResponse>(response, HttpStatusCode.Created);
    }

    public static Task<HttpResponseMessage> PutNoteAsync(this UserClient client, NoteDto note, long baseVersion, JsonElement document, string? title = null, IReadOnlyList<Guid>? tagIds = null, Guid? categoryId = null, bool? pinned = null) =>
        client.Http.PutAsJsonAsync(
            $"/api/v1/notes/{note.Id}",
            new UpdateNoteRequest(baseVersion, title ?? note.Title, categoryId ?? note.CategoryId, tagIds ?? note.TagIds, note.Color, pinned ?? note.IsPinned, document),
            Json);

    public static async Task<NoteWriteResponse> UpdateNoteAsync(this UserClient client, NoteDto note, long baseVersion, params string[] paragraphs) =>
        await ReadAsync<NoteWriteResponse>(await client.PutNoteAsync(note, baseVersion, Doc(paragraphs)), HttpStatusCode.OK);

    public static async Task<NoteDto> GetNoteAsync(this UserClient client, Guid id) =>
        await ReadAsync<NoteDto>(await client.Http.GetAsync($"/api/v1/notes/{id}"), HttpStatusCode.OK);

    public static async Task<T> GetAsync<T>(this UserClient client, string url) =>
        await ReadAsync<T>(await client.Http.GetAsync(url), HttpStatusCode.OK);

    public static async Task<NoteWriteResponse> PostActionAsync(this UserClient client, Guid noteId, string action) =>
        await ReadAsync<NoteWriteResponse>(await client.Http.PostAsync($"/api/v1/notes/{noteId}/{action}", null), HttpStatusCode.OK);

    public static async Task<CategoryDto> CreateCategoryAsync(this UserClient client, string name) =>
        await ReadAsync<CategoryDto>(await client.Http.PostAsJsonAsync("/api/v1/categories", new CreateCategoryRequest(NewId(), name), Json), HttpStatusCode.Created);

    public static async Task<TagDto> CreateTagAsync(this UserClient client, string name) =>
        await ReadAsync<TagDto>(await client.Http.PostAsJsonAsync("/api/v1/tags", new CreateTagRequest(NewId(), name), Json), HttpStatusCode.Created);

    public static async Task<CategoryDto> DefaultCategoryAsync(this UserClient client) =>
        (await client.GetAsync<PagedResponse<CategoryDto>>("/api/v1/categories")).Items.Single(category => category.IsDefault);

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(expected), body);
        return JsonSerializer.Deserialize<T>(body, Json)!;
    }

    /// <summary>Ověří ProblemDetails s kódem a vrátí tělo.</summary>
    public static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code, string? rule = null)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(status), body);
        var json = JsonDocument.Parse(body).RootElement;
        Assert.That(json.GetProperty("code").GetString(), Is.EqualTo(code), body);
        if (rule is not null)
        {
            Assert.That(json.GetProperty("rule").GetString(), Is.EqualTo(rule), body);
        }

        return json;
    }

    public static NoteState State(this NoteWriteResponse response) => response.Note.State;
}

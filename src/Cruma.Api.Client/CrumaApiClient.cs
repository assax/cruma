using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cruma.Api.Contracts;
using Cruma.Notes;

namespace Cruma.Api.Client;

/// <summary>
/// Typovaný klient REST API v1 pro tenké klienty (DEP-006, API-004). ProblemDetails převádí na <see cref="ApiError"/>,
/// nedostupnou síť na <see cref="ApiError.NetworkUnavailable"/> – nic nevyhazuje kvůli očekávaným selháním.
/// </summary>
public sealed class CrumaApiClient(HttpClient http)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // Poznámky

    public Task<ApiResult<NoteWriteResponse>> CreateNoteAsync(CreateNoteRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<NoteWriteResponse>(HttpMethod.Post, "api/v1/notes", request, cancellationToken);

    public Task<ApiResult<PagedResponse<NoteDto>>> ListNotesAsync(NoteState state, Guid? categoryId, Guid? tagId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResponse<NoteDto>>(HttpMethod.Get,
            $"api/v1/notes?state={state}&page={page}&pageSize={pageSize}{Optional("categoryId", categoryId)}{Optional("tagId", tagId)}", null, cancellationToken);

    public Task<ApiResult<NoteDto>> GetNoteAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<NoteDto>(HttpMethod.Get, $"api/v1/notes/{id}", null, cancellationToken);

    public Task<ApiResult<NoteWriteResponse>> UpdateNoteAsync(Guid id, UpdateNoteRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<NoteWriteResponse>(HttpMethod.Put, $"api/v1/notes/{id}", request, cancellationToken);

    public Task<ApiResult<NoteWriteResponse>> ArchiveNoteAsync(Guid id, CancellationToken cancellationToken = default) => ActionAsync(id, "archive", cancellationToken);

    public Task<ApiResult<NoteWriteResponse>> UnarchiveNoteAsync(Guid id, CancellationToken cancellationToken = default) => ActionAsync(id, "unarchive", cancellationToken);

    public Task<ApiResult<NoteWriteResponse>> TrashNoteAsync(Guid id, CancellationToken cancellationToken = default) => ActionAsync(id, "trash", cancellationToken);

    public Task<ApiResult<NoteWriteResponse>> RestoreNoteAsync(Guid id, CancellationToken cancellationToken = default) => ActionAsync(id, "restore", cancellationToken);

    public Task<ApiResult> DeleteNotePermanentlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/v1/notes/{id}", null, cancellationToken);

    public Task<ApiResult<NoteWriteResponse>> ResolveConflictAsync(Guid id, string blockId, ResolveConflictRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<NoteWriteResponse>(HttpMethod.Post, $"api/v1/notes/{id}/conflicts/{Uri.EscapeDataString(blockId)}/resolve", request, cancellationToken);

    public Task<ApiResult<PagedResponse<NoteDto>>> SearchAsync(string query, NoteState? state, int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResponse<NoteDto>>(HttpMethod.Get,
            $"api/v1/search?q={Uri.EscapeDataString(query)}&page={page}&pageSize={pageSize}{(state is null ? string.Empty : $"&state={state}")}", null, cancellationToken);

    // Kategorie a štítky

    public Task<ApiResult<PagedResponse<CategoryDto>>> ListCategoriesAsync(int page = 1, int pageSize = 200, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResponse<CategoryDto>>(HttpMethod.Get, $"api/v1/categories?page={page}&pageSize={pageSize}", null, cancellationToken);

    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<CategoryDto>(HttpMethod.Post, "api/v1/categories", request, cancellationToken);

    public Task<ApiResult<CategoryDto>> RenameCategoryAsync(Guid id, string name, CancellationToken cancellationToken = default) =>
        SendAsync<CategoryDto>(HttpMethod.Put, $"api/v1/categories/{id}", new RenameRequest(name), cancellationToken);

    public Task<ApiResult> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/v1/categories/{id}", null, cancellationToken);

    public Task<ApiResult<PagedResponse<TagDto>>> ListTagsAsync(int page = 1, int pageSize = 200, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResponse<TagDto>>(HttpMethod.Get, $"api/v1/tags?page={page}&pageSize={pageSize}", null, cancellationToken);

    public Task<ApiResult<TagDto>> CreateTagAsync(CreateTagRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<TagDto>(HttpMethod.Post, "api/v1/tags", request, cancellationToken);

    public Task<ApiResult<TagDto>> RenameTagAsync(Guid id, string name, CancellationToken cancellationToken = default) =>
        SendAsync<TagDto>(HttpMethod.Put, $"api/v1/tags/{id}", new RenameRequest(name), cancellationToken);

    public Task<ApiResult> DeleteTagAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/v1/tags/{id}", null, cancellationToken);

    // Přihlášení

    public Task<ApiResult<CurrentUserDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
        SendAsync<CurrentUserDto>(HttpMethod.Get, "auth/me", null, cancellationToken);

    public Task<ApiResult<SignInProvidersDto>> GetSignInProvidersAsync(CancellationToken cancellationToken = default) =>
        SendAsync<SignInProvidersDto>(HttpMethod.Get, "auth/providers", null, cancellationToken);

    public Task<ApiResult<CurrentUserDto>> SignInDevelopmentAsync(string subject, CancellationToken cancellationToken = default) =>
        SendAsync<CurrentUserDto>(HttpMethod.Post, "auth/dev/sign-in", new DevelopmentSignInRequest(subject), cancellationToken);

    public Task<ApiResult> SignOutAsync(CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "auth/sign-out", null, cancellationToken);

    private Task<ApiResult<NoteWriteResponse>> ActionAsync(Guid id, string action, CancellationToken cancellationToken) =>
        SendAsync<NoteWriteResponse>(HttpMethod.Post, $"api/v1/notes/{id}/{action}", null, cancellationToken);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        var (response, error) = await TrySendAsync(method, url, body, cancellationToken);
        if (error is not null)
        {
            return ApiResult<T>.Failure(error);
        }

        using (response)
        {
            if (!response!.IsSuccessStatusCode)
            {
                return ApiResult<T>.Failure(await ReadProblemAsync(response, cancellationToken));
            }

            var value = await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
            return value is null
                ? ApiResult<T>.Failure(new ApiError("invalid_response", response.StatusCode, "Odpověď serveru je prázdná."))
                : ApiResult<T>.Success(value);
        }
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        var (response, error) = await TrySendAsync(method, url, body, cancellationToken);
        if (error is not null)
        {
            return ApiResult.Failure(error);
        }

        using (response)
        {
            return response!.IsSuccessStatusCode ? ApiResult.Success() : ApiResult.Failure(await ReadProblemAsync(response, cancellationToken));
        }
    }

    private async Task<(HttpResponseMessage? Response, ApiError? Error)> TrySendAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        }

        try
        {
            return (await http.SendAsync(request, cancellationToken), null);
        }
        catch (HttpRequestException exception)
        {
            // Nedostupný server nebo síť je očekávaný stav tenkého klienta (offline), ne chyba aplikace.
            return (null, new ApiError(ApiError.NetworkUnavailable, null, exception.Message));
        }
    }

    private static async Task<ApiError> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = problem.RootElement;
            return new ApiError(
                Text(root, "code") ?? CodeFor(response.StatusCode),
                response.StatusCode,
                Text(root, "detail"),
                Text(root, "rule"),
                Text(root, "correlationId"));
        }
        catch (JsonException)
        {
            // Odpověď bez ProblemDetails (např. proxy) – kód podle stavu.
            return new ApiError(CodeFor(response.StatusCode), response.StatusCode, null);
        }
    }

    private static string? Text(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string CodeFor(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => "validation_failed",
        HttpStatusCode.Unauthorized => "unauthenticated",
        HttpStatusCode.Forbidden => "forbidden",
        HttpStatusCode.NotFound => "not_found",
        HttpStatusCode.Conflict => "version_conflict",
        HttpStatusCode.UpgradeRequired => "client_version_unsupported",
        _ => "internal_error",
    };

    private static string Optional(string name, Guid? value) => value is { } id ? $"&{name}={id}" : string.Empty;
}

using System.Text.Json;
using Cruma.Api.Contracts;
using Cruma.Content;
using Cruma.Server.Infrastructure;

namespace Cruma.Server.Notes.Endpoints;

/// <summary>Převod výsledků modulu na kontrakty API (API-004); entity se klientům nikdy neposílají.</summary>
internal static class NoteContractMapping
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public static NoteDto ToDto(NoteView note) =>
        new(note.Id, note.Version, note.Metadata.Title, note.Metadata.CategoryId, [.. note.Metadata.TagIds], note.Metadata.Color,
            note.Metadata.IsPinned, note.Metadata.State, note.HasConflict, ToJsonElement(note.Document), note.CreatedAtUtc, note.UpdatedAtUtc);

    public static NoteWriteResponse ToDto(NoteWriteResult result) =>
        new(
            ToDto(result.Note),
            result.Outcome.ToString().ToLowerInvariant(),
            [.. result.OverwrittenValues.Select(value => new OverwrittenFieldDto(value.Field, value.OverwrittenText, value.WinningText))],
            [.. result.Notices.Select(notice => new MergeNoticeDto(notice.Kind.ToString(), notice.BlockId))]);

    public static CategoryDto ToDto(CategoryView category) => new(category.Id, category.Name, category.IsDefault);

    public static TagDto ToDto(TagView tag) => new(tag.Id, tag.Name);

    public static JsonElement ToJsonElement(ContentDocument document) => JsonSerializer.SerializeToElement(document.ToJson());

    /// <summary>Načte dokument z těla požadavku; neplatný dokument je chyba validace.</summary>
    public static ServiceResult<ContentDocument> ParseDocument(JsonElement json)
    {
        try
        {
            return ContentDocument.Parse(json.GetRawText());
        }
        catch (ContentFormatException exception)
        {
            return ServiceError.Validation(exception.Message, "document_invalid");
        }
        catch (InvalidOperationException)
        {
            // Tělo požadavku s neplatným UTF-8 je chyba klienta, ne serveru.
            return ServiceError.Validation("Dokument není platný text v UTF-8.", "document_invalid");
        }
    }

    public static ServiceResult<ContentBlock> ParseBlock(JsonElement json)
    {
        try
        {
            return ContentBlock.FromJson(System.Text.Json.Nodes.JsonNode.Parse(json.GetRawText()));
        }
        catch (Exception exception) when (exception is ContentFormatException or JsonException or InvalidOperationException)
        {
            return ServiceError.Validation(exception.Message, "block_invalid");
        }
    }

    public static (int Page, int PageSize, int Skip) Paging(int? page, int? pageSize)
    {
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var number = Math.Max(page ?? 1, 1);
        return (number, size, (number - 1) * size);
    }
}

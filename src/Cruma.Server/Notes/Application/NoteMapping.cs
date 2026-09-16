using System.Text.Json;
using System.Text.Json.Serialization;
using Cruma.Content;
using Cruma.Notes;
using Cruma.Server.Infrastructure;
using Cruma.Server.Notes.Persistence;
using Cruma.Versioning;

namespace Cruma.Server.Notes.Application;

/// <summary>Převody mezi entitami modulu a doménovými typy sdílené logiky.</summary>
internal static class NoteMapping
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static VersionSource ToVersionSource(ClientType clientType) => clientType switch
    {
        ClientType.Desktop => VersionSource.Desktop,
        ClientType.Mobile => VersionSource.Mobile,
        _ => VersionSource.Web,
    };

    public static NoteView ToView(NoteEntity entity) =>
        new(entity.Id, entity.CurrentVersion, ToNote(entity), ContentDocument.Parse(entity.Document), entity.HasConflict,
            entity.CreatedAtUtc, entity.UpdatedAtUtc);

    public static Note ToNote(NoteEntity entity) =>
        Note.Restore(entity.Id, entity.Title, entity.CategoryId, entity.TagIds, entity.Color, entity.IsPinned, Enum.Parse<NoteState>(entity.State));

    public static void Apply(NoteEntity entity, Note metadata, ContentDocument document, long version, DateTimeOffset now)
    {
        entity.Title = metadata.Title;
        entity.CategoryId = metadata.CategoryId;
        entity.TagIds = [.. metadata.TagIds];
        entity.Color = metadata.Color;
        entity.IsPinned = metadata.IsPinned;
        entity.State = metadata.State.ToString();
        entity.Document = document.ToJsonString();
        entity.HasConflict = document.Blocks.Any(block => block.Type == ContentTypes.Conflict);
        entity.CurrentVersion = version;
        entity.UpdatedAtUtc = now;
    }

    public static NoteVersion ToVersion(NoteVersionEntity entity) =>
        NoteVersion.FromStored(
            entity.Number,
            entity.BaseNumber,
            entity.CreatedAtUtc,
            Enum.Parse<VersionSource>(entity.Source),
            ContentDocument.Parse(entity.Document),
            JsonSerializer.Deserialize<StoredMetadata>(entity.Metadata, Json)!.ToNote(entity.NoteId),
            JsonSerializer.Deserialize<List<OverwrittenValue>>(entity.OverwrittenValues, Json) ?? []);

    public static NoteVersionEntity ToEntity(NoteVersion version, IReadOnlyList<MergeNotice> notices, Guid? clientInstanceId) => new()
    {
        NoteId = version.NoteId,
        Number = version.Number,
        BaseNumber = version.BaseNumber,
        Source = version.Source.ToString(),
        ClientInstanceId = clientInstanceId,
        CreatedAtUtc = version.CreatedAtUtc,
        UpdatedAtUtc = version.CreatedAtUtc,
        Document = version.Document.ToJsonString(),
        Metadata = SerializeMetadata(version.Metadata),
        OverwrittenValues = JsonSerializer.Serialize(version.OverwrittenValues, Json),
        Notices = JsonSerializer.Serialize(notices, Json),
        HasConflict = version.HasConflict,
    };

    public static string SerializeMetadata(Note metadata) => JsonSerializer.Serialize(StoredMetadata.From(metadata), Json);

    public static string SerializeNotices(IReadOnlyList<MergeNotice> notices) => JsonSerializer.Serialize(notices, Json);

    /// <summary>Snímek vlastností uložený s verzí.</summary>
    private sealed record StoredMetadata(string? Title, Guid CategoryId, List<Guid> TagIds, string? Color, bool IsPinned, NoteState State)
    {
        public static StoredMetadata From(Note note) => new(note.Title, note.CategoryId, [.. note.TagIds], note.Color, note.IsPinned, note.State);

        public Note ToNote(Guid noteId) => Note.Restore(noteId, Title, CategoryId, TagIds, Color, IsPinned, State);
    }
}

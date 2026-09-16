using Cruma.Api.Contracts;
using Cruma.Content;
using Cruma.Notes;
using Cruma.Server.Infrastructure;
using static Cruma.Server.Notes.Endpoints.NoteContractMapping;

namespace Cruma.Server.Notes.Endpoints;

/// <summary>
/// REST API v1 poznámek, kategorií, štítků a vyhledávání (API-001). Endpointy jen převedou vstup, zavolají jednu
/// operaci služby a výsledek převedou na odpověď (API-002). Seznamy jsou stránkované (API-005).
/// </summary>
internal static class NotesEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        MapNotes(api.MapGroup("/notes"));
        MapCategories(api.MapGroup("/categories"));
        MapTags(api.MapGroup("/tags"));

        api.MapGet("/search", async (string? q, NoteState? state, int? page, int? pageSize, INotesService notes, CancellationToken cancellationToken) =>
        {
            var paging = Paging(page, pageSize);
            var found = await notes.SearchNotesAsync(q, state, paging.Skip, paging.PageSize, cancellationToken);
            return TypedResults.Ok(new PagedResponse<NoteDto>([.. found.Items.Select(ToDto)], paging.Page, paging.PageSize, found.TotalCount));
        });
    }

    private static void MapNotes(RouteGroupBuilder notes)
    {
        notes.MapPost("/", async (CreateNoteRequest request, INotesService service, CancellationToken cancellationToken) =>
        {
            var document = ParseDocument(request.Document);
            if (!document.IsSuccess)
            {
                return ProblemResults.Problem(document.Error!);
            }

            var result = await service.CreateNoteAsync(new CreateNoteCommand(request.Id, document.Value), cancellationToken);
            return result.ToHttp(value => TypedResults.Created($"/api/v1/notes/{value.Note.Id}", ToDto(value)));
        });

        notes.MapGet("/", async (NoteState? state, Guid? categoryId, Guid? tagId, int? page, int? pageSize, INotesService service, CancellationToken cancellationToken) =>
        {
            var paging = Paging(page, pageSize);
            var list = await service.ListNotesAsync(new NoteListQuery(state ?? NoteState.Active, categoryId, tagId, paging.Skip, paging.PageSize), cancellationToken);
            return TypedResults.Ok(new PagedResponse<NoteDto>([.. list.Items.Select(ToDto)], paging.Page, paging.PageSize, list.TotalCount));
        });

        notes.MapGet("/{id:guid}", async (Guid id, INotesService service, CancellationToken cancellationToken) =>
            (await service.GetNoteAsync(id, cancellationToken)).ToHttp(value => TypedResults.Ok(ToDto(value))));

        notes.MapPut("/{id:guid}", async (Guid id, UpdateNoteRequest request, INotesService service, CancellationToken cancellationToken) =>
        {
            var document = ParseDocument(request.Document);
            if (!document.IsSuccess)
            {
                return ProblemResults.Problem(document.Error!);
            }

            var metadata = new NoteMetadataInput(request.Title, request.CategoryId, request.TagIds, request.Color, request.IsPinned, State: null);
            var result = await service.SaveNoteAsync(new SaveNoteCommand(id, request.BaseVersion, document.Value, metadata), cancellationToken);
            return result.ToHttp(value => TypedResults.Ok(ToDto(value)));
        });

        notes.MapPost("/{id:guid}/archive", (Guid id, INotesService service, CancellationToken cancellationToken) => ChangeState(id, NoteStateChange.Archive, service, cancellationToken));
        notes.MapPost("/{id:guid}/unarchive", (Guid id, INotesService service, CancellationToken cancellationToken) => ChangeState(id, NoteStateChange.Unarchive, service, cancellationToken));
        notes.MapPost("/{id:guid}/trash", (Guid id, INotesService service, CancellationToken cancellationToken) => ChangeState(id, NoteStateChange.Trash, service, cancellationToken));
        notes.MapPost("/{id:guid}/restore", (Guid id, INotesService service, CancellationToken cancellationToken) => ChangeState(id, NoteStateChange.Restore, service, cancellationToken));

        notes.MapDelete("/{id:guid}", async (Guid id, INotesService service, CancellationToken cancellationToken) =>
            (await service.DeleteNotePermanentlyAsync(id, cancellationToken)).ToHttp(() => TypedResults.NoContent()));

        notes.MapPost("/{id:guid}/conflicts/{blockId}/resolve", async (Guid id, string blockId, ResolveConflictRequest request, INotesService service, CancellationToken cancellationToken) =>
        {
            ConflictSide? choice = request.Choice?.ToLowerInvariant() switch
            {
                "current" => ConflictSide.Current,
                "incoming" => ConflictSide.Incoming,
                null => null,
                _ => (ConflictSide)(-1),
            };
            if (choice is (ConflictSide)(-1) || (choice is null) == (request.EditedBlock is null))
            {
                return ProblemResults.Problem(ServiceError.Validation("Zadejte buď vybranou variantu, nebo upravený blok.", "conflict_resolution_invalid"));
            }

            ContentBlock? edited = null;
            if (request.EditedBlock is { } editedJson)
            {
                var block = ParseBlock(editedJson);
                if (!block.IsSuccess)
                {
                    return ProblemResults.Problem(block.Error!);
                }

                edited = block.Value;
            }

            var result = await service.ResolveConflictAsync(new ResolveConflictCommand(id, request.BaseVersion, blockId, choice, edited), cancellationToken);
            return result.ToHttp(value => TypedResults.Ok(ToDto(value)));
        });
    }

    private static void MapCategories(RouteGroupBuilder categories)
    {
        categories.MapGet("/", async (int? page, int? pageSize, INotesService service, CancellationToken cancellationToken) =>
        {
            var paging = Paging(page, pageSize);
            var list = await service.ListCategoriesAsync(paging.Skip, paging.PageSize, cancellationToken);
            return TypedResults.Ok(new PagedResponse<CategoryDto>([.. list.Items.Select(ToDto)], paging.Page, paging.PageSize, list.TotalCount));
        });

        categories.MapPost("/", async (CreateCategoryRequest request, INotesService service, CancellationToken cancellationToken) =>
            (await service.CreateCategoryAsync(request.Id, request.Name, cancellationToken))
                .ToHttp(value => TypedResults.Created($"/api/v1/categories/{value.Id}", ToDto(value))));

        categories.MapPut("/{id:guid}", async (Guid id, RenameRequest request, INotesService service, CancellationToken cancellationToken) =>
            (await service.RenameCategoryAsync(id, request.Name, cancellationToken)).ToHttp(value => TypedResults.Ok(ToDto(value))));

        categories.MapDelete("/{id:guid}", async (Guid id, INotesService service, CancellationToken cancellationToken) =>
            (await service.DeleteCategoryAsync(id, cancellationToken)).ToHttp(() => TypedResults.NoContent()));
    }

    private static void MapTags(RouteGroupBuilder tags)
    {
        tags.MapGet("/", async (int? page, int? pageSize, INotesService service, CancellationToken cancellationToken) =>
        {
            var paging = Paging(page, pageSize);
            var list = await service.ListTagsAsync(paging.Skip, paging.PageSize, cancellationToken);
            return TypedResults.Ok(new PagedResponse<TagDto>([.. list.Items.Select(ToDto)], paging.Page, paging.PageSize, list.TotalCount));
        });

        tags.MapPost("/", async (CreateTagRequest request, INotesService service, CancellationToken cancellationToken) =>
            (await service.CreateTagAsync(request.Id, request.Name, cancellationToken))
                .ToHttp(value => TypedResults.Created($"/api/v1/tags/{value.Id}", ToDto(value))));

        tags.MapPut("/{id:guid}", async (Guid id, RenameRequest request, INotesService service, CancellationToken cancellationToken) =>
            (await service.RenameTagAsync(id, request.Name, cancellationToken)).ToHttp(value => TypedResults.Ok(ToDto(value))));

        tags.MapDelete("/{id:guid}", async (Guid id, INotesService service, CancellationToken cancellationToken) =>
            (await service.DeleteTagAsync(id, cancellationToken)).ToHttp(() => TypedResults.NoContent()));
    }

    private static async Task<IResult> ChangeState(Guid id, NoteStateChange change, INotesService service, CancellationToken cancellationToken) =>
        (await service.ChangeNoteStateAsync(id, change, cancellationToken)).ToHttp(value => TypedResults.Ok(ToDto(value)));
}

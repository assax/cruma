using Cruma.Content;
using Cruma.Notes;

namespace Cruma.Versioning;

/// <summary>
/// Vyřešení konfliktu bloku výběrem varianty nebo ruční úpravou (FR-28 akc. 3). Vyřešení vytvoří novou verzi;
/// historie se nepřepisuje (VER-007).
/// </summary>
public static class ConflictResolution
{
    /// <summary>Nahradí blok konfliktu vybranou variantou.</summary>
    public static ContentDocument ChooseVariant(ContentDocument document, string conflictBlockId, ConflictSide side) =>
        Replace(document, conflictBlockId, conflict => conflict.Variant(side));

    /// <summary>Nahradí blok konfliktu ručně upraveným blokem; blok si ponechá identifikátor konfliktu (CNT-002).</summary>
    public static ContentDocument ReplaceWithEdit(ContentDocument document, string conflictBlockId, ContentBlock edited)
    {
        if (edited.Id != conflictBlockId)
        {
            throw new ArgumentException("Upravený blok musí mít identifikátor řešeného konfliktu.", nameof(edited));
        }

        return Replace(document, conflictBlockId, _ => edited);
    }

    /// <summary>Nová verze s vyřešeným dokumentem, vycházející z poslední verze.</summary>
    public static NoteVersion CreateResolvedVersion(
        NoteVersion latest, ContentDocument resolved, Note metadata, VersionSource source, DateTimeOffset createdAtUtc) =>
        latest.Next(resolved, metadata, source, createdAtUtc, latest.Number);

    private static ContentDocument Replace(ContentDocument document, string conflictBlockId, Func<ConflictBlock, ContentBlock> replacement)
    {
        var block = document.FindBlock(conflictBlockId)
            ?? throw new ArgumentException($"Dokument nemá blok '{conflictBlockId}'.", nameof(conflictBlockId));
        var conflict = ConflictBlock.TryRead(block)
            ?? throw new ArgumentException($"Blok '{conflictBlockId}' není konflikt.", nameof(conflictBlockId));

        var chosen = replacement(conflict);
        return document.WithBlocks(document.Blocks.Select(existing => existing.Id == conflictBlockId ? chosen : existing));
    }
}

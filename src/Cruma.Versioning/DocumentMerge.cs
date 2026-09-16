using System.Collections.Immutable;
using Cruma.Content;

namespace Cruma.Versioning;

/// <summary>Výsledek tříbodového merge dokumentu.</summary>
public sealed record DocumentMergeResult(
    ContentDocument Document,
    ImmutableArray<string> ConflictBlockIds,
    ImmutableArray<MergeNotice> Notices)
{
    public bool HasConflict => !ConflictBlockIds.IsEmpty;
}

/// <summary>
/// Tříbodový merge dokumentu po blocích nejvyšší úrovně podle identifikátoru bloku
/// (versioning-and-sync-pattern.md §3.2). Čistá funkce bez I/O, času a náhody (VER-001); konflikt nikdy nevybere
/// jednu variantu potichu (VER-003) a úprava proti smazání se zachová (VER-004).
/// </summary>
public static class DocumentMerge
{
    public static DocumentMergeResult Merge(
        ContentDocument baseDocument,
        ContentDocument current,
        ContentDocument incoming,
        ConflictOrigin currentOrigin,
        ConflictOrigin incomingOrigin)
    {
        var baseBlocks = ById(baseDocument);
        var currentBlocks = ById(current);
        var incomingBlocks = ById(incoming);

        var chosen = new Dictionary<string, ContentBlock>(StringComparer.Ordinal);
        var conflicts = ImmutableArray.CreateBuilder<string>();
        var notices = ImmutableArray.CreateBuilder<MergeNotice>();

        var allIds = current.Blocks.Select(block => block.Id)
            .Concat(incoming.Blocks.Select(block => block.Id))
            .Concat(baseDocument.Blocks.Select(block => block.Id))
            .Distinct(StringComparer.Ordinal);

        foreach (var id in allIds)
        {
            var baseBlock = baseBlocks.GetValueOrDefault(id);
            var currentBlock = currentBlocks.GetValueOrDefault(id);
            var incomingBlock = incomingBlocks.GetValueOrDefault(id);

            var result = MergeBlock(baseBlock, currentBlock, incomingBlock);
            switch (result.Kind)
            {
                case BlockOutcome.Take:
                    chosen[id] = result.Block!;
                    break;
                case BlockOutcome.Conflict:
                    chosen[id] = new ConflictBlock(id, currentBlock!, currentOrigin, incomingBlock!, incomingOrigin).ToBlock();
                    conflicts.Add(id);
                    break;
            }

            if (result.Notice is { } kind)
            {
                notices.Add(new MergeNotice(kind, id));
            }
        }

        var order = MergeOrder(baseDocument, current, incoming, chosen.Keys.ToHashSet(StringComparer.Ordinal));
        return new DocumentMergeResult(
            current.WithBlocks(order.Select(id => chosen[id])),
            conflicts.ToImmutable(),
            notices.ToImmutable());
    }

    private static (BlockOutcome Kind, ContentBlock? Block, MergeNoticeKind? Notice) MergeBlock(
        ContentBlock? baseBlock, ContentBlock? current, ContentBlock? incoming)
    {
        if (baseBlock is null)
        {
            // Blok přidaný na jedné nebo obou stranách.
            return (current, incoming) switch
            {
                (null, null) => (BlockOutcome.Drop, null, null),
                (not null, null) => (BlockOutcome.Take, current, null),
                (null, not null) => (BlockOutcome.Take, incoming, null),
                _ => current.ContentEquals(incoming) ? (BlockOutcome.Take, current, null) : (BlockOutcome.Conflict, null, null),
            };
        }

        var currentChanged = current is not null && !current.ContentEquals(baseBlock);
        var incomingChanged = incoming is not null && !incoming.ContentEquals(baseBlock);

        if (current is null && incoming is null)
        {
            return (BlockOutcome.Drop, null, null);
        }

        if (current is null)
        {
            return incomingChanged
                ? (BlockOutcome.Take, incoming, MergeNoticeKind.BlockRestoredAfterDeletion)
                : (BlockOutcome.Drop, null, null);
        }

        if (incoming is null)
        {
            return currentChanged
                ? (BlockOutcome.Take, current, MergeNoticeKind.BlockKeptAfterDeletion)
                : (BlockOutcome.Drop, null, null);
        }

        return (currentChanged, incomingChanged) switch
        {
            (false, false) => (BlockOutcome.Take, current, null),
            (true, false) => (BlockOutcome.Take, current, null),
            (false, true) => (BlockOutcome.Take, incoming, null),
            _ => current.ContentEquals(incoming) ? (BlockOutcome.Take, current, null) : (BlockOutcome.Conflict, null, null),
        };
    }

    /// <summary>
    /// Pořadí bloků (§3.2): pořadí strany, která přeuspořádala; při přeuspořádání na obou stranách nebo na žádné
    /// pořadí serveru. Bloky druhé strany se vloží za nejbližší předchozí společný blok, za bloky přidané první
    /// stranou. Samotné přeuspořádání konflikt nevytváří.
    /// </summary>
    private static List<string> MergeOrder(ContentDocument baseDocument, ContentDocument current, ContentDocument incoming, HashSet<string> resultIds)
    {
        var baseIds = baseDocument.Blocks.Select(block => block.Id).ToList();
        var currentIds = current.Blocks.Select(block => block.Id).ToList();
        var incomingIds = incoming.Blocks.Select(block => block.Id).ToList();

        var incomingPrimary = IsReordered(baseIds, incomingIds) && !IsReordered(baseIds, currentIds);
        var primary = incomingPrimary ? incomingIds : currentIds;
        var secondary = incomingPrimary ? currentIds : incomingIds;
        var baseSet = baseIds.ToHashSet(StringComparer.Ordinal);
        var secondarySet = secondary.ToHashSet(StringComparer.Ordinal);

        var order = primary.Where(resultIds.Contains).ToList();
        for (var index = 0; index < secondary.Count; index++)
        {
            var id = secondary[index];
            if (!resultIds.Contains(id) || order.Contains(id))
            {
                continue;
            }

            var anchor = secondary.Take(index).LastOrDefault(order.Contains);
            var position = anchor is null ? 0 : order.IndexOf(anchor) + 1;
            while (position < order.Count && !baseSet.Contains(order[position]) && !secondarySet.Contains(order[position]))
            {
                position++;
            }

            order.Insert(position, id);
        }

        return order;
    }

    private static bool IsReordered(List<string> baseIds, List<string> sideIds)
    {
        var sideSet = sideIds.ToHashSet(StringComparer.Ordinal);
        var baseSet = baseIds.ToHashSet(StringComparer.Ordinal);
        return !baseIds.Where(sideSet.Contains).SequenceEqual(sideIds.Where(baseSet.Contains), StringComparer.Ordinal);
    }

    private static Dictionary<string, ContentBlock> ById(ContentDocument document) =>
        document.Blocks.ToDictionary(block => block.Id, StringComparer.Ordinal);

    private enum BlockOutcome
    {
        Take,
        Drop,
        Conflict,
    }
}

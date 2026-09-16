using System.Collections.Immutable;
using Cruma.Notes;

namespace Cruma.Versioning;

/// <summary>Výsledek merge vlastností poznámky.</summary>
public sealed record MetadataMergeResult(Note Metadata, ImmutableArray<OverwrittenValue> OverwrittenValues);

/// <summary>
/// Tříbodový merge vlastností poznámky (versioning-and-sync-pattern.md §3.4, VER-005): štítky se slučují přidáním
/// a odebráním z obou stran; skalární vlastnost změněná na jedné straně se převezme, změněná na obou stranách
/// vyhraje později přijatá (příchozí) a přepsaná hodnota se vrátí k zápisu do verze.
/// </summary>
public static class MetadataMerge
{
    public const string TitleField = "title";
    public const string CategoryField = "category";
    public const string ColorField = "color";
    public const string PinnedField = "pinned";
    public const string StateField = "state";

    public static MetadataMergeResult Merge(Note baseNote, Note current, Note incoming)
    {
        if (current.Id != baseNote.Id || incoming.Id != baseNote.Id)
        {
            throw new ArgumentException("Merge vlastností musí být nad jednou poznámkou.");
        }

        var overwritten = ImmutableArray.CreateBuilder<OverwrittenValue>();

        var title = Scalar(TitleField, baseNote.Title, current.Title, incoming.Title, overwritten);
        var category = Scalar(CategoryField, baseNote.CategoryId, current.CategoryId, incoming.CategoryId, overwritten);
        var color = Scalar(ColorField, baseNote.Color, current.Color, incoming.Color, overwritten);
        var pinned = Scalar(PinnedField, baseNote.IsPinned, current.IsPinned, incoming.IsPinned, overwritten);
        var state = Scalar(StateField, baseNote.State, current.State, incoming.State, overwritten);

        var tags = baseNote.TagIds
            .Union(current.TagIds.Except(baseNote.TagIds))
            .Union(incoming.TagIds.Except(baseNote.TagIds))
            .Except(baseNote.TagIds.Except(current.TagIds))
            .Except(baseNote.TagIds.Except(incoming.TagIds));

        var merged = Note.Restore(baseNote.Id, title, category, tags, color, pinned, state);
        return new MetadataMergeResult(merged, overwritten.ToImmutable());
    }

    private static T Scalar<T>(string field, T baseValue, T current, T incoming, ImmutableArray<OverwrittenValue>.Builder overwritten)
    {
        var comparer = EqualityComparer<T>.Default;
        var currentChanged = !comparer.Equals(current, baseValue);
        var incomingChanged = !comparer.Equals(incoming, baseValue);

        if (currentChanged && incomingChanged && !comparer.Equals(current, incoming))
        {
            overwritten.Add(new OverwrittenValue(field, current?.ToString(), incoming?.ToString()));
            return incoming;
        }

        return incomingChanged ? incoming : current;
    }
}

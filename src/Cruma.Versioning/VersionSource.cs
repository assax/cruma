namespace Cruma.Versioning;

/// <summary>Zdroj verze poznámky (versioning-and-sync-pattern.md §2.2, FR-6 akc. 1).</summary>
public enum VersionSource
{
    Desktop,
    Web,
    Mobile,
    Ai,
    Merge,
    Restore,
}

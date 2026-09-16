namespace Cruma.Versioning;

/// <summary>Druh upozornění, které merge předává uživateli (VER-004).</summary>
public enum MergeNoticeKind
{
    /// <summary>Blok smazaný na serveru byl upraven klientem; úprava je zachována.</summary>
    BlockRestoredAfterDeletion,

    /// <summary>Blok upravený na serveru klient smazal; úprava je zachována.</summary>
    BlockKeptAfterDeletion,

    /// <summary>Poznámku v koši klient upravil; poznámka se vrátila mezi aktivní.</summary>
    NoteRestoredFromTrash,

    /// <summary>Klient přesunul do koše poznámku, která byla mezitím upravena; úprava zůstává v poznámce v koši.</summary>
    NoteTrashedAfterConcurrentEdit,
}

/// <summary>Upozornění z merge; <see cref="BlockId"/> je vyplněné u upozornění na blok.</summary>
public sealed record MergeNotice(MergeNoticeKind Kind, string? BlockId = null);

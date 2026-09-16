namespace Cruma.Versioning;

/// <summary>
/// Skalární vlastnost změněná na obou stranách, kde vyhrála později přijatá změna. Přepsaná hodnota zůstává
/// v záznamu verze a stav synchronizace přepsání ohlásí (VER-005, FR-28 akc. 6).
/// </summary>
public sealed record OverwrittenValue(string Field, string? OverwrittenText, string? WinningText);

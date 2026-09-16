namespace Cruma.Content;

/// <summary>Dokument neodpovídá základnímu tvaru schématu (není JSON objekt dokumentu, chybí verze, blok bez ID…).</summary>
public sealed class ContentFormatException(string message) : Exception(message);

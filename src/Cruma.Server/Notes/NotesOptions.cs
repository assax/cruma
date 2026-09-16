namespace Cruma.Server.Notes;

/// <summary>Konfigurace modulu poznámek (sekce <c>Cruma:Notes</c>).</summary>
public sealed class NotesOptions
{
    public const string Section = "Cruma:Notes";

    /// <summary>
    /// Interval nečinnosti, do kterého se po sobě jdoucí uložení z téže instance klienta spojují do jedné verze
    /// (plan.md N-3, I1-D-2). Výchozí hodnota 5 minut.
    /// </summary>
    public TimeSpan VersionCoalescingInterval { get; set; } = TimeSpan.FromMinutes(5);
}

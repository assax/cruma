using System.IO;
using System.Text.Json;

namespace Cruma.Desktop;

/// <summary>Nastavení desktopu ze souboru <c>appsettings.json</c> vedle aplikace.</summary>
public sealed class DesktopOptions
{
    /// <summary>Adresa serveru Cruma.</summary>
    public string ServerUrl { get; set; } = "https://localhost:5001/";

    /// <summary>
    /// Adresa feedu instalátoru a aktualizací na serveru Cruma (I1-D-5, FR-37 akc. 3); prázdná = aktualizace vypnuté.
    /// </summary>
    public string? UpdateFeedUrl { get; set; }

    /// <summary>
    /// Vývojové přihlášení přes server (<c>/auth/dev/sign-in</c>) – jen Debug build, dokud nebude přihlášení
    /// systémovým prohlížečem (T-20, SEC-002).
    /// </summary>
    public bool DevelopmentSignIn { get; set; }

    public static DesktopOptions Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var options = File.Exists(path)
            ? JsonSerializer.Deserialize<DesktopOptions>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new DesktopOptions()
            : new DesktopOptions();
#if !DEBUG
        options.DevelopmentSignIn = false;
#endif
        if (!options.ServerUrl.EndsWith('/'))
        {
            options.ServerUrl += "/";
        }

        return options;
    }
}

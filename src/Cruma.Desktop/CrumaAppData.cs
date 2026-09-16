using System.IO;

namespace Cruma.Desktop;

/// <summary>
/// Datová složka desktopu. Vývojový (Debug) build má vlastní složku <c>%LOCALAPPDATA%\Cruma\dev\</c>,
/// aby nikdy nesahal na data nainstalované verze (PER-006).
/// </summary>
public sealed record CrumaAppData(string Root, bool IsDevelopmentBuild)
{
    public string LogDirectory => Path.Combine(Root, "logs");

    public static CrumaAppData Resolve()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var root = Path.Combine(localAppData, "Cruma");
#if DEBUG
        return new CrumaAppData(Path.Combine(root, "dev"), IsDevelopmentBuild: true);
#else
        return new CrumaAppData(root, IsDevelopmentBuild: false);
#endif
    }
}

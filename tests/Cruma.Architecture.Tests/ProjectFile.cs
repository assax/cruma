using System.Xml.Linq;

namespace Cruma.Architecture.Tests;

/// <summary>Reference jednoho projektu přečtené z jeho .csproj souboru.</summary>
public sealed record ProjectFile(string Name, IReadOnlySet<string> ProjectReferences, IReadOnlySet<string> PackageReferences)
{
    public static ProjectFile Load(string csprojPath)
    {
        var document = XDocument.Load(csprojPath);
        var projectReferences = document.Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension(NormalizeSeparators((string?)element.Attribute("Include"))))
            .ToHashSet(StringComparer.Ordinal);
        var packageReferences = document.Descendants("PackageReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        return new ProjectFile(Path.GetFileNameWithoutExtension(csprojPath), projectReferences, packageReferences);
    }

    // Cesty v .csproj jsou s obráceným lomítkem; na Linuxu je Path jinak nerozdělí.
    private static string NormalizeSeparators(string? path) =>
        (path ?? string.Empty).Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
}

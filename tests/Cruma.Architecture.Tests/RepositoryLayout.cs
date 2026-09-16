namespace Cruma.Architecture.Tests;

/// <summary>Cesty v repozitáři, hledané od výstupní složky testu nahoru ke <c>Cruma.slnx</c>.</summary>
public static class RepositoryLayout
{
    public static string Root { get; } = FindRoot();

    public static string TemplatePath =>
        Path.Combine(Root, ".architecture", "architecture-cruma", "shared", "solution-structure-template.md");

    public static IReadOnlyList<ProjectFile> LoadProjects(string folder) =>
        Directory.GetFiles(Path.Combine(Root, folder), "*.csproj", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(ProjectFile.Load)
            .ToList();

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cruma.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Kořen repozitáře (Cruma.slnx) nebyl nad výstupní složkou testu nalezen.");
    }
}

namespace Cruma.Architecture.Tests;

/// <summary>Kontroly pravidel profilu nad projekty řešení. Vrací popisy porušení, prázdný seznam = v pořádku.</summary>
public static class ArchitectureRules
{
    // Kompoziční kořeny, kde smí být NLog (LOG-003).
    private static readonly HashSet<string> CompositionRoots = new(StringComparer.Ordinal)
    {
        "Cruma.Desktop", "Cruma.Web", "Cruma.Server",
    };

    /// <summary>DEP-001..DEP-004, STR-001: produkční projekt referencuje jen projekty povolené v §3 šablony.</summary>
    public static IReadOnlyList<string> FindForbiddenProjectReferences(IEnumerable<ProjectFile> projects, SolutionStructureTemplate template)
    {
        var violations = new List<string>();
        foreach (var project in projects)
        {
            var allowed = template.AllowedReferences.GetValueOrDefault(project.Name) ?? new HashSet<string>();
            violations.AddRange(project.ProjectReferences
                .Where(reference => !allowed.Contains(reference))
                .Order(StringComparer.Ordinal)
                .Select(reference => $"Nepovolená reference: {project.Name} → {reference}"));
        }

        return violations;
    }

    /// <summary>STR-001: řešení obsahuje jen projekty vyjmenované v šabloně.</summary>
    public static IReadOnlyList<string> FindUnlistedProjects(
        IEnumerable<ProjectFile> productionProjects, IEnumerable<ProjectFile> testProjects, SolutionStructureTemplate template) =>
        productionProjects.Where(project => !template.ProductionProjects.Contains(project.Name))
            .Select(project => $"Produkční projekt není v solution-structure-template.md §2: {project.Name}")
            .Concat(testProjects.Where(project => !template.TestProjects.Contains(project.Name))
                .Select(project => $"Testovací projekt není v solution-structure-template.md §4: {project.Name}"))
            .ToList();

    /// <summary>DEP-002: sdílená logika smí mít jen balíčky Microsoft.Extensions.*.Abstractions.</summary>
    public static IReadOnlyList<string> FindForbiddenSharedLogicPackages(IEnumerable<ProjectFile> projects, SolutionStructureTemplate template) =>
        projects.Where(project => template.SharedLogicProjects.Contains(project.Name))
            .SelectMany(project => project.PackageReferences
                .Where(package => !(package.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal)
                    && package.EndsWith(".Abstractions", StringComparison.Ordinal)))
                .Select(package => $"Nepovolený balíček ve sdílené logice: {project.Name} → {package}"))
            .ToList();

    /// <summary>
    /// UI-001, UI-002: zdrojové soubory sdíleného UI nepoužívají JS interop, HttpClient ani platformní API přímo.
    /// Výjimkou pro JS interop je jen Cruma.Ui.Editor.
    /// </summary>
    public static IReadOnlyList<string> FindForbiddenUiApis(IEnumerable<(string Project, string File, string Source)> sources)
    {
        string[] jsApis = ["IJSRuntime", "IJSObjectReference", "IJSInProcessRuntime"];
        string[] platformApis = ["HttpClient", "System.Windows", "Microsoft.Maui", "OperatingSystem.Is"];
        return sources
            .SelectMany(source => (source.Project == "Cruma.Ui.Editor" ? platformApis : jsApis.Concat(platformApis))
                .Where(api => source.Source.Contains(api, StringComparison.Ordinal))
                .Select(api => $"Zakázané API ve sdíleném UI: {source.Project}/{source.File} → {api}"))
            .ToList();
    }

    /// <summary>LOG-003: balíčky NLog jen v kompozičních kořenech.</summary>
    public static IReadOnlyList<string> FindNLogOutsideCompositionRoots(IEnumerable<ProjectFile> projects) =>
        projects.Where(project => !CompositionRoots.Contains(project.Name))
            .SelectMany(project => project.PackageReferences
                .Where(package => package.StartsWith("NLog", StringComparison.Ordinal))
                .Select(package => $"NLog mimo kompoziční kořen: {project.Name} → {package}"))
            .ToList();
}

namespace Cruma.Architecture.Tests;

/// <summary>Architektonický test řešení proti profilu (plan.md N-5; DEP-001..DEP-004, STR-001, LOG-003).</summary>
public class SolutionStructureTests
{
    private SolutionStructureTemplate template = null!;
    private IReadOnlyList<ProjectFile> productionProjects = null!;
    private IReadOnlyList<ProjectFile> testProjects = null!;

    [OneTimeSetUp]
    public void LoadSolution()
    {
        template = SolutionStructureTemplate.Parse(File.ReadAllText(RepositoryLayout.TemplatePath));
        productionProjects = RepositoryLayout.LoadProjects("src");
        testProjects = RepositoryLayout.LoadProjects("tests");
    }

    [Test]
    public void ProjectReferences_InSrc_AreAllowedByTemplate()
    {
        Assert.That(productionProjects, Is.Not.Empty);
        Assert.That(ArchitectureRules.FindForbiddenProjectReferences(productionProjects, template), Is.Empty);
    }

    [Test]
    public void Projects_InSolution_AreListedInTemplate()
    {
        Assert.That(ArchitectureRules.FindUnlistedProjects(productionProjects, testProjects, template), Is.Empty);
    }

    [Test]
    public void SharedLogicPackages_OnlyAbstractions()
    {
        Assert.That(ArchitectureRules.FindForbiddenSharedLogicPackages(productionProjects, template), Is.Empty);
    }

    [Test]
    public void SharedUi_DoesNotUseJsHttpOrPlatformApis()
    {
        var sources = new[] { "Cruma.Ui", "Cruma.Ui.Editor" }
            .SelectMany(project => Directory
                .EnumerateFiles(Path.Combine(RepositoryLayout.Root, "src", project), "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith(".razor", StringComparison.Ordinal) || file.EndsWith(".cs", StringComparison.Ordinal))
                .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !file.Contains("node_modules", StringComparison.Ordinal))
                .Select(file => (project, Path.GetFileName(file), File.ReadAllText(file))))
            .ToList();

        Assert.That(sources, Is.Not.Empty);
        Assert.That(ArchitectureRules.FindForbiddenUiApis(sources), Is.Empty);
    }

    [Test]
    public void NLogPackages_OnlyInCompositionRoots()
    {
        Assert.That(ArchitectureRules.FindNLogOutsideCompositionRoots(productionProjects.Concat(testProjects)), Is.Empty);
    }
}

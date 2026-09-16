namespace Cruma.Architecture.Tests;

/// <summary>Ověřuje, že kontroly porušení skutečně odhalí a pojmenují.</summary>
public class ArchitectureRulesTests
{
    private const string Template = """
        # 2. Projects

        ## 2.1 Shared logic

        | Project | Plan component |
        |---|---|
        | `Cruma.Notes` | C-1 |
        | `Cruma.Content` | C-3 |

        ## 2.2 UI

        | Project | Plan component |
        |---|---|
        | `Cruma.Ui` | C-9 |
        | `Cruma.Web` | C-12 |
        | `Cruma.Server` | C-13 |

        # 3. Allowed references

        ```text
        Cruma.Notes                → (nothing)
        Cruma.Content              → Cruma.Notes
        Cruma.Ui                   → shared logic
        Cruma.Web                  → Cruma.Ui
        Cruma.Server               → shared logic, Cruma.Web (static assets hosting only)
        ```

        # 4. Test projects

        | Project | Tests |
        |---|---|
        | `Cruma.Notes.Tests` | domain |

        # 5. Projects per increment
        """;

    private static readonly SolutionStructureTemplate Parsed = SolutionStructureTemplate.Parse(Template);

    [Test]
    public void Parse_SharedLogicAlias_ExpandsToSection21()
    {
        Assert.That(Parsed.AllowedReferences["Cruma.Ui"], Is.EquivalentTo(new[] { "Cruma.Notes", "Cruma.Content" }));
        Assert.That(Parsed.AllowedReferences["Cruma.Server"], Is.EquivalentTo(new[] { "Cruma.Notes", "Cruma.Content", "Cruma.Web" }));
        Assert.That(Parsed.AllowedReferences["Cruma.Notes"], Is.Empty);
    }

    [Test]
    public void FindForbiddenProjectReferences_ForbiddenReference_NamesBothProjects()
    {
        var projects = new[] { Project("Cruma.Notes", references: ["Cruma.Ui"]) };

        var violations = ArchitectureRules.FindForbiddenProjectReferences(projects, Parsed);

        Assert.That(violations, Is.EqualTo(new[] { "Nepovolená reference: Cruma.Notes → Cruma.Ui" }));
    }

    [Test]
    public void FindForbiddenProjectReferences_AllowedReferences_ReturnsEmpty()
    {
        var projects = new[] { Project("Cruma.Server", references: ["Cruma.Web", "Cruma.Content"]) };

        Assert.That(ArchitectureRules.FindForbiddenProjectReferences(projects, Parsed), Is.Empty);
    }

    [Test]
    public void FindForbiddenProjectReferences_ProjectMissingInSection3_AllowsNothing()
    {
        var projects = new[] { Project("Cruma.Unknown", references: ["Cruma.Notes"]) };

        Assert.That(ArchitectureRules.FindForbiddenProjectReferences(projects, Parsed), Has.Count.EqualTo(1));
    }

    [Test]
    public void FindUnlistedProjects_ProjectNotInTemplate_IsReported()
    {
        var violations = ArchitectureRules.FindUnlistedProjects(
            [Project("Cruma.Notes"), Project("Cruma.Extra")], [Project("Cruma.Extra.Tests")], Parsed);

        Assert.That(violations, Has.Count.EqualTo(2));
        Assert.That(violations, Has.Some.Contains("Cruma.Extra"));
        Assert.That(violations, Has.Some.Contains("Cruma.Extra.Tests"));
    }

    [Test]
    public void FindForbiddenSharedLogicPackages_NonAbstractionsPackage_IsReported()
    {
        var projects = new[]
        {
            Project("Cruma.Notes", packages: ["Microsoft.Extensions.Logging.Abstractions", "Microsoft.EntityFrameworkCore"]),
        };

        Assert.That(ArchitectureRules.FindForbiddenSharedLogicPackages(projects, Parsed),
            Is.EqualTo(new[] { "Nepovolený balíček ve sdílené logice: Cruma.Notes → Microsoft.EntityFrameworkCore" }));
    }

    [Test]
    public void FindNLogOutsideCompositionRoots_NLogInLibrary_IsReported()
    {
        var projects = new[] { Project("Cruma.Server", packages: ["NLog.Web.AspNetCore"]), Project("Cruma.Ui", packages: ["NLog"]) };

        Assert.That(ArchitectureRules.FindNLogOutsideCompositionRoots(projects),
            Is.EqualTo(new[] { "NLog mimo kompoziční kořen: Cruma.Ui → NLog" }));
    }

    private static ProjectFile Project(string name, string[]? references = null, string[]? packages = null) =>
        new(name, (references ?? []).ToHashSet(), (packages ?? []).ToHashSet());
}

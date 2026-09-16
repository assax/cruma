using System.Text.RegularExpressions;

namespace Cruma.Architecture.Tests;

/// <summary>
/// Model šablony <c>solution-structure-template.md</c> z profilu: projekty podle §2 a §4 a povolené reference podle §3.
/// Test čte přímo šablonu, aby jediným zdrojem pravdy zůstal profil (plan.md N-5).
/// </summary>
public sealed partial class SolutionStructureTemplate
{
    private const string SharedLogicAlias = "shared logic";

    private SolutionStructureTemplate(
        IReadOnlySet<string> sharedLogicProjects,
        IReadOnlySet<string> productionProjects,
        IReadOnlySet<string> testProjects,
        IReadOnlyDictionary<string, IReadOnlySet<string>> allowedReferences)
    {
        SharedLogicProjects = sharedLogicProjects;
        ProductionProjects = productionProjects;
        TestProjects = testProjects;
        AllowedReferences = allowedReferences;
    }

    /// <summary>Projekty sdílené logiky (§2.1).</summary>
    public IReadOnlySet<string> SharedLogicProjects { get; }

    /// <summary>Produkční projekty ze všech tabulek §2.</summary>
    public IReadOnlySet<string> ProductionProjects { get; }

    /// <summary>Testovací projekty z tabulky §4.</summary>
    public IReadOnlySet<string> TestProjects { get; }

    /// <summary>Povolené reference podle §3; projekt bez řádku nesmí referencovat nic.</summary>
    public IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedReferences { get; }

    public static SolutionStructureTemplate Parse(string markdown)
    {
        var sharedLogic = ProjectNamesInTables(Section(markdown, "## 2.1", "## 2.2"));
        var production = ProjectNamesInTables(Section(markdown, "# 2. Projects", "# 3. Allowed references"));
        var tests = ProjectNamesInTables(Section(markdown, "# 4. Test projects", "# 5."));
        var allowed = ParseAllowedReferences(Section(markdown, "# 3. Allowed references", "# 4. Test projects"), sharedLogic);

        if (sharedLogic.Count == 0 || production.Count == 0 || tests.Count == 0 || allowed.Count == 0)
        {
            throw new FormatException("Šablona struktury řešení nemá očekávané sekce §2.1, §2, §3 a §4.");
        }

        return new SolutionStructureTemplate(sharedLogic, production, tests, allowed);
    }

    private static string Section(string markdown, string startHeading, string endHeading)
    {
        var start = IndexOfHeading(markdown, startHeading);
        if (start < 0)
        {
            throw new FormatException($"Šablona neobsahuje nadpis '{startHeading}'.");
        }

        var end = IndexOfHeading(markdown[start..], endHeading);
        return end < 0 ? markdown[start..] : markdown.Substring(start, end);
    }

    private static int IndexOfHeading(string text, string heading)
    {
        var match = Regex.Match(text, "^" + Regex.Escape(heading), RegexOptions.Multiline);
        return match.Success ? match.Index : -1;
    }

    private static HashSet<string> ProjectNamesInTables(string section) =>
        TableProjectCell().Matches(section)
            .SelectMany(match => BacktickedName().Matches(match.Groups["cell"].Value))
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static Dictionary<string, IReadOnlySet<string>> ParseAllowedReferences(string section, IReadOnlySet<string> sharedLogic)
    {
        var block = CodeBlock().Match(section);
        if (!block.Success)
        {
            throw new FormatException("Sekce §3 šablony neobsahuje blok s povolenými referencemi.");
        }

        var result = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);
        foreach (var line in block.Groups["body"].Value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('→', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                throw new FormatException($"Řádek povolených referencí nemá tvar 'projekt → reference': {line}");
            }

            var targets = ExpandNames(parts[1], sharedLogic);
            foreach (var source in ExpandNames(parts[0], sharedLogic))
            {
                result[source] = targets;
            }
        }

        return result;
    }

    // Rozvine seznam jmen oddělených čárkou; "(nothing)" je prázdný seznam, "shared logic" jsou projekty §2.1,
    // závorka za jménem je jen vysvětlivka.
    private static HashSet<string> ExpandNames(string list, IReadOnlySet<string> sharedLogic)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var item = Parenthesis().Replace(raw, string.Empty).Trim();
            if (item.Length == 0)
            {
                continue;
            }

            if (item == SharedLogicAlias)
            {
                names.UnionWith(sharedLogic);
            }
            else
            {
                names.Add(item);
            }
        }

        return names;
    }

    [GeneratedRegex(@"^\|\s*(?<cell>`[^|]*`)\s*\|", RegexOptions.Multiline)]
    private static partial Regex TableProjectCell();

    [GeneratedRegex(@"`(?<name>Cruma\.[A-Za-z.]+)`")]
    private static partial Regex BacktickedName();

    [GeneratedRegex(@"```text\s*\n(?<body>.*?)```", RegexOptions.Singleline)]
    private static partial Regex CodeBlock();

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex Parenthesis();
}

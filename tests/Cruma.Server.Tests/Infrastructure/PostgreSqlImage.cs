using System.Text.RegularExpressions;

namespace Cruma.Server.Tests.Infrastructure;

/// <summary>
/// Image PostgreSQL pro testy. Čte se z <c>deploy/compose.yaml</c>, aby testy běžely nad stejnou verzí jako vývoj
/// a produkce (OPS-004, TST-003).
/// </summary>
public static partial class PostgreSqlImage
{
    public static string FromCompose()
    {
        var composePath = Path.Combine(FindRepositoryRoot(), "deploy", "compose.yaml");
        var match = PostgresImage().Match(File.ReadAllText(composePath));
        if (!match.Success)
        {
            throw new InvalidOperationException($"V {composePath} chybí image PostgreSQL s pevnou verzí.");
        }

        return match.Groups["image"].Value;
    }

    private static string FindRepositoryRoot()
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

    [GeneratedRegex(@"^\s*image:\s*(?<image>\S*postgres:\d+\.\d+\S*)\s*$", RegexOptions.Multiline)]
    private static partial Regex PostgresImage();
}

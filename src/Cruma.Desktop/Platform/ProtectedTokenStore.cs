using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cruma.Desktop.Platform;

/// <summary>
/// Úložiště přihlašovacího tokenu chráněné DPAPI pro aktuálního uživatele Windows (SEC-003, desktop-pattern.md §1).
/// Soubor leží v datové složce aplikace, nikdy v lokální databázi (PER-005).
/// </summary>
public sealed class ProtectedTokenStore(CrumaAppData appData)
{
    private static readonly byte[] Entropy = "Cruma.Desktop.Token.v1"u8.ToArray();

    private string FilePath => Path.Combine(appData.Root, "session.token");

    public void Save(string token)
    {
        Directory.CreateDirectory(appData.Root);
        var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, protectedBytes);
    }

    public string? Load()
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(FilePath), Entropy, DataProtectionScope.CurrentUser));
        }
        catch (CryptographicException)
        {
            // Token jiného uživatele Windows nebo poškozený soubor – chová se jako odhlášení, lokální data zůstávají.
            Clear();
            return null;
        }
    }

    public void Clear()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }
}

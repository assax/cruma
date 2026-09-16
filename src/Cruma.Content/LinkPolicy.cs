namespace Cruma.Content;

/// <summary>Povolená schémata odkazů v dokumentu: jen http, https a mailto (FR-7 akc. 7, security-policy.md §3).</summary>
public static class LinkPolicy
{
    private static readonly string[] AllowedSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto];

    public static bool IsAllowed(string? href) =>
        Uri.TryCreate(href, UriKind.Absolute, out var uri)
        && AllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);
}

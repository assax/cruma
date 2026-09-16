using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Cruma.Server.Infrastructure;

/// <summary>
/// Feed desktopu (I1-D-5, plan.md N-6): složka s instalátorem a soubory aktualizací (výstup <c>vpk pack</c>) vystavená
/// pod <c>/desktop/</c> bez autentizace – statické soubory nejsou endpointy, výjimka z API-003 je jen tato cesta.
/// </summary>
public static class DesktopFeed
{
    public const string PathKey = "Cruma:Desktop:FeedPath";
    public const string RequestPath = "/desktop";

    public static IApplicationBuilder UseDesktopFeed(this IApplicationBuilder app, IConfiguration configuration)
    {
        var folder = configuration[PathKey];
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return app;
        }

        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".nupkg"] = "application/octet-stream";
        return app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.GetFullPath(folder)),
            RequestPath = RequestPath,
            ContentTypeProvider = contentTypes,
            ServeUnknownFileTypes = false,
        });
    }
}

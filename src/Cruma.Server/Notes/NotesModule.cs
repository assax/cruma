using Cruma.Server.Notes.Application;
using Cruma.Server.Notes.Endpoints;

namespace Cruma.Server.Notes;

public static class NotesModule
{
    public static IServiceCollection AddNotesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NotesOptions>(configuration.GetSection(NotesOptions.Section));
        return services.AddScoped<INotesService, NotesService>();
    }

    public static void MapNotesEndpoints(this IEndpointRouteBuilder endpoints) => NotesEndpoints.Map(endpoints);
}

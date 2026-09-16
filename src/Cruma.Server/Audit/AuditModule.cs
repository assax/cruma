namespace Cruma.Server.Audit;

public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services) =>
        services.AddScoped<IAuditService, AuditService>();
}

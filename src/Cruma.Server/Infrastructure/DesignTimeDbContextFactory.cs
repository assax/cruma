using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cruma.Server.Infrastructure;

/// <summary>Vytváří kontext pro <c>dotnet ef migrations</c> bez spuštění hostitele; k databázi se nepřipojuje.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CrumaDbContext>
{
    public CrumaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CrumaDbContext>();
        InfrastructureModule.ConfigureDbContext(options, "Host=localhost;Database=cruma;Username=cruma");
        return new CrumaDbContext(options.Options, new NoUser());
    }

    private sealed class NoUser : ICurrentUser
    {
        public Guid? UserId => null;
    }
}

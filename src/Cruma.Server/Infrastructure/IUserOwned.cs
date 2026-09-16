namespace Cruma.Server.Infrastructure;

/// <summary>
/// Entita patřící uživateli. <see cref="CrumaDbContext"/> na ni použije globální filtr aktuálního uživatele
/// a při zápisu hlídá vlastníka (PER-002).
/// </summary>
public interface IUserOwned
{
    Guid OwnerUserId { get; set; }
}

using Cruma.Server.Audit;
using Cruma.Server.Identity.Persistence;
using Cruma.Server.Infrastructure;
using Cruma.Server.Notes;
using Microsoft.EntityFrameworkCore;

namespace Cruma.Server.Identity;

/// <summary>Ověřená externí identita z přihlášení (poskytovatel a jeho identifikátor subjektu).</summary>
public sealed record ExternalLogin(string Provider, string Subject);

/// <summary>Veřejné rozhraní modulu identity. Ostatní moduly znají jen interní identifikátor uživatele (SEC-001, NFR-14).</summary>
public interface IIdentityService
{
    /// <summary>
    /// Přihlásí externí identitu. Při prvním přihlášení v jedné transakci založí uživatele, naváže identitu,
    /// vytvoří výchozí kategorii a zapíše audit (plan.md N-4). Vrací interní identifikátor uživatele.
    /// </summary>
    Task<Guid> SignInExternalAsync(ExternalLogin login, CancellationToken cancellationToken);

    Task RecordSignOutAsync(Guid userId, CancellationToken cancellationToken);

    Task RecordSignInFailedAsync(string provider, string reason, CancellationToken cancellationToken);
}

internal sealed class IdentityService(
    CrumaDbContext db,
    CurrentUser currentUser,
    INotesService notes,
    IAuditService audit,
    TimeProvider timeProvider) : IIdentityService
{
    public async Task<Guid> SignInExternalAsync(ExternalLogin login, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        var link = await db.Set<LinkedIdentityEntity>()
            .SingleOrDefaultAsync(identity => identity.Provider == login.Provider && identity.Subject == login.Subject, cancellationToken);

        Guid userId;
        if (link is null)
        {
            userId = Guid.CreateVersion7(now);
            db.Set<UserEntity>().Add(new UserEntity { Id = userId, CreatedAtUtc = now, LastSignInAtUtc = now });
            db.Set<LinkedIdentityEntity>().Add(new LinkedIdentityEntity
            {
                Id = Guid.CreateVersion7(now),
                UserId = userId,
                Provider = login.Provider,
                Subject = login.Subject,
                LinkedAtUtc = now,
            });
            await db.SaveChangesAsync(cancellationToken);

            // Session ještě neexistuje – založení dat jménem nového uživatele je pojmenovaná výjimka (N-4).
            using (currentUser.ActAs(userId))
            {
                await notes.EnsureDefaultCategoryAsync(cancellationToken);
                audit.Record(new AuditEvent(AuditOperations.ProviderLinked, AuditObjectTypes.User, userId.ToString(),
                    Attributes: new Dictionary<string, string> { ["provider"] = login.Provider }, UserId: userId));
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            userId = link.UserId;
            var user = await db.Set<UserEntity>().SingleAsync(entity => entity.Id == userId, cancellationToken);
            user.LastSignInAtUtc = now;
        }

        audit.Record(new AuditEvent(AuditOperations.SignIn, AuditObjectTypes.User, userId.ToString(),
            Attributes: new Dictionary<string, string> { ["provider"] = login.Provider }, UserId: userId));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return userId;
    }

    public Task RecordSignOutAsync(Guid userId, CancellationToken cancellationToken) =>
        audit.RecordNowAsync(new AuditEvent(AuditOperations.SignOut, AuditObjectTypes.User, userId.ToString(), UserId: userId), cancellationToken);

    public Task RecordSignInFailedAsync(string provider, string reason, CancellationToken cancellationToken) =>
        audit.RecordNowAsync(
            new AuditEvent(AuditOperations.SignInFailed, AuditObjectTypes.User, null, AuditResult.Failure,
                new Dictionary<string, string> { ["provider"] = provider, ["reason"] = reason }),
            cancellationToken);
}

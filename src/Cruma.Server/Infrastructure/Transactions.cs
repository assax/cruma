using Microsoft.EntityFrameworkCore;

namespace Cruma.Server.Infrastructure;

/// <summary>Transakce kolem jednoho use case; vnořené volání se připojí k probíhající transakci.</summary>
public static class Transactions
{
    /// <summary>
    /// Spustí operaci v transakci. Úspěšný výsledek uloží změny a potvrdí, neúspěšný výsledek i výjimka transakci
    /// vrátí zpět. Běží-li už transakce (např. zpracování změny synchronizace), jen se připojí.
    /// </summary>
    public static async Task<TResult> InTransactionAsync<TResult>(
        this CrumaDbContext db, Func<Task<TResult>> operation, CancellationToken cancellationToken)
        where TResult : ServiceResult
    {
        if (db.Database.CurrentTransaction is not null)
        {
            return await operation();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var result = await operation();
        if (!result.IsSuccess)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return result;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}

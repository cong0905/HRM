using HRM.Next.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Persistence;

public sealed class UnitOfWork(HrmDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> useCase, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Nested use-case transactions are not supported.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await useCase(cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            // Request cancellation must not cancel cleanup of an already-open transaction.
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }
}

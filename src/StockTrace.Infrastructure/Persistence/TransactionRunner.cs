using System.Data;
using StockTrace.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace StockTrace.Infrastructure.Persistence;

internal sealed class TransactionRunner(ApplicationDbContext dbContext) : ITransactionRunner
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}

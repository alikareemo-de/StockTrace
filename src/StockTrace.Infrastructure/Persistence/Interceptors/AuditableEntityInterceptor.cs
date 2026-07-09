using StockTrace.Application.Abstractions;
using StockTrace.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace StockTrace.Infrastructure.Persistence.Interceptors;

internal sealed class AuditableEntityInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditInformation(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditInformation(DbContext? context)
    {
        if (context is null) return;

        var now = timeProvider.GetUtcNow();
        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(AuditableEntity.CreatedAt)).CurrentValue = now;
                entry.Property(nameof(AuditableEntity.CreatedBy)).CurrentValue = currentUser.UserId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(AuditableEntity.ModifiedAt)).CurrentValue = now;
                entry.Property(nameof(AuditableEntity.ModifiedBy)).CurrentValue = currentUser.UserId;
            }

            if (entry.State == EntityState.Deleted && entry.Entity is SoftDeletableEntity)
            {
                entry.State = EntityState.Modified;
                entry.Property(nameof(SoftDeletableEntity.IsDeleted)).CurrentValue = true;
                entry.Property(nameof(SoftDeletableEntity.DeletedAt)).CurrentValue = now;
                entry.Property(nameof(SoftDeletableEntity.DeletedBy)).CurrentValue = currentUser.UserId;
            }
        }
    }
}

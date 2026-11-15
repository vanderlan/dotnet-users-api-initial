using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using UsersApi.Domain.Entities;

namespace UsersApi.Data.Interceptors;

/// <summary>
/// Intercepta o SaveChanges: preenche PublicId/CreatedAt em entidades novas e
/// LastUpdated em toda alteração. Assim nenhuma regra de negócio precisa lembrar disso.
/// </summary>
public sealed class TimestampsInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;

        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.PublicId = Guid.NewGuid().ToString();
                entry.Entity.CreatedAt = now;
                entry.Entity.LastUpdated = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastUpdated = now;
            }
        }
    }
}

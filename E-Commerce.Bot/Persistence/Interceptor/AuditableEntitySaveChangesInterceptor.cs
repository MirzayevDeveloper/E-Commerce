using E_Commerce.Bot.Entities.BaseEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace E_Commerce.Bot.Persistence.Interceptor
{
	public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
	{
		public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
		{
			UpdateEntities(eventData.Context!);

			return base.SavingChanges(eventData, result);
		}

		public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			UpdateEntities(eventData.Context!);

			return base.SavingChangesAsync(eventData, result, cancellationToken);
		}

		public void UpdateEntities(DbContext context)
		{
			if (context == null) return;

			foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
			{
				if (entry.State is EntityState.Added)
				{
					entry.Entity.CreatedDate = DateTime.Now;
				}

				if (entry.State is EntityState.Added || entry.State is EntityState.Modified)
				{
					entry.Entity.UpdatedDate = DateTime.Now;
				}
			}
		}
	}
}

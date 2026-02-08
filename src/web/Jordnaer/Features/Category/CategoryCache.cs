using Jordnaer.Database;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace Jordnaer.Features.Category;

public interface ICategoryCache
{
	ValueTask<List<Shared.Category>> GetOrCreateCategoriesAsync(CancellationToken cancellationToken = default);
}

public class CategoryCache(
	IFusionCache fusionCache,
	IDbContextFactory<JordnaerDbContext> contextFactory)
	: ICategoryCache
{
	public async ValueTask<List<Shared.Category>> GetOrCreateCategoriesAsync(CancellationToken cancellationToken = default) =>
		await fusionCache.GetOrSetAsync<List<Shared.Category>>(
			nameof(Shared.Category),
			async (ctx, innerToken) =>
			{
				await using var context = await contextFactory.CreateDbContextAsync(innerToken);
				return await context.Categories.AsNoTracking().ToListAsync(innerToken);
			},
			options: new FusionCacheEntryOptions { Duration = TimeSpan.FromMinutes(15) },
			token: cancellationToken) ?? [];
}

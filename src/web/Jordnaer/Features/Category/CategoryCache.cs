using Jordnaer.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jordnaer.Features.Category;

public interface ICategoryCache
{
	ValueTask<List<Shared.Category>> GetOrCreateCategoriesAsync(CancellationToken cancellationToken = default);
}

public class CategoryCache : ICategoryCache
{
	private readonly IMemoryCache _memoryCache;
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory;

	public CategoryCache(IMemoryCache memoryCache, IDbContextFactory<JordnaerDbContext> contextFactory)
	{
		_memoryCache = memoryCache;
		_contextFactory = contextFactory;
	}

#pragma warning disable CS8603 // Possible null reference return.
	public async ValueTask<List<Shared.Category>> GetOrCreateCategoriesAsync(CancellationToken cancellationToken = default) =>
		await _memoryCache.GetOrCreateAsync(nameof(Shared.Category), async entry =>
		{
			await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

			var categories = await context.Categories.AsNoTracking().ToListAsync(cancellationToken);

			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

			return categories;
		});
#pragma warning restore CS8603 // Possible null reference return.
}

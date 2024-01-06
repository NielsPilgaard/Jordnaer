using Microsoft.Extensions.Caching.Memory;

namespace Jordnaer.Features.Category;

public interface ICategoryCache
{
	Task<List<Jordnaer.Shared.Category>> GetOrCreateCategoriesAsync();
}

public class CategoryCache : ICategoryCache
{
	private readonly IMemoryCache _memoryCache;
	private readonly ICategoryClient _categoryApi;

	public CategoryCache(IMemoryCache memoryCache, ICategoryClient categoryApi)
	{
		_memoryCache = memoryCache;
		_categoryApi = categoryApi;
	}

#pragma warning disable CS8603 // Possible null reference return.
	public async Task<List<Jordnaer.Shared.Category>> GetOrCreateCategoriesAsync() =>
		await _memoryCache.GetOrCreateAsync(nameof(Jordnaer.Shared.Category), async entry =>
		{
			var result = await _categoryApi.GetCategories();
			if (result.IsSuccessStatusCode)
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
				return result.Content!;
			}

			if (entry.Value is List<Jordnaer.Shared.Category> oldEntry)
			{
				// Set this cache entry to expire in quickly to retry early
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
				return oldEntry;
			}

			return new List<Jordnaer.Shared.Category>();
		});
#pragma warning restore CS8603 // Possible null reference return.
}

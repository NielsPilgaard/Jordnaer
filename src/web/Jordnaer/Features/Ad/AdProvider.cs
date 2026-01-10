using Jordnaer.Database;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.Ad;

public interface IAdProvider
{
	Task<List<AdData>> GetAdsAsync(int count, CancellationToken cancellationToken = default);
}

public class AdProvider(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<AdProvider> logger) : IAdProvider
{
	public async Task<List<AdData>> GetAdsAsync(int count, CancellationToken cancellationToken = default)
	{
		if (count <= 0)
		{
			return [];
		}

		var allAds = new List<AdData>();

		// Get partner ads from database
		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var partnerAds = await context.Partners
				.AsNoTracking()
				.Where(p => p.CanHaveAd && p.AdImageUrl != null && p.AdImageUrl != "")
				.Select(p => new AdData
				{
					Title = p.Name ?? "Partner",
					Description = p.Description,
					ImagePath = p.AdImageUrl!,
					Link = p.Link ?? "#",
					PartnerId = p.Id
				})
				.ToListAsync(cancellationToken);

			allAds.AddRange(partnerAds);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to fetch partner ads from database");
		}

		// Add hardcoded ads as fallback/supplement
		var hardcodedAds = HardcodedAds.GetAdsForSearch(count);
		allAds.AddRange(hardcodedAds);

		if (allAds.Count == 0)
		{
			return [];
		}

		// Shuffle to mix partner and hardcoded ads
		var shuffled = allAds.OrderBy(_ => Random.Shared.Next()).ToList();

		// Return requested count, cycling if needed
		var result = new List<AdData>();
		for (var i = 0; i < count; i++)
		{
			result.Add(shuffled[i % shuffled.Count]);
		}

		return result;
	}
}

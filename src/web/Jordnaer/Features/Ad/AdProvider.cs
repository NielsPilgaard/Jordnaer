using Jordnaer.Database;
using Jordnaer.Extensions;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using ZiggyCreatures.Caching.Fusion;

namespace Jordnaer.Features.Ad;

public interface IAdProvider
{
	Task<OneOf<List<AdData>, Error<string>>> GetAdsAsync(int count, CancellationToken cancellationToken = default);
}

public class AdProvider(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	IFusionCache fusionCache,
	ILogger<AdProvider> logger) : IAdProvider
{
	private const string Tag = "ads";

	public async Task<OneOf<List<AdData>, Error<string>>> GetAdsAsync(int count, CancellationToken cancellationToken = default)
	{
		if (count <= 0)
		{
			return new List<AdData>();
		}

		List<AdData>? partnerAds;
		try
		{
			// Cache the partner ads from database
			partnerAds = await fusionCache.GetOrSetAsync<List<AdData>>(
				"PartnerAds",
				async (ctx, innerToken) =>
				{
					await using var context = await contextFactory.CreateDbContextAsync(innerToken);
					var utcNow = DateTime.UtcNow;
					return await context.Partners
						.AsNoTracking()
						.Where(p => p.CanHaveAd && p.AdImageUrl != null && p.AdImageUrl != "")
						.Where(p => (p.DisplayStartUtc == null || utcNow >= p.DisplayStartUtc) &&
									(p.DisplayEndUtc == null || utcNow <= p.DisplayEndUtc))
						.Select(p => new AdData
						{
							Title = p.Name ?? "Partner",
							Description = p.Description,
							ImagePath = p.AdImageUrl!,
							Link = p.AdLink ?? p.PartnerPageLink ?? "#",
							PartnerId = p.Id,
							LabelColor = p.AdLabelColor
						})
						.ToListAsync(innerToken);
				},
				tags: [Tag],
				token: cancellationToken);
		}
		catch (Exception ex)
		{
			return logger.LogAndReturnErrorResult(ex, "Failed to load partner ads from database.");
		}

		var allAds = partnerAds ?? [];

		// Add hardcoded ads as fallback/supplement
		allAds.AddRange(HardcodedAds.GetAll());

		if (allAds.Count is 0)
		{
			return new List<AdData>();
		}

		// Randomly select the requested count of ads
		return allAds
			.Shuffle()
			.Take(count)
			.ToList();
	}
}

using Jordnaer.Database;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Ad;

public interface IAdProvider
{
	Task<OneOf<List<AdData>, Error<string>>> GetAdsAsync(int count, CancellationToken cancellationToken = default);
}

public class AdProvider(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<AdProvider> logger) : IAdProvider
{
	public async Task<OneOf<List<AdData>, Error<string>>> GetAdsAsync(int count, CancellationToken cancellationToken = default)
	{
		if (count <= 0)
		{
			return new List<AdData>();
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
					Link = p.AdLink ?? p.PartnerPageLink ?? "#",
					PartnerId = p.Id,
					LabelColor = p.AdLabelColor
				})
				.ToListAsync(cancellationToken);

			allAds.AddRange(partnerAds);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to fetch partner ads from database");
			return new Error<string>("Failed to fetch ads from database");
		}

		// Add hardcoded ads as fallback/supplement
		allAds.AddRange(HardcodedAds.GetAll());

		// TODO: This is a prime use-case for caching.

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

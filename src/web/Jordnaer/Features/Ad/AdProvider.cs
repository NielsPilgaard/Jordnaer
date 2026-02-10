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

/// <summary>
/// Internal cache entry that includes display-window fields so time filtering
/// can be applied at read time instead of at cache-population time.
/// </summary>
internal record PartnerAdEntry
{
	public required string Title { get; init; }
	public string? Description { get; init; }
	public required string ImagePath { get; init; }
	public required string Link { get; init; }
	public Guid PartnerId { get; init; }
	public string? LabelColor { get; init; }
	public DateTime? DisplayStartUtc { get; init; }
	public DateTime? DisplayEndUtc { get; init; }
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

		List<PartnerAdEntry>? cachedPartners;
		try
		{
			// Cache all ad-eligible partners (superset) — time-window filtering
			// is applied at read time so the cache stays valid across display boundaries.
			cachedPartners = await fusionCache.GetOrSetAsync<List<PartnerAdEntry>>(
				"PartnerAds",
				async (ctx, innerToken) =>
				{
					await using var context = await contextFactory.CreateDbContextAsync(innerToken);
					return await context.Partners
						.AsNoTracking()
						.Where(p => p.CanHaveAd && p.AdImageUrl != null && p.AdImageUrl != "")
						.Select(p => new PartnerAdEntry
						{
							Title = p.Name ?? "Partner",
							Description = p.Description,
							ImagePath = p.AdImageUrl!,
							Link = p.AdLink ?? p.PartnerPageLink ?? "#",
							PartnerId = p.Id,
							LabelColor = p.AdLabelColor,
							DisplayStartUtc = p.DisplayStartUtc,
							DisplayEndUtc = p.DisplayEndUtc
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

		// Apply time-window filter at read time so stale cache doesn't serve expired ads
		var utcNow = DateTime.UtcNow;
		var partnerAds = (cachedPartners ?? [])
			.Where(p => (p.DisplayStartUtc is null || utcNow >= p.DisplayStartUtc) &&
						(p.DisplayEndUtc is null || utcNow <= p.DisplayEndUtc))
			.Select(p => new AdData
			{
				Title = p.Title,
				Description = p.Description,
				ImagePath = p.ImagePath,
				Link = p.Link,
				PartnerId = p.PartnerId,
				LabelColor = p.LabelColor
			})
			.ToList();

		var allAds = partnerAds;

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

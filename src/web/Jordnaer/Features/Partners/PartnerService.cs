using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Email;
using Jordnaer.Features.Images;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Partners;

public interface IPartnerService
{
	Task<OneOf<Partner, NotFound>> GetPartnerByIdAsync(Guid id, CancellationToken cancellationToken = default);
	Task<OneOf<Partner, NotFound>> GetPartnerByUserIdAsync(string userId, CancellationToken cancellationToken = default);
	Task<List<Partner>> GetAllPartnersAsync(CancellationToken cancellationToken = default);
	Task<PartnerAnalyticsDto> GetAnalyticsAsync(Guid partnerId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> RecordImpressionAsync(Guid partnerId, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> RecordClickAsync(Guid partnerId, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> UploadPendingChangesAsync(Guid partnerId, Stream? adImageStream, string? adImageFileName, string? name, string? description, Stream? logoStream, string? logoFileName, string? link, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> ApproveChangesAsync(Guid partnerId, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> RejectChangesAsync(Guid partnerId, CancellationToken cancellationToken = default);
}

public class PartnerService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<PartnerService> logger,
	CurrentUser currentUser,
	IImageService imageService,
	IEmailService emailService) : IPartnerService
{
	private const string PartnerAdsContainer = "partner-ads";
	private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB
	private static readonly string[] AllowedImageFormats = [".png", ".jpg", ".jpeg", ".webp"];

	public async Task<OneOf<Partner, NotFound>> GetPartnerByIdAsync(Guid id, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var partner = await context.Partners
			.AsNoTracking()
			.Include(s => s.Analytics)
			.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

		return partner is null
			? new NotFound()
			: partner;
	}

	public async Task<OneOf<Partner, NotFound>> GetPartnerByUserIdAsync(string userId, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var partner = await context.Partners
			.AsNoTracking()
			.Include(s => s.Analytics)
			.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

		return partner is null
			? new NotFound()
			: partner;
	}

	public async Task<List<Partner>> GetAllPartnersAsync(CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.Partners
			.AsNoTracking()
			.OrderBy(s => s.Name)
			.ToListAsync(cancellationToken);
	}

	public async Task<PartnerAnalyticsDto> GetAnalyticsAsync(Guid partnerId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var analytics = await context.PartnerAnalytics
			.AsNoTracking()
			.Where(a => a.PartnerId == partnerId && a.Date >= fromDate.Date && a.Date <= toDate.Date)
			.OrderBy(a => a.Date)
			.ToListAsync(cancellationToken);

		var totalImpressions = analytics.Sum(a => a.Impressions);
		var totalClicks = analytics.Sum(a => a.Clicks);
		var ctr = totalImpressions > 0 ? (double)totalClicks / totalImpressions * 100 : 0;

		return new PartnerAnalyticsDto
		{
			PartnerId = partnerId,
			TotalImpressions = totalImpressions,
			TotalClicks = totalClicks,
			ClickThroughRate = Math.Round(ctr, 2),
			DailyAnalytics = analytics.Select(a => new DailyAnalyticsDto
			{
				Date = a.Date,
				Impressions = a.Impressions,
				Clicks = a.Clicks
			}).ToList()
		};
	}

	public async Task<OneOf<Success, Error<string>>> RecordImpressionAsync(Guid partnerId, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var today = DateTime.UtcNow.Date;

			var analytics = await context.PartnerAnalytics
				.FirstOrDefaultAsync(a => a.PartnerId == partnerId && a.Date == today, cancellationToken);

			if (analytics is null)
			{
				analytics = new PartnerAnalytics
				{
					PartnerId = partnerId,
					Date = today,
					Impressions = 1,
					Clicks = 0
				};
				context.PartnerAnalytics.Add(analytics);
			}
			else
			{
				analytics.Impressions++;
			}

			await context.SaveChangesAsync(cancellationToken);
			return new Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to record impression for partner {PartnerId}", partnerId);
			return new Error<string>("Failed to record impression");
		}
	}

	public async Task<OneOf<Success, Error<string>>> RecordClickAsync(Guid partnerId, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var today = DateTime.UtcNow.Date;

			var analytics = await context.PartnerAnalytics
				.FirstOrDefaultAsync(a => a.PartnerId == partnerId && a.Date == today, cancellationToken);

			if (analytics is null)
			{
				analytics = new PartnerAnalytics
				{
					PartnerId = partnerId,
					Date = today,
					Impressions = 0,
					Clicks = 1
				};
				context.PartnerAnalytics.Add(analytics);
			}
			else
			{
				analytics.Clicks++;
			}

			await context.SaveChangesAsync(cancellationToken);
			return new Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to record click for partner {PartnerId}", partnerId);
			return new Error<string>("Failed to record click");
		}
	}

	public async Task<OneOf<Success, Error<string>>> UploadPendingChangesAsync(
		Guid partnerId,
		Stream? adImageStream,
		string? adImageFileName,
		string? name,
		string? description,
		Stream? logoStream,
		string? logoFileName,
		string? link,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var partner = await context.Partners.FirstOrDefaultAsync(s => s.Id == partnerId, cancellationToken);

			if (partner is null)
			{
				return new Error<string>("Partner not found");
			}

			// Validate that the current user owns this partner
			if (partner.UserId != currentUser.Id)
			{
				return new Error<string>("Unauthorized");
			}

			var hasChanges = false;

			// Upload ad image if provided
			if (adImageStream is not null)
			{
				if (adImageStream.Length > MaxImageSizeBytes)
				{
					return new Error<string>("Ad image exceeds maximum size of 5MB");
				}

				// Validate file extension
				if (!string.IsNullOrEmpty(adImageFileName))
				{
					var extension = Path.GetExtension(adImageFileName).ToLowerInvariant();
					if (!AllowedImageFormats.Contains(extension))
					{
						return new Error<string>($"Ad image format not allowed. Allowed formats: {string.Join(", ", AllowedImageFormats)}");
					}
				}

				var adImageUrl = await imageService.UploadImageAsync(
					$"{partnerId}_ad_{DateTime.UtcNow:yyyyMMddHHmmss}.webp",
					PartnerAdsContainer,
					adImageStream,
					cancellationToken);

				partner.PendingAdImageUrl = adImageUrl;
				hasChanges = true;
			}

			// Upload logo if provided
			if (logoStream is not null)
			{
				if (logoStream.Length > MaxImageSizeBytes)
				{
					return new Error<string>("Logo exceeds maximum size of 5MB");
				}

				// Validate file extension
				if (!string.IsNullOrEmpty(logoFileName))
				{
					var extension = Path.GetExtension(logoFileName).ToLowerInvariant();
					if (!AllowedImageFormats.Contains(extension))
					{
						return new Error<string>($"Logo format not allowed. Allowed formats: {string.Join(", ", AllowedImageFormats)}");
					}
				}

				var logoUrl = await imageService.UploadImageAsync(
					$"{partnerId}_logo_{DateTime.UtcNow:yyyyMMddHHmmss}.webp",
					PartnerAdsContainer,
					logoStream,
					cancellationToken);

				partner.PendingLogoUrl = logoUrl;
				hasChanges = true;
			}

			// Update text fields
			if (!string.IsNullOrWhiteSpace(name))
			{
				partner.PendingName = name.Trim();
				hasChanges = true;
			}

			if (!string.IsNullOrWhiteSpace(description))
			{
				partner.PendingDescription = description.Trim();
				hasChanges = true;
			}

			if (!string.IsNullOrWhiteSpace(link))
			{
				partner.PendingLink = link.Trim();
				hasChanges = true;
			}

			if (hasChanges)
			{
				partner.HasPendingApproval = true;
				partner.LastUpdateUtc = DateTime.UtcNow;
				context.Partners.Update(partner);
				await context.SaveChangesAsync(cancellationToken);

				// Send notification email to admin
				await emailService.SendPartnerImageApprovalEmailAsync(partner.Id, partner.Name, cancellationToken);

				return new Success();
			}

			return new Error<string>("No changes provided");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to upload pending changes for partner {PartnerId}", partnerId);
			return new Error<string>("Failed to upload changes");
		}
	}

	public async Task<OneOf<Success, Error<string>>> ApproveChangesAsync(Guid partnerId, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			// Check admin authorization
			if (!currentUser.User.IsInRole(AppRoles.Admin))
			{
				return new Error<string>("Unauthorized: Admin role required");
			}

			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var partner = await context.Partners.FirstOrDefaultAsync(s => s.Id == partnerId, cancellationToken);

			if (partner is null)
			{
				return new Error<string>("Partner not found");
			}

			// Delete old ad image if new one is being approved
			if (!string.IsNullOrEmpty(partner.AdImageUrl) && !string.IsNullOrEmpty(partner.PendingAdImageUrl))
			{
				var adImageName = Path.GetFileName(new Uri(partner.AdImageUrl).LocalPath);
				await imageService.DeleteImageAsync(adImageName, PartnerAdsContainer, cancellationToken);
			}

			// Delete old logo if new one is being approved
			if (!string.IsNullOrEmpty(partner.LogoUrl) && !string.IsNullOrEmpty(partner.PendingLogoUrl))
			{
				var logoName = Path.GetFileName(new Uri(partner.LogoUrl).LocalPath);
				await imageService.DeleteImageAsync(logoName, PartnerAdsContainer, cancellationToken);
			}

			// Move pending changes to active
			if (!string.IsNullOrEmpty(partner.PendingAdImageUrl))
			{
				partner.AdImageUrl = partner.PendingAdImageUrl;
				partner.PendingAdImageUrl = null;
			}

			if (!string.IsNullOrEmpty(partner.PendingLogoUrl))
			{
				partner.LogoUrl = partner.PendingLogoUrl;
				partner.PendingLogoUrl = null;
			}

			if (!string.IsNullOrEmpty(partner.PendingName))
			{
				partner.Name = partner.PendingName;
				partner.PendingName = null;
			}

			if (!string.IsNullOrEmpty(partner.PendingDescription))
			{
				partner.Description = partner.PendingDescription;
				partner.PendingDescription = null;
			}

			if (!string.IsNullOrEmpty(partner.PendingLink))
			{
				partner.Link = partner.PendingLink;
				partner.PendingLink = null;
			}

			partner.HasPendingApproval = false;
			partner.LastUpdateUtc = DateTime.UtcNow;

			await context.SaveChangesAsync(cancellationToken);

			return new Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to approve changes for partner {PartnerId}", partnerId);
			return new Error<string>("Failed to approve changes");
		}
	}

	public async Task<OneOf<Success, Error<string>>> RejectChangesAsync(Guid partnerId, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			// Check admin authorization
			if (!currentUser.User.IsInRole(AppRoles.Admin))
			{
				return new Error<string>("Unauthorized: Admin role required");
			}

			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var partner = await context.Partners.FirstOrDefaultAsync(s => s.Id == partnerId, cancellationToken);

			if (partner is null)
			{
				return new Error<string>("Partner not found");
			}

			// Delete pending ad image
			if (!string.IsNullOrEmpty(partner.PendingAdImageUrl))
			{
				var adImageName = Path.GetFileName(new Uri(partner.PendingAdImageUrl).LocalPath);
				await imageService.DeleteImageAsync(adImageName, PartnerAdsContainer, cancellationToken);
				partner.PendingAdImageUrl = null;
			}

			// Delete pending logo
			if (!string.IsNullOrEmpty(partner.PendingLogoUrl))
			{
				var logoName = Path.GetFileName(new Uri(partner.PendingLogoUrl).LocalPath);
				await imageService.DeleteImageAsync(logoName, PartnerAdsContainer, cancellationToken);
				partner.PendingLogoUrl = null;
			}

			// Clear pending text fields
			partner.PendingName = null;
			partner.PendingDescription = null;
			partner.PendingLink = null;
			partner.HasPendingApproval = false;

			await context.SaveChangesAsync(cancellationToken);

			return new Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to reject changes for partner {PartnerId}", partnerId);
			return new Error<string>("Failed to reject changes");
		}
	}
}

public class PartnerAnalyticsDto
{
	public Guid PartnerId { get; set; }
	public int TotalImpressions { get; set; }
	public int TotalClicks { get; set; }
	public double ClickThroughRate { get; set; }
	public List<DailyAnalyticsDto> DailyAnalytics { get; set; } = [];
}

public class DailyAnalyticsDto
{
	public DateTime Date { get; set; }
	public int Impressions { get; set; }
	public int Clicks { get; set; }
}

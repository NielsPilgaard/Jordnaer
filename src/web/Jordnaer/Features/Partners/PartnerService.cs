using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Email;
using Jordnaer.Features.Images;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using Serilog;

namespace Jordnaer.Features.Partners;

public interface IPartnerService
{
	Task<OneOf<Partner, NotFound>> GetPartnerByIdAsync(Guid id, CancellationToken cancellationToken = default);
	Task<OneOf<Partner, NotFound>> GetPartnerByUserIdAsync(string userId, CancellationToken cancellationToken = default);
	Task<List<Partner>> GetAllPartnersAsync(CancellationToken cancellationToken = default);
	Task<PartnerAnalyticsDto> GetAnalyticsAsync(Guid partnerId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> RecordImpressionAsync(Guid partnerId, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> RecordClickAsync(Guid partnerId, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> UploadPendingImagesAsync(Guid partnerId, Stream? mobileImageStream, string? mobileImageFileName, Stream? desktopImageStream, string? desktopImageFileName, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> ApproveImagesAsync(Guid partnerId, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> RejectImagesAsync(Guid partnerId, CancellationToken cancellationToken = default);
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

	public async Task<OneOf<Success, Error<string>>> UploadPendingImagesAsync(
		Guid partnerId,
		Stream? mobileImageStream,
		string? mobileImageFileName,
		Stream? desktopImageStream,
		string? desktopImageFileName,
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

			// Upload mobile image if provided
			if (mobileImageStream is not null)
			{
				if (mobileImageStream.Length > MaxImageSizeBytes)
				{
					return new Error<string>("Mobile image exceeds maximum size of 5MB");
				}

				// Validate file extension
				if (!string.IsNullOrEmpty(mobileImageFileName))
				{
					var extension = Path.GetExtension(mobileImageFileName).ToLowerInvariant();
					if (!AllowedImageFormats.Contains(extension))
					{
						return new Error<string>($"Mobile image format not allowed. Allowed formats: {string.Join(", ", AllowedImageFormats)}");
					}
				}

				var mobileImageUrl = await imageService.UploadImageAsync(
					$"{partnerId}_mobile_{DateTime.UtcNow:yyyyMMddHHmmss}.webp",
					PartnerAdsContainer,
					mobileImageStream,
					cancellationToken);

				partner.PendingMobileImageUrl = mobileImageUrl;
			}

			// Upload desktop image if provided
			if (desktopImageStream is not null)
			{
				if (desktopImageStream.Length > MaxImageSizeBytes)
				{
					return new Error<string>("Desktop image exceeds maximum size of 5MB");
				}

				// Validate file extension
				if (!string.IsNullOrEmpty(desktopImageFileName))
				{
					var extension = Path.GetExtension(desktopImageFileName).ToLowerInvariant();
					if (!AllowedImageFormats.Contains(extension))
					{
						return new Error<string>($"Desktop image format not allowed. Allowed formats: {string.Join(", ", AllowedImageFormats)}");
					}
				}

				var desktopImageUrl = await imageService.UploadImageAsync(
					$"{partnerId}_desktop_{DateTime.UtcNow:yyyyMMddHHmmss}.webp",
					PartnerAdsContainer,
					desktopImageStream,
					cancellationToken);

				partner.PendingDesktopImageUrl = desktopImageUrl;
			}

			if (mobileImageStream is not null || desktopImageStream is not null)
			{
				partner.HasPendingImageApproval = true;
				partner.LastImageUpdateUtc = DateTime.UtcNow;
				context.Partners.Update(partner);
				await context.SaveChangesAsync(cancellationToken);

				// Send notification email to admin
				await emailService.SendPartnerImageApprovalEmailAsync(partner.Id, partner.Name, cancellationToken);

				return new Success();
			}

			return new Error<string>("No images provided");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to upload pending images for partner {PartnerId}", partnerId);
			return new Error<string>("Failed to upload images");
		}
	}

	public async Task<OneOf<Success, Error<string>>> ApproveImagesAsync(Guid partnerId, CancellationToken cancellationToken = default)
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

			// Delete old mobile image if new one is being approved
			if (!string.IsNullOrEmpty(partner.MobileImageUrl) && !string.IsNullOrEmpty(partner.PendingMobileImageUrl))
			{
				var mobileImageName = Path.GetFileName(new Uri(partner.MobileImageUrl).LocalPath);
				await imageService.DeleteImageAsync(mobileImageName, PartnerAdsContainer, cancellationToken);
			}

			// Delete old desktop image if new one is being approved
			if (!string.IsNullOrEmpty(partner.DesktopImageUrl) && !string.IsNullOrEmpty(partner.PendingDesktopImageUrl))
			{
				var desktopImageName = Path.GetFileName(new Uri(partner.DesktopImageUrl).LocalPath);
				await imageService.DeleteImageAsync(desktopImageName, PartnerAdsContainer, cancellationToken);
			}

			// Move pending images to active
			if (!string.IsNullOrEmpty(partner.PendingMobileImageUrl))
			{
				partner.MobileImageUrl = partner.PendingMobileImageUrl;
				partner.PendingMobileImageUrl = null;
			}

			if (!string.IsNullOrEmpty(partner.PendingDesktopImageUrl))
			{
				partner.DesktopImageUrl = partner.PendingDesktopImageUrl;
				partner.PendingDesktopImageUrl = null;
			}

			partner.HasPendingImageApproval = false;
			partner.LastImageUpdateUtc = DateTime.UtcNow;

			await context.SaveChangesAsync(cancellationToken);

			return new Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to approve images for partner {PartnerId}", partnerId);
			return new Error<string>("Failed to approve images");
		}
	}

	public async Task<OneOf<Success, Error<string>>> RejectImagesAsync(Guid partnerId, CancellationToken cancellationToken = default)
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

			// Delete pending images
			if (!string.IsNullOrEmpty(partner.PendingMobileImageUrl))
			{
				var mobileImageName = Path.GetFileName(new Uri(partner.PendingMobileImageUrl).LocalPath);
				await imageService.DeleteImageAsync(mobileImageName, PartnerAdsContainer, cancellationToken);
				partner.PendingMobileImageUrl = null;
			}

			if (!string.IsNullOrEmpty(partner.PendingDesktopImageUrl))
			{
				var desktopImageName = Path.GetFileName(new Uri(partner.PendingDesktopImageUrl).LocalPath);
				await imageService.DeleteImageAsync(desktopImageName, PartnerAdsContainer, cancellationToken);
				partner.PendingDesktopImageUrl = null;
			}

			partner.HasPendingImageApproval = false;

			await context.SaveChangesAsync(cancellationToken);

			return new Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to reject images for partner {PartnerId}", partnerId);
			return new Error<string>("Failed to reject images");
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

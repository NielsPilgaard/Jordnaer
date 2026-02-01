using EntityFramework.Exceptions.Common;
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

public enum AnalyticsType
{
	Impression,
	Click
}

public interface IPartnerService
{
	Task<OneOf<Partner, NotFound>> GetPartnerByIdAsync(Guid id, CancellationToken cancellationToken = default);
	Task<OneOf<Partner, NotFound>> GetPartnerByUserIdAsync(string userId, CancellationToken cancellationToken = default);
	Task<OneOf<List<Partner>, Error<string>>> GetAllPartnersAsync(CancellationToken cancellationToken = default);
	Task<OneOf<PartnerAnalyticsDto, NotFound, Error<string>>> GetAnalyticsAsync(Guid partnerId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> RecordImpressionAsync(Guid partnerId, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> RecordClickAsync(Guid partnerId, CancellationToken cancellationToken = default);
	Task<OneOf<string, Error<string>>> UploadPreviewImageAsync(Guid partnerId, Stream imageStream, string fileName, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> UploadPendingChangesAsync(Guid partnerId, Stream? adImageStream, string? adImageFileName, string? name, string? description, Stream? logoStream, string? logoFileName, string? partnerPageLink, string? adLink, string? adLabelColor, CancellationToken cancellationToken = default);
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

	public async Task<OneOf<List<Partner>, Error<string>>> GetAllPartnersAsync(CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var partners = await context.Partners
				.AsNoTracking()
				.OrderBy(s => s.Name ?? string.Empty)
				.ToListAsync(cancellationToken);
			return partners;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to get all partners");
			return new Error<string>("Failed to retrieve partners");
		}
	}

	public async Task<OneOf<PartnerAnalyticsDto, NotFound, Error<string>>> GetAnalyticsAsync(Guid partnerId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			// Verify partner exists
			var partnerExists = await context.Partners.AnyAsync(p => p.Id == partnerId, cancellationToken);
			if (!partnerExists)
			{
				return new NotFound();
			}

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
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to get analytics for partner {PartnerId}", partnerId);
			return new Error<string>("Failed to retrieve analytics");
		}
	}

	public async Task<OneOf<Success, Error<string>>> RecordImpressionAsync(Guid partnerId, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();
		return await RecordAnalyticsAsync(partnerId, AnalyticsType.Impression, cancellationToken);
	}

	public async Task<OneOf<Success, Error<string>>> RecordClickAsync(Guid partnerId, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();
		return await RecordAnalyticsAsync(partnerId, AnalyticsType.Click, cancellationToken);
	}

	private async Task<OneOf<Success, Error<string>>> RecordAnalyticsAsync(
		Guid partnerId,
		AnalyticsType analyticsType,
		CancellationToken cancellationToken)
	{
		var incrementImpression = analyticsType == AnalyticsType.Impression;
		var incrementClick = analyticsType == AnalyticsType.Click;
		var analyticsTypeName = analyticsType.ToString().ToLowerInvariant();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var today = DateTime.UtcNow.Date;

			// Try to update existing record first (atomic increment)
			var rowsAffected = await context.PartnerAnalytics
				.Where(a => a.PartnerId == partnerId && a.Date == today)
				.ExecuteUpdateAsync(setters =>
				{
					if (incrementImpression)
					{
						setters.SetProperty(a => a.Impressions, a => a.Impressions + 1);
					}

					if (incrementClick)
					{
						setters.SetProperty(a => a.Clicks, a => a.Clicks + 1);
					}
				}, cancellationToken);

			if (rowsAffected > 0)
			{
				return new Success();
			}

			// No existing record - validate partner exists before attempting insert
			var partnerExists = await context.Partners.AnyAsync(p => p.Id == partnerId, cancellationToken);
			if (!partnerExists)
			{
				logger.LogWarning("Attempted to record {AnalyticsType} for non-existent partner {PartnerId}", analyticsTypeName, partnerId);
				return new Error<string>($"Partner with ID '{partnerId}' not found");
			}

			// Try to insert a new record
			var analytics = new PartnerAnalytics
			{
				PartnerId = partnerId,
				Date = today,
				Impressions = incrementImpression ? 1 : 0,
				Clicks = incrementClick ? 1 : 0
			};
			context.PartnerAnalytics.Add(analytics);

			try
			{
				await context.SaveChangesAsync(cancellationToken);
				return new Success();
			}
			catch (UniqueConstraintException)
			{
				// Race condition: another request inserted the record between our check and insert
				// Detach the failed entity and retry the update
				context.Entry(analytics).State = EntityState.Detached;

				rowsAffected = await context.PartnerAnalytics
					.Where(a => a.PartnerId == partnerId && a.Date == today)
					.ExecuteUpdateAsync(setters =>
					{
						if (incrementImpression)
						{
							setters.SetProperty(a => a.Impressions, a => a.Impressions + 1);
						}

						if (incrementClick)
						{
							setters.SetProperty(a => a.Clicks, a => a.Clicks + 1);
						}
					}, cancellationToken);

				if (rowsAffected == 0)
				{
					logger.LogError("Failed to increment {AnalyticsType} for partner {PartnerId} on {Date}: no rows affected after retry", analyticsType, partnerId, today);
					return new Error<string>($"Failed to record {analyticsTypeName}: analytics record not found");
				}

				return new Success();
			}
			catch (ReferenceConstraintException ex)
			{
				logger.LogError(ex, "Foreign key violation when recording {AnalyticsType} for partner {PartnerId}", analyticsTypeName, partnerId);
				return new Error<string>($"Partner with ID '{partnerId}' not found");
			}
		}
		catch (Exception ex) when (ex is not UniqueConstraintException and not ReferenceConstraintException)
		{
			logger.LogError(ex, "Failed to record {AnalyticsType} for partner {PartnerId}", analyticsTypeName, partnerId);
			return new Error<string>($"Failed to record {analyticsTypeName}");
		}
	}

	public async Task<OneOf<string, Error<string>>> UploadPreviewImageAsync(
		Guid partnerId,
		Stream imageStream,
		string fileName,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			// Validate file name and extension first (before buffering)
			if (string.IsNullOrWhiteSpace(fileName))
			{
				return new Error<string>("File name is required");
			}

			var extension = Path.GetExtension(fileName).ToLowerInvariant();
			if (!AllowedImageFormats.Contains(extension))
			{
				return new Error<string>($"Image format not allowed. Allowed formats: {string.Join(", ", AllowedImageFormats)}");
			}

			// Buffer the stream to avoid issues with non-seekable streams and Stream.Length
			var bufferResult = await BufferStreamAsync(imageStream, cancellationToken);
			if (bufferResult.IsT1)
			{
				return bufferResult.AsT1;
			}

			using var bufferedStream = bufferResult.AsT0;

			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var partner = await context.Partners
				.AsNoTracking()
				.FirstOrDefaultAsync(s => s.Id == partnerId, cancellationToken);

			if (partner is null)
			{
				return new Error<string>("Partner not found");
			}

			// Validate that the current user owns this partner
			if (partner.UserId != currentUser.Id)
			{
				return new Error<string>("Unauthorized");
			}

			// Upload preview image (lifecycle policy will handle automatic deletion after 90 days)
			var previewUrl = await imageService.UploadImageAsync(
				$"preview/{partnerId}/{Guid.NewGuid():N}{extension}",
				PartnerAdsContainer,
				bufferedStream,
				cancellationToken);

			return previewUrl;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to upload preview image for partner {PartnerId}", partnerId);
			return new Error<string>("Failed to upload preview image");
		}
	}

	private static async Task<OneOf<MemoryStream, Error<string>>> BufferStreamAsync(Stream inputStream, CancellationToken cancellationToken)
	{
		var buffer = new MemoryStream();
		var tempBuffer = new byte[81920]; // 80KB chunks
		long totalBytesRead = 0;

		try
		{
			int bytesRead;
			while ((bytesRead = await inputStream.ReadAsync(tempBuffer, cancellationToken)) > 0)
			{
				totalBytesRead += bytesRead;
				if (totalBytesRead > MaxImageSizeBytes)
				{
					buffer.Dispose();
					return new Error<string>("Image exceeds maximum size of 5MB");
				}

				await buffer.WriteAsync(tempBuffer.AsMemory(0, bytesRead), cancellationToken);
			}

			buffer.Position = 0;
			return buffer;
		}
		catch
		{
			buffer.Dispose();
			throw;
		}
	}

	private static string? ExtractBlobPathFromUri(Uri uri, string containerName)
	{
		// URI path is like "/PartnerAds/partnerId_ad_timestamp.jpg"
		// We need to return "partnerId_ad_timestamp.jpg" (path relative to container)
		var segments = uri.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

		// Find the container segment and return everything after it
		var containerIndex = Array.FindIndex(segments, s => s.Equals(containerName, StringComparison.OrdinalIgnoreCase));
		if (containerIndex >= 0 && containerIndex < segments.Length - 1)
		{
			return string.Join("/", segments.Skip(containerIndex + 1));
		}

		// Fallback: if container not found, return the path without leading slash
		// This handles cases where the path might not include the container
		return uri.LocalPath.TrimStart('/');
	}

	public async Task<OneOf<Success, Error<string>>> UploadPendingChangesAsync(
		Guid partnerId,
		Stream? adImageStream,
		string? adImageFileName,
		string? name,
		string? description,
		Stream? logoStream,
		string? logoFileName,
		string? partnerPageLink,
		string? adLink,
		string? adLabelColor,
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

			// Validate URLs first (before any uploads to avoid orphaned blobs)
			string? validatedPartnerPageLink = null;
			if (!string.IsNullOrWhiteSpace(partnerPageLink))
			{
				var trimmedLink = partnerPageLink.Trim();
				if (!Uri.TryCreate(trimmedLink, UriKind.Absolute, out var uri) ||
					(uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
				{
					return new Error<string>("Ugyldig partnerside link URL");
				}

				validatedPartnerPageLink = trimmedLink;
			}

			string? validatedAdLink = null;
			if (!string.IsNullOrWhiteSpace(adLink))
			{
				var trimmedLink = adLink.Trim();
				if (!Uri.TryCreate(trimmedLink, UriKind.Absolute, out var uri) ||
					(uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
				{
					return new Error<string>("Ugyldig annonce link URL");
				}

				validatedAdLink = trimmedLink;
			}

			// Validate ad label color (should be hex color like #FFFFFF)
			string? validatedAdLabelColor = null;
			if (!string.IsNullOrWhiteSpace(adLabelColor))
			{
				var trimmedColor = adLabelColor.Trim();
				if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedColor, "^#[0-9A-Fa-f]{6}$"))
				{
					return new Error<string>("Ugyldig farve. Brug hex format som #FFFFFF");
				}

				validatedAdLabelColor = trimmedColor;
			}

			var hasChanges = false;
			MemoryStream? bufferedAdImageStream = null;
			MemoryStream? bufferedLogoStream = null;

			try
			{
				// Upload ad image if provided
				if (adImageStream is not null)
				{
					// Validate file name and extension first
					if (string.IsNullOrWhiteSpace(adImageFileName))
					{
						return new Error<string>("Ad image file name is required");
					}

					var extension = Path.GetExtension(adImageFileName).ToLowerInvariant();
					if (!AllowedImageFormats.Contains(extension))
					{
						return new Error<string>($"Ad image format not allowed. Allowed formats: {string.Join(", ", AllowedImageFormats)}");
					}

					// Buffer the stream to avoid issues with non-seekable streams
					var bufferResult = await BufferStreamAsync(adImageStream, cancellationToken);
					if (bufferResult.IsT1)
					{
						return new Error<string>($"Ad image: {bufferResult.AsT1.Value}");
					}

					bufferedAdImageStream = bufferResult.AsT0;

					var adImageUrl = await imageService.UploadImageAsync(
						$"{partnerId}_ad_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}",
						PartnerAdsContainer,
						bufferedAdImageStream,
						cancellationToken);

					partner.PendingAdImageUrl = adImageUrl;
					hasChanges = true;
				}

				// Upload logo if provided
				if (logoStream is not null)
				{
					// Validate file name and extension first
					if (string.IsNullOrWhiteSpace(logoFileName))
					{
						return new Error<string>("Logo file name is required");
					}

					var extension = Path.GetExtension(logoFileName).ToLowerInvariant();
					if (!AllowedImageFormats.Contains(extension))
					{
						return new Error<string>($"Logo format not allowed. Allowed formats: {string.Join(", ", AllowedImageFormats)}");
					}

					// Buffer the stream to avoid issues with non-seekable streams
					var bufferResult = await BufferStreamAsync(logoStream, cancellationToken);
					if (bufferResult.IsT1)
					{
						return new Error<string>($"Logo: {bufferResult.AsT1.Value}");
					}

					bufferedLogoStream = bufferResult.AsT0;

					var logoUrl = await imageService.UploadImageAsync(
						$"{partnerId}_logo_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}",
						PartnerAdsContainer,
						bufferedLogoStream,
						cancellationToken);

					partner.PendingLogoUrl = logoUrl;
					hasChanges = true;
				}
			}
			finally
			{
				bufferedAdImageStream?.Dispose();
				bufferedLogoStream?.Dispose();
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

			if (validatedPartnerPageLink is not null)
			{
				partner.PendingPartnerPageLink = validatedPartnerPageLink;
				hasChanges = true;
			}

			if (validatedAdLink is not null)
			{
				partner.PendingAdLink = validatedAdLink;
				hasChanges = true;
			}

			if (validatedAdLabelColor is not null)
			{
				partner.PendingAdLabelColor = validatedAdLabelColor;
				hasChanges = true;
			}

			if (hasChanges)
			{
				partner.HasPendingApproval = true;
				partner.LastUpdateUtc = DateTime.UtcNow;
				context.Partners.Update(partner);
				await context.SaveChangesAsync(cancellationToken);

				// Send notification email to admin with null-safe partner name
				var displayName = partner.PendingName ?? partner.Name ?? "Unknown Partner";
				await emailService.SendPartnerImageApprovalEmailAsync(partner.Id, displayName, cancellationToken);

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

			// Extract blob paths for old images that will be replaced (delete after successful DB commit)
			string? oldAdImageBlobPath = null;
			string? oldLogoBlobPath = null;

			if (!string.IsNullOrEmpty(partner.AdImageUrl) && !string.IsNullOrEmpty(partner.PendingAdImageUrl))
			{
				if (Uri.TryCreate(partner.AdImageUrl, UriKind.Absolute, out var adUri))
				{
					oldAdImageBlobPath = ExtractBlobPathFromUri(adUri, PartnerAdsContainer);
				}
				else
				{
					logger.LogWarning("Invalid ad image URL format: {AdImageUrl}", partner.AdImageUrl);
				}
			}

			if (!string.IsNullOrEmpty(partner.LogoUrl) && !string.IsNullOrEmpty(partner.PendingLogoUrl))
			{
				if (Uri.TryCreate(partner.LogoUrl, UriKind.Absolute, out var logoUri))
				{
					oldLogoBlobPath = ExtractBlobPathFromUri(logoUri, PartnerAdsContainer);
				}
				else
				{
					logger.LogWarning("Invalid logo URL format: {LogoUrl}", partner.LogoUrl);
				}
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

			if (!string.IsNullOrEmpty(partner.PendingPartnerPageLink))
			{
				partner.PartnerPageLink = partner.PendingPartnerPageLink;
				partner.PendingPartnerPageLink = null;
			}

			if (!string.IsNullOrEmpty(partner.PendingAdLink))
			{
				partner.AdLink = partner.PendingAdLink;
				partner.PendingAdLink = null;
			}

			if (!string.IsNullOrEmpty(partner.PendingAdLabelColor))
			{
				partner.AdLabelColor = partner.PendingAdLabelColor;
				partner.PendingAdLabelColor = null;
			}

			partner.HasPendingApproval = false;
			partner.LastUpdateUtc = DateTime.UtcNow;

			await context.SaveChangesAsync(cancellationToken);

			// Only delete old blobs after successful DB commit
			if (!string.IsNullOrEmpty(oldAdImageBlobPath))
			{
				try
				{
					await imageService.DeleteImageAsync(oldAdImageBlobPath, PartnerAdsContainer, cancellationToken);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to delete old ad image blob {BlobPath} for partner {PartnerId}", oldAdImageBlobPath, partnerId);
				}
			}

			if (!string.IsNullOrEmpty(oldLogoBlobPath))
			{
				try
				{
					await imageService.DeleteImageAsync(oldLogoBlobPath, PartnerAdsContainer, cancellationToken);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to delete old logo blob {BlobPath} for partner {PartnerId}", oldLogoBlobPath, partnerId);
				}
			}

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

			// Extract blob paths before clearing the URLs (so we can delete after successful DB commit)
			string? pendingAdImageBlobPath = null;
			string? pendingLogoBlobPath = null;

			if (!string.IsNullOrEmpty(partner.PendingAdImageUrl))
			{
				if (Uri.TryCreate(partner.PendingAdImageUrl, UriKind.Absolute, out var adUri))
				{
					pendingAdImageBlobPath = ExtractBlobPathFromUri(adUri, PartnerAdsContainer);
				}
				else
				{
					logger.LogWarning("Invalid pending ad image URL format: {PendingAdImageUrl}", partner.PendingAdImageUrl);
				}
			}

			if (!string.IsNullOrEmpty(partner.PendingLogoUrl))
			{
				if (Uri.TryCreate(partner.PendingLogoUrl, UriKind.Absolute, out var logoUri))
				{
					pendingLogoBlobPath = ExtractBlobPathFromUri(logoUri, PartnerAdsContainer);
				}
				else
				{
					logger.LogWarning("Invalid pending logo URL format: {PendingLogoUrl}", partner.PendingLogoUrl);
				}
			}

			// Clear pending fields in DB first
			partner.PendingAdImageUrl = null;
			partner.PendingLogoUrl = null;
			partner.PendingName = null;
			partner.PendingDescription = null;
			partner.PendingPartnerPageLink = null;
			partner.PendingAdLink = null;
			partner.PendingAdLabelColor = null;
			partner.HasPendingApproval = false;

			await context.SaveChangesAsync(cancellationToken);

			// Only delete blobs after successful DB commit
			if (!string.IsNullOrEmpty(pendingAdImageBlobPath))
			{
				try
				{
					await imageService.DeleteImageAsync(pendingAdImageBlobPath, PartnerAdsContainer, cancellationToken);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to delete pending ad image blob {BlobPath} for partner {PartnerId}", pendingAdImageBlobPath, partnerId);
				}
			}

			if (!string.IsNullOrEmpty(pendingLogoBlobPath))
			{
				try
				{
					await imageService.DeleteImageAsync(pendingLogoBlobPath, PartnerAdsContainer, cancellationToken);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to delete pending logo blob {BlobPath} for partner {PartnerId}", pendingLogoBlobPath, partnerId);
				}
			}

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

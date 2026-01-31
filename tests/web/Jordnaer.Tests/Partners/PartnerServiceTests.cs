using System.Security.Claims;
using Bogus;
using FluentAssertions;
using Jordnaer.Database;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Email;
using Jordnaer.Features.Images;
using Jordnaer.Features.Partners;
using Jordnaer.Shared;
using Jordnaer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf.Types;
using Xunit;
using Claim = System.Security.Claims.Claim;

namespace Jordnaer.Tests.Partners;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(SqlServerContainerCollection))]
public class PartnerServiceTests : IAsyncLifetime
{
	private readonly PartnerService _partnerService;
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory = Substitute.For<IDbContextFactory<JordnaerDbContext>>();
	private readonly JordnaerDbContext _context;
	private readonly IImageService _imageService = Substitute.For<IImageService>();
	private readonly IEmailService _emailService = Substitute.For<IEmailService>();
	private readonly string _userProfileId;
	private const string OtherUserId = "test-other-user-partner-service";
	private const string SecondPartnerId = "test-second-partner-user";

	private readonly Faker<Partner> _partnerFaker = new Faker<Partner>("en")
		.RuleFor(s => s.Id, _ => Guid.NewGuid())
		.RuleFor(s => s.Name, f => f.Company.CompanyName())
		.RuleFor(s => s.Description, f => f.Lorem.Sentence())
		.RuleFor(s => s.PartnerPageLink, f => f.Internet.Url())
		.RuleFor(s => s.LogoUrl, f => f.Image.PicsumUrl())
		.RuleFor(s => s.AdImageUrl, f => f.Image.PicsumUrl())
		.RuleFor(s => s.CreatedUtc, f => f.Date.Past(1));

	private readonly SqlServerContainer<JordnaerDbContext> _sqlServerContainer;

	public PartnerServiceTests(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
	{
		_sqlServerContainer = sqlServerContainer;
		_context = _sqlServerContainer.CreateContext();
		_userProfileId = Guid.NewGuid().ToString();

		_contextFactory.CreateDbContextAsync().ReturnsForAnyArgs(sqlServerContainer.CreateContext());

		var currentUser = new CurrentUser
		{
			User = new ClaimsPrincipal(
				new ClaimsIdentity(
					[
						new Claim(ClaimTypes.NameIdentifier, _userProfileId),
						new Claim(ClaimTypes.Role, AppRoles.Admin)
					],
					"TestAuthType"
				))
		};

		_partnerService = new PartnerService(
			_contextFactory,
			Substitute.For<ILogger<PartnerService>>(),
			currentUser,
			_imageService,
			_emailService);
	}

	[Fact]
	public async Task GetPartnerByIdAsync_ReturnsNotFound_WhenPartnerDoesNotExist()
	{
		// Arrange
		var id = Guid.NewGuid();

		// Act
		var result = await _partnerService.GetPartnerByIdAsync(id);

		// Assert
		result.Value.Should().BeOfType<NotFound>();
	}

	[Fact]
	public async Task GetPartnerByIdAsync_ReturnsPartner_WhenPartnerExists()
	{
		// Arrange
		var partner = AddPartner();
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerService.GetPartnerByIdAsync(partner.Id);

		// Assert
		result.Value.Should().BeOfType<Partner>();
		var returnedPartner = result.AsT0;
		returnedPartner.Should().NotBeNull();
		returnedPartner.Name.Should().Be(partner.Name);
		returnedPartner.Description.Should().Be(partner.Description);
	}

	[Fact]
	public async Task GetPartnerByUserIdAsync_ReturnsNotFound_WhenPartnerDoesNotExist()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();

		// Act
		var result = await _partnerService.GetPartnerByUserIdAsync(userId);

		// Assert
		result.Value.Should().BeOfType<NotFound>();
	}

	[Fact]
	public async Task GetPartnerByUserIdAsync_ReturnsPartner_WhenPartnerExists()
	{
		// Arrange
		var partner = AddPartner();
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerService.GetPartnerByUserIdAsync(partner.UserId);

		// Assert
		result.Value.Should().BeOfType<Partner>();
		var returnedPartner = result.AsT0;
		returnedPartner.Should().NotBeNull();
		returnedPartner.UserId.Should().Be(partner.UserId);
	}

	[Fact]
	public async Task RecordImpressionAsync_CreatesNewAnalytics_WhenNoneExistForToday()
	{
		// Arrange
		var today = DateTime.UtcNow.Date;
		var partner = AddPartner();
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerService.RecordImpressionAsync(partner.Id);

		// Assert
		result.Value.Should().BeOfType<Success>();

		var analytics = await _context.PartnerAnalytics
			.FirstOrDefaultAsync(a => a.PartnerId == partner.Id && a.Date == today);

		analytics.Should().NotBeNull();
		analytics!.Impressions.Should().Be(1);
		analytics.Clicks.Should().Be(0);
	}

	[Fact]
	public async Task RecordImpressionAsync_IncrementsExistingAnalytics_WhenAnalyticsExistForToday()
	{
		// Arrange
		var today = DateTime.UtcNow.Date;
		var partner = AddPartner();
		var analytics = new PartnerAnalytics
		{
			PartnerId = partner.Id,
			Date = today,
			Impressions = 5,
			Clicks = 2
		};
		_context.PartnerAnalytics.Add(analytics);
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerService.RecordImpressionAsync(partner.Id);

		// Assert
		result.Value.Should().BeOfType<Success>();

		// Reload the entity from database to see changes made by the service
		await _context.Entry(analytics).ReloadAsync();
		analytics.Impressions.Should().Be(6);
		analytics.Clicks.Should().Be(2);
	}

	[Fact]
	public async Task RecordClickAsync_CreatesNewAnalytics_WhenNoneExistForToday()
	{
		// Arrange
		var today = DateTime.UtcNow.Date;
		var partner = AddPartner();
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerService.RecordClickAsync(partner.Id);

		// Assert
		result.Value.Should().BeOfType<Success>();

		var analytics = await _context.PartnerAnalytics
			.FirstOrDefaultAsync(a => a.PartnerId == partner.Id && a.Date == today);

		analytics.Should().NotBeNull();
		analytics!.Impressions.Should().Be(0);
		analytics!.Clicks.Should().Be(1);
	}

	[Fact]
	public async Task RecordClickAsync_IncrementsExistingAnalytics_WhenAnalyticsExistForToday()
	{
		// Arrange
		var today = DateTime.UtcNow.Date;
		var partner = AddPartner();
		var analytics = new PartnerAnalytics
		{
			PartnerId = partner.Id,
			Date = today,
			Impressions = 10,
			Clicks = 3
		};
		_context.PartnerAnalytics.Add(analytics);
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerService.RecordClickAsync(partner.Id);

		// Assert
		result.Value.Should().BeOfType<Success>();

		// Reload the entity from database to see changes made by the service
		await _context.Entry(analytics).ReloadAsync();
		analytics.Impressions.Should().Be(10);
		analytics.Clicks.Should().Be(4);
	}

	[Fact]
	public async Task GetAnalyticsAsync_ReturnsEmptyAnalytics_WhenNoDataExists()
	{
		// Arrange
		var partner = AddPartner();
		await _context.SaveChangesAsync();

		var fromDate = DateTime.UtcNow.AddDays(-7);
		var toDate = DateTime.UtcNow;

		// Act
		var result = await _partnerService.GetAnalyticsAsync(partner.Id, fromDate, toDate);

		// Assert
		result.IsT0.Should().BeTrue();
		var analytics = result.AsT0;
		analytics.Should().NotBeNull();
		analytics.TotalImpressions.Should().Be(0);
		analytics.TotalClicks.Should().Be(0);
		analytics.ClickThroughRate.Should().Be(0);
		analytics.DailyAnalytics.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAnalyticsAsync_ReturnsNotFound_WhenPartnerDoesNotExist()
	{
		// Arrange
		var partnerId = Guid.NewGuid();
		var fromDate = DateTime.UtcNow.AddDays(-7);
		var toDate = DateTime.UtcNow;

		// Act
		var result = await _partnerService.GetAnalyticsAsync(partnerId, fromDate, toDate);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Should().BeOfType<NotFound>();
	}

	[Fact]
	public async Task GetAnalyticsAsync_ReturnsAggregatedData_WhenDataExists()
	{
		// Arrange
		var partner = AddPartner();
		var today = DateTime.UtcNow.Date;

		_context.PartnerAnalytics.Add(new PartnerAnalytics
		{
			PartnerId = partner.Id,
			Date = today.AddDays(-2),
			Impressions = 100,
			Clicks = 5
		});

		_context.PartnerAnalytics.Add(new PartnerAnalytics
		{
			PartnerId = partner.Id,
			Date = today.AddDays(-1),
			Impressions = 200,
			Clicks = 10
		});

		await _context.SaveChangesAsync();

		var fromDate = today.AddDays(-7);
		var toDate = today;

		// Act
		var result = await _partnerService.GetAnalyticsAsync(partner.Id, fromDate, toDate);

		// Assert
		result.IsT0.Should().BeTrue();
		var analytics = result.AsT0;
		analytics.Should().NotBeNull();
		analytics.TotalImpressions.Should().Be(300);
		analytics.TotalClicks.Should().Be(15);
		analytics.ClickThroughRate.Should().Be(5); // 15/300 * 100 = 5%
		analytics.DailyAnalytics.Should().HaveCount(2);
	}

	[Fact]
	public async Task UploadPendingChangesAsync_ReturnsError_WhenPartnerNotFound()
	{
		// Arrange
		var partnerId = Guid.NewGuid();
		var stream = new MemoryStream([1, 2, 3]);

		// Act
		var result = await _partnerService.UploadPendingChangesAsync(partnerId, stream, "image.png", null, null, null, null, null, null, null);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Be("Partner not found");
	}

	[Fact]
	public async Task UploadPendingChangesAsync_ReturnsError_WhenUserNotAuthorized()
	{
		// Arrange
		var partner = AddPartner();
		partner.UserId = OtherUserId; // Different user
		await _context.SaveChangesAsync();

		var stream = new MemoryStream([1, 2, 3]);

		// Act
		var result = await _partnerService.UploadPendingChangesAsync(partner.Id, stream, "image.png", null, null, null, null, null, null, null);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Be("Unauthorized");
	}

	[Fact]
	public async Task UploadPendingChangesAsync_UploadsImagesAndSendsEmail_WhenValid()
	{
		// Arrange
		var partner = AddPartner();
		partner.UserId = _userProfileId; // Same user as CurrentUser
		await _context.SaveChangesAsync();

		var adImageStream = new MemoryStream([1, 2, 3]);

		_imageService.UploadImageAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<Stream>(),
			Arg.Any<CancellationToken>())
			.Returns("https://example.com/image.png");

		// Act
		var result = await _partnerService.UploadPendingChangesAsync(partner.Id, adImageStream, "ad.png", null, null, null, null, null, null, null);

		// Assert
		result.IsT0.Should().BeTrue();

		await _imageService.Received(1).UploadImageAsync(
			Arg.Any<string>(),
			"partner-ads",
			Arg.Any<Stream>(),
			Arg.Any<CancellationToken>());

		await _emailService.Received(1).SendPartnerImageApprovalEmailAsync(
			partner.Id,
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());

		// Reload the entity from database to see changes made by the service
		await _context.Entry(partner).ReloadAsync();
		partner.HasPendingApproval.Should().BeTrue();
		partner.PendingAdImageUrl.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task ApproveChangesAsync_ReturnsError_WhenPartnerNotFound()
	{
		// Arrange
		var partnerId = Guid.NewGuid();

		// Act
		var result = await _partnerService.ApproveChangesAsync(partnerId);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Be("Partner not found");
	}

	[Fact]
	public async Task ApproveChangesAsync_MovesImagesToActive_WhenValid()
	{
		// Arrange
		var partner = AddPartner();
		partner.PendingAdImageUrl = "https://example.com/pending-ad.png";
		partner.HasPendingApproval = true;
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerService.ApproveChangesAsync(partner.Id);

		// Assert
		result.IsT0.Should().BeTrue();

		// Reload the entity from database to see changes made by the service
		await _context.Entry(partner).ReloadAsync();
		partner.AdImageUrl.Should().Be("https://example.com/pending-ad.png");
		partner.PendingAdImageUrl.Should().BeNull();
		partner.HasPendingApproval.Should().BeFalse();
	}

	[Fact]
	public async Task RejectChangesAsync_ReturnsError_WhenPartnerNotFound()
	{
		// Arrange
		var partnerId = Guid.NewGuid();

		// Act
		var result = await _partnerService.RejectChangesAsync(partnerId);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Be("Partner not found");
	}

	[Fact]
	public async Task RejectChangesAsync_DeletesPendingImages_WhenValid()
	{
		// Arrange
		var partner = AddPartner();
		partner.PendingAdImageUrl = "https://example.com/pending-ad.png";
		partner.HasPendingApproval = true;
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerService.RejectChangesAsync(partner.Id);

		// Assert
		result.IsT0.Should().BeTrue();

		await _imageService.Received(1).DeleteImageAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());

		// Reload the entity from database to see changes made by the service
		await _context.Entry(partner).ReloadAsync();
		partner.PendingAdImageUrl.Should().BeNull();
		partner.HasPendingApproval.Should().BeFalse();
	}

	private Partner AddPartner(string? userId = null)
	{
		var partner = _partnerFaker.Generate();
		partner.UserId = userId ?? _userProfileId;
		// Ensure unique name to avoid IX_Partners_Name unique index violations
		partner.Name = $"{partner.Name} {Guid.NewGuid()}";
		_context.Partners.Add(partner);
		return partner;
	}

	public async Task InitializeAsync()
	{
		// Create the ApplicationUser that tests will reference (if not exists)
		var existingUser = await _context.Users.FindAsync(_userProfileId);
		if (existingUser == null)
		{
			var user = new ApplicationUser
			{
				Id = _userProfileId,
				UserName = "test@example.com",
				Email = "test@example.com",
				EmailConfirmed = true
			};
			_context.Users.Add(user);
		}

		// Create a second user for unauthorized tests (if not exists)
		var existingOtherUser = await _context.Users.FindAsync(OtherUserId);
		if (existingOtherUser == null)
		{
			var otherUser = new ApplicationUser
			{
				Id = OtherUserId,
				UserName = "other@example.com",
				Email = "other@example.com",
				EmailConfirmed = true
			};
			_context.Users.Add(otherUser);
		}

		// Create a third user for tests that need multiple partners (if not exists)
		var existingSecondPartner = await _context.Users.FindAsync(SecondPartnerId);
		if (existingSecondPartner == null)
		{
			var secondPartner = new ApplicationUser
			{
				Id = SecondPartnerId,
				UserName = "second@example.com",
				Email = "second@example.com",
				EmailConfirmed = true
			};
			_context.Users.Add(secondPartner);
		}

		await _context.SaveChangesAsync();
	}

	public async Task DisposeAsync()
	{
		await _context.DisposeAsync();
	}
}

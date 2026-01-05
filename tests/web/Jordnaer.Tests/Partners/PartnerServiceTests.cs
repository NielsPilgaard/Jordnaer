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
public class PartnerUserServiceTests : IAsyncLifetime
{
	private readonly PartnerUserService _partnerUserService;
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory = Substitute.For<IDbContextFactory<JordnaerDbContext>>();
	private readonly JordnaerDbContext _context;
	private readonly IImageService _imageService = Substitute.For<IImageService>();
	private readonly IEmailService _emailService = Substitute.For<IEmailService>();
	private readonly string _userProfileId;

	private readonly Faker<Partner> _sponsorFaker = new Faker<Partner>("en")
		.RuleFor(s => s.Id, _ => Guid.NewGuid())
		.RuleFor(s => s.Name, f => f.Company.CompanyName())
		.RuleFor(s => s.Description, f => f.Lorem.Sentence())
		.RuleFor(s => s.Link, f => f.Internet.Url())
		.RuleFor(s => s.LogoUrl, f => f.Image.PicsumUrl())
		.RuleFor(s => s.MobileImageUrl, f => f.Image.PicsumUrl())
		.RuleFor(s => s.DesktopImageUrl, f => f.Image.PicsumUrl())
		.RuleFor(s => s.CreatedUtc, f => f.Date.Past(1));

	private readonly SqlServerContainer<JordnaerDbContext> _sqlServerContainer;

	public PartnerUserServiceTests(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
	{
		_sqlServerContainer = sqlServerContainer;
		_context = _sqlServerContainer.CreateContext();
		_userProfileId = Guid.NewGuid().ToString();

		_contextFactory.CreateDbContextAsync().ReturnsForAnyArgs(sqlServerContainer.CreateContext());

		var currentUser = new CurrentUser
		{
			User = new ClaimsPrincipal(
				new ClaimsIdentity(
					[new Claim(ClaimTypes.NameIdentifier, _userProfileId)]
				))
		};

		_partnerUserService = new PartnerUserService(
			_contextFactory,
			Substitute.For<ILogger<PartnerUserService>>(),
			currentUser,
			_imageService,
			_emailService);
	}

	[Fact]
	public async Task GetSponsorByIdAsync_ReturnsNotFound_WhenSponsorDoesNotExist()
	{
		// Arrange
		var id = Guid.NewGuid();

		// Act
		var result = await _partnerUserService.GetSponsorByIdAsync(id);

		// Assert
		result.Value.Should().BeOfType<NotFound>();
	}

	[Fact]
	public async Task GetSponsorByIdAsync_ReturnsSponsor_WhenSponsorExists()
	{
		// Arrange
		var partner = AddSponsor();
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerUserService.GetSponsorByIdAsync(partner.Id);

		// Assert
		result.Value.Should().BeOfType<Partner>();
		var returnedSponsor = result.AsT0;
		returnedSponsor.Should().NotBeNull();
		returnedSponsor.Name.Should().Be(partner.Name);
		returnedSponsor.Description.Should().Be(partner.Description);
	}

	[Fact]
	public async Task GetSponsorByUserIdAsync_ReturnsNotFound_WhenSponsorDoesNotExist()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();

		// Act
		var result = await _partnerUserService.GetSponsorByUserIdAsync(userId);

		// Assert
		result.Value.Should().BeOfType<NotFound>();
	}

	[Fact]
	public async Task GetSponsorByUserIdAsync_ReturnsSponsor_WhenSponsorExists()
	{
		// Arrange
		var partner = AddSponsor();
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerUserService.GetSponsorByUserIdAsync(partner.UserId);

		// Assert
		result.Value.Should().BeOfType<Partner>();
		var returnedSponsor = result.AsT0;
		returnedSponsor.Should().NotBeNull();
		returnedSponsor.UserId.Should().Be(partner.UserId);
	}

	[Fact]
	public async Task GetAllSponsorsAsync_ReturnsEmptyList_WhenNoSponsorsExist()
	{
		// Act
		var result = await _partnerUserService.GetAllSponsorsAsync();

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllSponsorsAsync_ReturnsAllSponsors_WhenSponsorsExist()
	{
		// Arrange
		var sponsor1 = AddSponsor();
		var sponsor2 = AddSponsor();
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerUserService.GetAllSponsorsAsync();

		// Assert
		result.Should().HaveCount(2);
		result.Should().Contain(s => s.Id == sponsor1.Id);
		result.Should().Contain(s => s.Id == sponsor2.Id);
	}

	[Fact]
	public async Task RecordImpressionAsync_CreatesNewAnalytics_WhenNoneExistForToday()
	{
		// Arrange
		var partner = AddSponsor();
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerUserService.RecordImpressionAsync(partner.Id);

		// Assert
		result.Value.Should().BeOfType<Success>();

		var analytics = await _context.SponsorAnalytics
			.FirstOrDefaultAsync(a => a.PartnerId == partner.Id && a.Date == DateTime.UtcNow.Date);

		analytics.Should().NotBeNull();
		analytics!.Impressions.Should().Be(1);
		analytics.Clicks.Should().Be(0);
	}

	[Fact]
	public async Task RecordImpressionAsync_IncrementsExistingAnalytics_WhenAnalyticsExistForToday()
	{
		// Arrange
		var partner = AddSponsor();
		var analytics = new SponsorAnalytics
		{
			PartnerId = partner.Id,
			Date = DateTime.UtcNow.Date,
			Impressions = 5,
			Clicks = 2
		};
		_context.SponsorAnalytics.Add(analytics);
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerUserService.RecordImpressionAsync(partner.Id);

		// Assert
		result.Value.Should().BeOfType<Success>();

		var updatedAnalytics = await _context.SponsorAnalytics
			.FirstOrDefaultAsync(a => a.PartnerId == partner.Id && a.Date == DateTime.UtcNow.Date);

		updatedAnalytics.Should().NotBeNull();
		updatedAnalytics!.Impressions.Should().Be(6);
		updatedAnalytics.Clicks.Should().Be(2);
	}

	[Fact]
	public async Task RecordClickAsync_CreatesNewAnalytics_WhenNoneExistForToday()
	{
		// Arrange
		var partner = AddSponsor();
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerUserService.RecordClickAsync(partner.Id);

		// Assert
		result.Value.Should().BeOfType<Success>();

		var analytics = await _context.SponsorAnalytics
			.FirstOrDefaultAsync(a => a.PartnerId == partner.Id && a.Date == DateTime.UtcNow.Date);

		analytics.Should().NotBeNull();
		analytics!.Impressions.Should().Be(0);
		analytics!.Clicks.Should().Be(1);
	}

	[Fact]
	public async Task RecordClickAsync_IncrementsExistingAnalytics_WhenAnalyticsExistForToday()
	{
		// Arrange
		var partner = AddSponsor();
		var analytics = new SponsorAnalytics
		{
			PartnerId = partner.Id,
			Date = DateTime.UtcNow.Date,
			Impressions = 10,
			Clicks = 3
		};
		_context.SponsorAnalytics.Add(analytics);
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerUserService.RecordClickAsync(partner.Id);

		// Assert
		result.Value.Should().BeOfType<Success>();

		var updatedAnalytics = await _context.SponsorAnalytics
			.FirstOrDefaultAsync(a => a.PartnerId == partner.Id && a.Date == DateTime.UtcNow.Date);

		updatedAnalytics.Should().NotBeNull();
		updatedAnalytics!.Impressions.Should().Be(10);
		updatedAnalytics!.Clicks.Should().Be(4);
	}

	[Fact]
	public async Task GetAnalyticsAsync_ReturnsEmptyAnalytics_WhenNoDataExists()
	{
		// Arrange
		var partner = AddSponsor();
		await _context.SaveChangesAsync();

		var fromDate = DateTime.UtcNow.AddDays(-7);
		var toDate = DateTime.UtcNow;

		// Act
		var result = await _partnerUserService.GetAnalyticsAsync(partner.Id, fromDate, toDate);

		// Assert
		result.Should().NotBeNull();
		result.TotalImpressions.Should().Be(0);
		result.TotalClicks.Should().Be(0);
		result.ClickThroughRate.Should().Be(0);
		result.DailyAnalytics.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAnalyticsAsync_ReturnsAggregatedData_WhenDataExists()
	{
		// Arrange
		var partner = AddSponsor();
		var today = DateTime.UtcNow.Date;

		_context.SponsorAnalytics.Add(new SponsorAnalytics
		{
			PartnerId = partner.Id,
			Date = today.AddDays(-2),
			Impressions = 100,
			Clicks = 5
		});

		_context.SponsorAnalytics.Add(new SponsorAnalytics
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
		var result = await _partnerUserService.GetAnalyticsAsync(partner.Id, fromDate, toDate);

		// Assert
		result.Should().NotBeNull();
		result.TotalImpressions.Should().Be(300);
		result.TotalClicks.Should().Be(15);
		result.ClickThroughRate.Should().Be(5); // 15/300 * 100 = 5%
		result.DailyAnalytics.Should().HaveCount(2);
	}

	[Fact]
	public async Task UploadPendingImagesAsync_ReturnsError_WhenSponsorNotFound()
	{
		// Arrange
		var partnerId = Guid.NewGuid();
		var stream = new MemoryStream([1, 2, 3]);

		// Act
		var result = await _partnerUserService.UploadPendingImagesAsync(partnerId, stream, null);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Be("Partner not found");
	}

	[Fact]
	public async Task UploadPendingImagesAsync_ReturnsError_WhenUserNotAuthorized()
	{
		// Arrange
		var partner = AddSponsor();
		partner.UserId = Guid.NewGuid().ToString(); // Different user
		await _context.SaveChangesAsync();

		var stream = new MemoryStream([1, 2, 3]);

		// Act
		var result = await _partnerUserService.UploadPendingImagesAsync(partner.Id, stream, null);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Be("Unauthorized");
	}

	[Fact]
	public async Task UploadPendingImagesAsync_UploadsImagesAndSendsEmail_WhenValid()
	{
		// Arrange
		var partner = AddSponsor();
		partner.UserId = _userProfileId; // Same user as CurrentUser
		await _context.SaveChangesAsync();

		var mobileStream = new MemoryStream([1, 2, 3]);
		var desktopStream = new MemoryStream([4, 5, 6]);

		_imageService.UploadImageAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<Stream>(),
			Arg.Any<CancellationToken>())
			.Returns("https://example.com/image.png");

		// Act
		var result = await _partnerUserService.UploadPendingImagesAsync(partner.Id, mobileStream, desktopStream);

		// Assert
		result.IsT0.Should().BeTrue();

		await _imageService.Received(2).UploadImageAsync(
			Arg.Any<string>(),
			"partner-ads",
			Arg.Any<Stream>(),
			Arg.Any<CancellationToken>());

		await _emailService.Received(1).SendSponsorImageApprovalEmailAsync(
			partner.Id,
			partner.Name,
			Arg.Any<CancellationToken>());

		var updatedSponsor = await _context.Partners.FirstAsync(s => s.Id == partner.Id);
		updatedSponsor.HasPendingImageApproval.Should().BeTrue();
		updatedSponsor.PendingMobileImageUrl.Should().NotBeNullOrEmpty();
		updatedSponsor.PendingDesktopImageUrl.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task ApproveImagesAsync_ReturnsError_WhenSponsorNotFound()
	{
		// Arrange
		var partnerId = Guid.NewGuid();

		// Act
		var result = await _partnerUserService.ApproveImagesAsync(partnerId);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Be("Partner not found");
	}

	[Fact]
	public async Task ApproveImagesAsync_MovesImagesToActive_WhenValid()
	{
		// Arrange
		var partner = AddSponsor();
		partner.PendingMobileImageUrl = "https://example.com/pending-mobile.png";
		partner.PendingDesktopImageUrl = "https://example.com/pending-desktop.png";
		partner.HasPendingImageApproval = true;
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerUserService.ApproveImagesAsync(partner.Id);

		// Assert
		result.IsT0.Should().BeTrue();

		var updatedSponsor = await _context.Partners.FirstAsync(s => s.Id == partner.Id);
		updatedSponsor.MobileImageUrl.Should().Be("https://example.com/pending-mobile.png");
		updatedSponsor.DesktopImageUrl.Should().Be("https://example.com/pending-desktop.png");
		updatedSponsor.PendingMobileImageUrl.Should().BeNull();
		updatedSponsor.PendingDesktopImageUrl.Should().BeNull();
		updatedSponsor.HasPendingImageApproval.Should().BeFalse();
	}

	[Fact]
	public async Task RejectImagesAsync_ReturnsError_WhenSponsorNotFound()
	{
		// Arrange
		var partnerId = Guid.NewGuid();

		// Act
		var result = await _partnerUserService.RejectImagesAsync(partnerId);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Be("Partner not found");
	}

	[Fact]
	public async Task RejectImagesAsync_DeletesPendingImages_WhenValid()
	{
		// Arrange
		var partner = AddSponsor();
		partner.PendingMobileImageUrl = "https://example.com/pending-mobile.png";
		partner.PendingDesktopImageUrl = "https://example.com/pending-desktop.png";
		partner.HasPendingImageApproval = true;
		await _context.SaveChangesAsync();

		// Act
		var result = await _partnerUserService.RejectImagesAsync(partner.Id);

		// Assert
		result.IsT0.Should().BeTrue();

		await _imageService.Received(2).DeleteImageAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());

		var updatedSponsor = await _context.Partners.FirstAsync(s => s.Id == partner.Id);
		updatedSponsor.PendingMobileImageUrl.Should().BeNull();
		updatedSponsor.PendingDesktopImageUrl.Should().BeNull();
		updatedSponsor.HasPendingImageApproval.Should().BeFalse();
	}

	private Partner AddSponsor()
	{
		var partner = _sponsorFaker.Generate();
		partner.UserId = _userProfileId;
		_context.Partners.Add(partner);
		return partner;
	}

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		await _context.Database.EnsureDeletedAsync();
		await _context.DisposeAsync();
	}
}

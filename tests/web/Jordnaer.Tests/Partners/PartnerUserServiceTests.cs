using Bogus;
using FluentAssertions;
using Jordnaer.Database;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Email;
using Jordnaer.Features.Partners;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Microsoft.EntityFrameworkCore.Sqlite;
namespace Jordnaer.Tests.Partners;

public class PartnerServiceTests : IAsyncLifetime
{
	private readonly JordnaerDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly IUserRoleService _userRoleService;
	private readonly IEmailService _emailService;
	private readonly ILogger<PartnerUserService> _logger;
	private readonly PartnerUserService _partnerUserService;
	private readonly Faker _faker = new("en");

	public PartnerServiceTests()
	{
		var options = new DbContextOptionsBuilder<JordnaerDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;

		_context = new JordnaerDbContext(options);

		// Setup UserManager
		var userStore = Substitute.For<IUserStore<ApplicationUser>>();
		_userManager = Substitute.For<UserManager<ApplicationUser>>(
			userStore, null, null, null, null, null, null, null, null);

		_userRoleService = Substitute.For<IUserRoleService>();
		_emailService = Substitute.For<IEmailService>();
		_logger = Substitute.For<ILogger<PartnerUserService>>();

		_partnerUserService = new PartnerUserService(
			_userManager,
			_userRoleService,
			_context,
			_emailService,
			_logger);
	}

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		await _context.DisposeAsync();
	}

	[Fact]
	public async Task CreatePartnerAccountAsync_ShouldCreateUserProfilePartnerAndSendEmail()
	{
		// Arrange
		var request = new CreatePartnerRequest
		{
			Name = _faker.Company.CompanyName(),
			Email = _faker.Internet.Email(),
			Description = _faker.Lorem.Sentence(),
			Link = _faker.Internet.Url(),
			LogoUrl = _faker.Internet.Avatar()
		};

		var createdUser = new ApplicationUser
		{
			Id = Guid.NewGuid().ToString(),
			UserName = request.Email,
			Email = request.Email,
			EmailConfirmed = true
		};

		_userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
		_userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
			.Returns(callInfo =>
			{
				var user = callInfo.ArgAt<ApplicationUser>(0);
				user.Id = createdUser.Id;
				return Task.FromResult(IdentityResult.Success);
			});

		_userRoleService.AddRoleToUserAsync(createdUser.Id, AppRoles.Partner)
			.Returns(new OneOf.Types.Success());

		// Act
		var result = await _partnerUserService.CreatePartnerAccountAsync(request);

		// Assert
		result.IsT0.Should().BeTrue();
		var partnerResult = result.AsT0;

		partnerResult.Email.Should().Be(request.Email);
		partnerResult.TemporaryPassword.Should().NotBeNullOrEmpty();
		partnerResult.TemporaryPassword.Length.Should().BeGreaterThan(10);

		// Verify user was created
		await _userManager.Received(1).CreateAsync(
			Arg.Is<ApplicationUser>(u =>
				u.Email == request.Email &&
				u.UserName == request.Email &&
				u.EmailConfirmed == true),
			Arg.Any<string>());

		// Verify UserProfile was created
		var userProfile = await _context.UserProfiles.FindAsync(createdUser.Id);
		userProfile.Should().NotBeNull();

		// Verify Partner role was assigned
		await _userRoleService.Received(1)
			.AddRoleToUserAsync(createdUser.Id, AppRoles.Partner);

		// Verify Partner was created
		var partner = await _context.Partners.FirstOrDefaultAsync(s => s.UserId == createdUser.Id);
		partner.Should().NotBeNull();
		partner!.Name.Should().Be(request.Name);
		partner.Description.Should().Be(request.Description);
		partner.Link.Should().Be(request.Link);
		partner.LogoUrl.Should().Be(request.LogoUrl);
		partner.UserId.Should().Be(createdUser.Id);

		// Verify welcome email was sent
		await _emailService.Received(1).SendPartnerWelcomeEmailAsync(
			request.Email,
			request.Name,
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CreatePartnerAccountAsync_ShouldReturnError_WhenUserAlreadyExists()
	{
		// Arrange
		var existingUser = new ApplicationUser
		{
			Id = Guid.NewGuid().ToString(),
			Email = _faker.Internet.Email(),
			UserName = _faker.Internet.Email()
		};

		_userManager.FindByEmailAsync(existingUser.Email).Returns(existingUser);

		var request = new CreatePartnerRequest
		{
			Name = _faker.Company.CompanyName(),
			Email = existingUser.Email!,
			Description = _faker.Lorem.Sentence(),
			Link = _faker.Internet.Url()
		};

		// Act
		var result = await _partnerUserService.CreatePartnerAccountAsync(request);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Contain("findes allerede");

		await _userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
	}

	[Fact]
	public async Task CreatePartnerAccountAsync_ShouldReturnError_WhenEmailIsInvalid()
	{
		// Arrange
		var request = new CreatePartnerRequest
		{
			Name = _faker.Company.CompanyName(),
			Email = "invalid-email", // Missing @
			Description = _faker.Lorem.Sentence(),
			Link = _faker.Internet.Url()
		};

		_userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);

		// Act
		var result = await _partnerUserService.CreatePartnerAccountAsync(request);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Contain("Ugyldig email");

		await _userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
	}

	[Fact]
	public async Task CreatePartnerAccountAsync_ShouldReturnError_WhenLinkIsInvalid()
	{
		// Arrange
		var request = new CreatePartnerRequest
		{
			Name = _faker.Company.CompanyName(),
			Email = _faker.Internet.Email(),
			Description = _faker.Lorem.Sentence(),
			Link = "not-a-valid-url"
		};

		_userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);

		// Act
		var result = await _partnerUserService.CreatePartnerAccountAsync(request);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Contain("Ugyldig link");

		await _userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
	}

	[Fact]
	public async Task CreatePartnerAccountAsync_ShouldReturnError_WhenUserCreationFails()
	{
		// Arrange
		var request = new CreatePartnerRequest
		{
			Name = _faker.Company.CompanyName(),
			Email = _faker.Internet.Email(),
			Description = _faker.Lorem.Sentence(),
			Link = _faker.Internet.Url()
		};

		_userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
		_userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
			.Returns(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

		// Act
		var result = await _partnerUserService.CreatePartnerAccountAsync(request);

		// Assert
		result.IsT1.Should().BeTrue();
		result.AsT1.Value.Should().Contain("Kunne ikke oprette brugerkonto");
	}

	[Fact]
	public async Task CreatePartnerAccountAsync_ShouldRollbackUser_WhenRoleAssignmentFails()
	{
		// Arrange
		var request = new CreatePartnerRequest
		{
			Name = _faker.Company.CompanyName(),
			Email = _faker.Internet.Email(),
			Description = _faker.Lorem.Sentence(),
			Link = _faker.Internet.Url()
		};

		var createdUser = new ApplicationUser
		{
			Id = Guid.NewGuid().ToString(),
			UserName = request.Email,
			Email = request.Email,
			EmailConfirmed = true
		};

		_userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
		_userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
			.Returns(callInfo =>
			{
				var user = callInfo.ArgAt<ApplicationUser>(0);
				user.Id = createdUser.Id;
				return Task.FromResult(IdentityResult.Success);
			});

		_userRoleService.AddRoleToUserAsync(createdUser.Id, AppRoles.Partner)
			.Returns(new OneOf.Types.Error<string>("Failed to add role"));

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => _partnerUserService.CreatePartnerAccountAsync(request));

		// Verify user was deleted (rollback)
		await _userManager.Received(1).DeleteAsync(Arg.Any<ApplicationUser>());
	}

	[Fact]
	public async Task CreatePartnerAccountAsync_ShouldGenerateSecurePassword()
	{
		// Arrange
		var request = new CreatePartnerRequest
		{
			Name = _faker.Company.CompanyName(),
			Email = _faker.Internet.Email(),
			Description = _faker.Lorem.Sentence(),
			Link = _faker.Internet.Url()
		};

		var createdUser = new ApplicationUser
		{
			Id = Guid.NewGuid().ToString(),
			UserName = request.Email,
			Email = request.Email
		};

		_userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
		_userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
			.Returns(callInfo =>
			{
				var user = callInfo.ArgAt<ApplicationUser>(0);
				user.Id = createdUser.Id;
				return Task.FromResult(IdentityResult.Success);
			});

		_userRoleService.AddRoleToUserAsync(createdUser.Id, AppRoles.Partner)
			.Returns(new OneOf.Types.Success());

		// Act
		var result = await _partnerUserService.CreatePartnerAccountAsync(request);

		// Assert
		result.IsT0.Should().BeTrue();
		var password = result.AsT0.TemporaryPassword;

		// Password should be in format: aBc4-Xy7z-Mn2P-Qr9T (19 chars with 3 hyphens)
		password.Length.Should().Be(19);
		password.Count(c => c == '-').Should().Be(3);

		// Should contain at least one uppercase, lowercase, and digit
		password.Should().Match(p => p.Any(char.IsUpper));
		password.Should().Match(p => p.Any(char.IsLower));
		password.Should().Match(p => p.Any(char.IsDigit));
	}

	[Fact]
	public async Task CreatePartnerAccountAsync_ShouldHandleNullLogoUrl()
	{
		// Arrange
		var request = new CreatePartnerRequest
		{
			Name = _faker.Company.CompanyName(),
			Email = _faker.Internet.Email(),
			Description = _faker.Lorem.Sentence(),
			Link = _faker.Internet.Url(),
			LogoUrl = null // No logo
		};

		var createdUser = new ApplicationUser
		{
			Id = Guid.NewGuid().ToString(),
			UserName = request.Email,
			Email = request.Email
		};

		_userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
		_userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
			.Returns(callInfo =>
			{
				var user = callInfo.ArgAt<ApplicationUser>(0);
				user.Id = createdUser.Id;
				return Task.FromResult(IdentityResult.Success);
			});

		_userRoleService.AddRoleToUserAsync(createdUser.Id, AppRoles.Partner)
			.Returns(new OneOf.Types.Success());

		// Act
		var result = await _partnerUserService.CreatePartnerAccountAsync(request);

		// Assert
		result.IsT0.Should().BeTrue();

		var partner = await _context.Partners.FirstOrDefaultAsync(s => s.UserId == createdUser.Id);
		partner.Should().NotBeNull();
		partner!.LogoUrl.Should().BeNull();
	}

	[Fact]
	public async Task ResendWelcomeEmailAsync_ShouldGenerateNewPasswordAndSendEmail()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();
		var user = new ApplicationUser
		{
			Id = userId,
			Email = _faker.Internet.Email(),
			UserName = _faker.Internet.Email()
		};

		var partner = new Partner
		{
			Id = Guid.NewGuid(),
			Name = _faker.Company.CompanyName(),
			Description = _faker.Lorem.Sentence(),
			Link = _faker.Internet.Url(),
			UserId = userId,
			CreatedUtc = DateTime.UtcNow
		};

		await _context.Partners.AddAsync(partner);
		await _context.SaveChangesAsync();

		_userManager.FindByIdAsync(userId).Returns(user);
		_userManager.RemovePasswordAsync(user).Returns(IdentityResult.Success);
		_userManager.AddPasswordAsync(user, Arg.Any<string>()).Returns(IdentityResult.Success);

		// Act
		var result = await _partnerUserService.ResendWelcomeEmailAsync(userId);

		// Assert
		result.IsT0.Should().BeTrue();

		// Verify password was reset
		await _userManager.Received(1).RemovePasswordAsync(user);
		await _userManager.Received(1).AddPasswordAsync(user, Arg.Any<string>());

		// Verify welcome email was sent
		await _emailService.Received(1).SendPartnerWelcomeEmailAsync(
			user.Email!,
			partner.Name,
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ResendWelcomeEmailAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();
		_userManager.FindByIdAsync(userId).Returns((ApplicationUser?)null);

		// Act
		var result = await _partnerUserService.ResendWelcomeEmailAsync(userId);

		// Assert
		result.IsT1.Should().BeTrue();

		await _emailService.DidNotReceive().SendPartnerWelcomeEmailAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ResendWelcomeEmailAsync_ShouldReturnNotFound_WhenPartnerDoesNotExist()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();
		var user = new ApplicationUser
		{
			Id = userId,
			Email = _faker.Internet.Email(),
			UserName = _faker.Internet.Email()
		};

		_userManager.FindByIdAsync(userId).Returns(user);

		// Act
		var result = await _partnerUserService.ResendWelcomeEmailAsync(userId);

		// Assert
		result.IsT1.Should().BeTrue();

		await _emailService.DidNotReceive().SendPartnerWelcomeEmailAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());
	}
}

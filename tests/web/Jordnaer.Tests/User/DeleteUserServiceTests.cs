using FluentAssertions;
using Jordnaer.Database;
using Jordnaer.Features.DeleteUser;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Jordnaer.Features.Email;
using Jordnaer.Features.Images;
using Jordnaer.Shared;
using Xunit;
using Jordnaer.Tests.Infrastructure;
using Microsoft.AspNetCore.Components;

namespace Jordnaer.Tests.User;

internal class NavigationManagerMock : NavigationManager
{
	public NavigationManagerMock()
	{
		Initialize("https://localhost:7116/", "https://localhost:7116/");
	}
}

[Trait("Category", "IntegrationTest")]
[Collection(nameof(SqlServerContainerCollection))]
public class DeleteUserServiceTests : IAsyncLifetime
{
	private readonly UserManager<ApplicationUser> _userManager = Substitute.For<UserManager<ApplicationUser>>(Substitute.For<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
	private readonly ILogger<DeleteUserService> _logger = Substitute.For<ILogger<DeleteUserService>>();
	private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
	private readonly NavigationManager _server = new NavigationManagerMock();
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory = Substitute.For<IDbContextFactory<JordnaerDbContext>>();
	private readonly IDiagnosticContext _diagnosticContext = Substitute.For<IDiagnosticContext>();
	private readonly DeleteUserService _deleteUserService;
	private readonly IImageService _imageService = Substitute.For<IImageService>();
	private readonly JordnaerDbContext _context;

	public DeleteUserServiceTests(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
	{
		_context = sqlServerContainer.CreateContext();

		_contextFactory.CreateDbContextAsync().ReturnsForAnyArgs(sqlServerContainer.CreateContext());

		_deleteUserService = new DeleteUserService(_userManager, _logger, _publishEndpoint, _server, _contextFactory, _diagnosticContext, _imageService);
	}

	[Fact]
	public async Task InitiateDeleteUserAsync_Should_Send_Email_On_Success()
	{
		// Arrange
		var user = new ApplicationUser { Email = "test@test.com", Id = NewId.NextGuid().ToString() };
		_context.Users.Add(new ApplicationUser { Id = user.Id });
		await _context.SaveChangesAsync();

		_userManager.GenerateUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose).Returns("token");

		_publishEndpoint.Publish(Arg.Any<SendEmail>()).Returns(Task.CompletedTask);

		// Act
		var result = await _deleteUserService.InitiateDeleteUserAsync(user.Id);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task DeleteUserAsync_Should_Delete_User_When_Token_Is_Valid()
	{
		// Arrange
		var token = NewId.NextGuid().ToString();
		var user = new ApplicationUser { Email = "test@test.com", Id = NewId.NextGuid().ToString() };
		_context.Users.Add(user);
		_context.UserProfiles.Add(new UserProfile { Id = user.Id });
		await _context.SaveChangesAsync();

		_userManager.VerifyUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose, token)
					.ReturnsForAnyArgs(true);

		_userManager.DeleteAsync(user)
					.ReturnsForAnyArgs(IdentityResult.Success);

		// Act
		var result = await _deleteUserService.DeleteUserAsync(user.Id, token);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task DeleteUserAsync_Should_Not_Delete_User_When_Token_Is_Invalid()
	{
		// Arrange
		var user = new ApplicationUser { Email = "test@test.com", Id = NewId.NextGuid().ToString() };
		_userManager.VerifyUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose, "token").Returns(false);

		// Act
		var result = await _deleteUserService.DeleteUserAsync(user.Id, "token");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task DeleteUserAsync_Should_Not_Delete_User_When_DeleteAsync_Fails()
	{
		// Arrange
		var user = new ApplicationUser { Email = "test@test.com", Id = NewId.NextGuid().ToString() };
		_userManager.VerifyUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose, "token").Returns(true);
		_userManager.DeleteAsync(user).Returns(IdentityResult.Failed(new IdentityError()));

		// Act
		var result = await _deleteUserService.DeleteUserAsync(user.Id, "token");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task DeleteUserAsync_Should_Not_Delete_User_When_ExecuteDeleteAsync_Fails()
	{
		// Arrange
		var user = new ApplicationUser { Email = "test@test.com", Id = NewId.NextGuid().ToString() };
		_userManager.VerifyUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose, "token").Returns(true);
		_userManager.DeleteAsync(user).Returns(IdentityResult.Success);

		// Act
		var result = await _deleteUserService.DeleteUserAsync(user.Id, "token");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task DeleteUserAsync_Should_Not_Delete_User_When_Exception_Thrown()
	{
		// Arrange
		var user = new ApplicationUser { Email = "test@test.com", Id = NewId.NextGuid().ToString() };
		_userManager.VerifyUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose, "token").Returns(true);
		_userManager.DeleteAsync(user).ThrowsAsync(new Exception());

		// Act
		var result = await _deleteUserService.DeleteUserAsync(user.Id, "token");

		// Assert
		result.Should().BeFalse();
	}

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() => await _context.DisposeAsync();
}

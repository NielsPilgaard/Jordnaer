using FluentAssertions;
using Jordnaer.Database;
using Jordnaer.Features.DeleteUser;
using Jordnaer.Features.Profile;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog;
using System.Net;
using Xunit;
using Response = SendGrid.Response;

namespace Jordnaer.Tests;

public class DeleteUserServiceTests : IClassFixture<SqlServerContainer<JordnaerDbContext>>
{
	private readonly UserManager<ApplicationUser> _userManager = Substitute.For<UserManager<ApplicationUser>>(Substitute.For<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
	private readonly ILogger<DeleteUserService> _logger = Substitute.For<ILogger<DeleteUserService>>();
	private readonly ISendGridClient _sendGridClient = Substitute.For<ISendGridClient>();
	private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory = Substitute.For<IDbContextFactory<JordnaerDbContext>>();
	private readonly IDiagnosticContext _diagnosticContext = Substitute.For<IDiagnosticContext>();
	private readonly DeleteUserService _deleteUserService;
	private readonly IImageService _imageService = Substitute.For<IImageService>();
	private readonly AuthenticationStateProvider _authenticationStateProvider = Substitute.For<AuthenticationStateProvider>();

	public DeleteUserServiceTests(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
	{
		_contextFactory.CreateDbContextAsync().ReturnsForAnyArgs(sqlServerContainer.Context);

		_deleteUserService = new DeleteUserService(_userManager, _logger, _sendGridClient, _httpContextAccessor, _contextFactory, _diagnosticContext, _imageService, _authenticationStateProvider);
	}

	[Fact]
	public async Task InitiateDeleteUserAsync_Should_Send_Email_On_Success()
	{
		// Arrange
		var user = new ApplicationUser { Email = "test@test.com", Id = NewId.NextGuid().ToString() };
		_userManager.GenerateUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose).Returns("token");
		_httpContextAccessor.HttpContext!.Request.Scheme.Returns("https");
		_httpContextAccessor.HttpContext!.Request.Host.Returns(new HostString("localhost"));
		_sendGridClient.SendEmailAsync(Arg.Any<SendGridMessage>()).Returns(new Response(HttpStatusCode.Accepted, null, null));

		// Act
		var result = await _deleteUserService.InitiateDeleteUserAsync(user.Id);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task DeleteUserAsync_Should_Delete_User_When_Token_Is_Valid()
	{
		// Arrange
		var user = new ApplicationUser { Email = "test@test.com", Id = NewId.NextGuid().ToString() };
		_userManager.VerifyUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose, "token").Returns(true);
		_userManager.DeleteAsync(user).Returns(IdentityResult.Success);

		await using var context = await _contextFactory.CreateDbContextAsync();
		context.UserProfiles.Add(new UserProfile { Id = user.Id });
		await context.SaveChangesAsync();

		// Act
		var result = await _deleteUserService.DeleteUserAsync(user.Id, "token");

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
}

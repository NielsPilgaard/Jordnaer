using System.Net;
using FluentAssertions;
using Jordnaer.Server.Authentication;
using Jordnaer.Server.Database;
using Jordnaer.Server.Features.DeleteUser;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog;
using Xunit;

namespace Jordnaer.Server.Tests.Authentication;

public class DeleteUserService_Should : IClassFixture<SqlServerContainer<JordnaerDbContext>>
{
    private readonly UserManager<ApplicationUser> _userManager = Substitute.For<UserManager<ApplicationUser>>(Substitute.For<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
    private readonly ILogger<UserService> _logger = Substitute.For<ILogger<UserService>>();
    private readonly ISendGridClient _sendGridClient = Substitute.For<ISendGridClient>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly JordnaerDbContext _context;
    private readonly IDiagnosticContext _diagnosticContext = Substitute.For<IDiagnosticContext>();
    private readonly IDeleteUserService _deleteUserService;

    public DeleteUserService_Should(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
    {
        _context = sqlServerContainer.Context;

        _deleteUserService = new DeleteUserService(_userManager, _logger, _sendGridClient, _httpContextAccessor, _context, _diagnosticContext);
    }

    [Fact]
    public async Task InitiateDeleteUserAsync_Should_Send_Email_On_Success()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManager.GenerateUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose).Returns("token");
        _httpContextAccessor.HttpContext!.Request.Scheme.Returns("https");
        _httpContextAccessor.HttpContext!.Request.Host.Returns(new HostString("localhost"));
        _sendGridClient.SendEmailAsync(Arg.Any<SendGridMessage>()).Returns(new Response(HttpStatusCode.Accepted, null, null));

        // Act
        bool result = await _deleteUserService.InitiateDeleteUserAsync(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task InitiateDeleteUserAsync_Should_Not_Send_Email_When_HttpContext_Is_Null()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com" };
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        bool result = await _deleteUserService.InitiateDeleteUserAsync(user);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_Should_Delete_User_When_Token_Is_Valid()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", Id = NewId.NextGuid().ToString() };
        _userManager.VerifyUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose, "token").Returns(true);
        _userManager.DeleteAsync(user).Returns(IdentityResult.Success);

        _context.UserProfiles.Add(new UserProfile { Id = user.Id });
        await _context.SaveChangesAsync();

        // Act
        bool result = await _deleteUserService.DeleteUserAsync(user, "token");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_Should_Not_Delete_User_When_Token_Is_Invalid()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManager.VerifyUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose, "token").Returns(false);

        // Act
        bool result = await _deleteUserService.DeleteUserAsync(user, "token");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_Should_Not_Delete_User_When_DeleteAsync_Fails()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManager.VerifyUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose, "token").Returns(true);
        _userManager.DeleteAsync(user).Returns(IdentityResult.Failed(new IdentityError()));

        // Act
        bool result = await _deleteUserService.DeleteUserAsync(user, "token");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_Should_Not_Delete_User_When_ExecuteDeleteAsync_Fails()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManager.VerifyUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose, "token").Returns(true);
        _userManager.DeleteAsync(user).Returns(IdentityResult.Success);

        // Act
        bool result = await _deleteUserService.DeleteUserAsync(user, "token");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_Should_Not_Delete_User_When_Exception_Thrown()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManager.VerifyUserTokenAsync(user, DeleteUserService.TokenProvider, DeleteUserService.TokenPurpose, "token").Returns(true);
        _userManager.DeleteAsync(user).ThrowsAsync(new Exception());

        // Act
        bool result = await _deleteUserService.DeleteUserAsync(user, "token");

        // Assert
        result.Should().BeFalse();
    }
}

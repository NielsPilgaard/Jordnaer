using Bogus;
using FluentAssertions;
using Jordnaer.Authentication;
using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jordnaer.Server.Tests.Authentication;

[Trait("Category", "IntegrationTest")]
public class UserService_Should : IClassFixture<SqlServerContainer<JordnaerDbContext>>, IAsyncLifetime
{
	private readonly JordnaerDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly IUserService _userService;
	private const string VALID_PASSWORD = "123456789ABCabc";

	public UserService_Should(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
	{
		_context = sqlServerContainer.Context;
		_userManager = Substitute.For<UserManager<ApplicationUser>>(new UserStore<ApplicationUser>(_context), null, null, null, null, null, null, null, null);

		_userService = new UserService(_userManager, Substitute.For<ILogger<UserService>>(), _context);
	}

	[Fact]
	public async Task Create_User_When_User_Info_Is_Valid()
	{
		// Arrange
		var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };
		_userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);

		// Act
		string? result = await _userService.CreateUserAsync(userInfo);

		// Assert
		result.Should().NotBeNull();
	}

	[Fact]
	public async Task CreateUserAsync_Should_Not_Create_User_When_Invalid()
	{
		// Arrange
		var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };
		_userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(IdentityResult.Failed(new IdentityError()));

		// Act
		string? result = await _userService.CreateUserAsync(userInfo);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task IsLoginValidAsync_Should_Return_True_When_Valid()
	{
		// Arrange
		var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };
		_userManager.FindByEmailAsync(Arg.Any<string>()).Returns(new ApplicationUser { Email = userInfo.Email, UserName = userInfo.Email });
		_userManager.CheckPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(true);

		// Act
		string? result = await _userService.IsLoginValidAsync(userInfo);

		// Assert
		result.Should().NotBeNull();
	}

	[Fact]
	public async Task IsLoginValidAsync_Should_Return_False_When_User_Not_Found()
	{
		// Arrange
		var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };
		_userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser)null!);

		// Act
		string? result = await _userService.IsLoginValidAsync(userInfo);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task IsLoginValidAsync_Should_Return_False_When_Password_Not_Matching()
	{
		// Arrange
		var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };
		_userManager.FindByEmailAsync(Arg.Any<string>()).Returns(new ApplicationUser { Email = userInfo.Email, UserName = userInfo.Email });
		_userManager.CheckPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(false);

		// Act
		string? result = await _userService.IsLoginValidAsync(userInfo);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task IsLoginValidAsync_Should_Return_False_When_User_Is_LockedOut()
	{
		// Arrange
		var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };
		_userManager.FindByEmailAsync(Arg.Any<string>()).Returns(new ApplicationUser { Email = userInfo.Email, UserName = userInfo.Email });
		_userManager.SupportsUserLockout.Returns(true);
		_userManager.IsLockedOutAsync(Arg.Any<ApplicationUser>()).Returns(true);

		// Act
		string? result = await _userService.IsLoginValidAsync(userInfo);

		// Assert
		result.Should().BeNull();
	}


	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() => await _context.UserProfiles.ExecuteDeleteAsync();
}

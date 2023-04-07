using Bogus;
using FluentAssertions;
using Jordnaer.Server.Authentication;
using Jordnaer.Server.Data;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Serilog;
using Xunit;

namespace Jordnaer.Server.Tests.Authentication;

public class UserService_Should
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserService _userService;
    private const string VALID_PASSWORD = "123456789ABCabc";

    public UserService_Should()
    {
        _userManager = Substitute.For<UserManager<ApplicationUser>>(Substitute.For<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

        _userService = new UserService(_userManager, Substitute.For<ILogger>());
    }

    [Fact]
    public async Task Create_User_When_User_Info_Is_Valid()
    {
        // Arrange
        var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);

        // Act
        bool result = await _userService.CreateUserAsync(userInfo);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserAsync_Should_Not_Create_User_When_Invalid()
    {
        // Arrange
        var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(IdentityResult.Failed(new IdentityError()));

        // Act
        bool result = await _userService.CreateUserAsync(userInfo);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsLoginValidAsync_Should_Return_True_When_Valid()
    {
        // Arrange
        var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(new ApplicationUser { Email = userInfo.Email, UserName = userInfo.Email });
        _userManager.CheckPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(true);

        // Act
        bool result = await _userService.IsLoginValidAsync(userInfo);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsLoginValidAsync_Should_Return_False_When_User_Not_Found()
    {
        // Arrange
        var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser)null!);

        // Act
        bool result = await _userService.IsLoginValidAsync(userInfo);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsLoginValidAsync_Should_Return_False_When_Password_Not_Matching()
    {
        // Arrange
        var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(new ApplicationUser { Email = userInfo.Email, UserName = userInfo.Email });
        _userManager.CheckPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(false);

        // Act
        bool result = await _userService.IsLoginValidAsync(userInfo);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_Should_Delete_User_When_Valid()
    {
        // Arrange

        var user = new ApplicationUser { Email = "test@example.com", UserName = "test@example.com" };
        _userManager.DeleteAsync(Arg.Any<ApplicationUser>()).Returns(IdentityResult.Success);

        // Act
        bool result = await _userService.DeleteUserAsync(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_Should_Not_Delete_User_When_Invalid()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@example.com", UserName = "test@example.com" };
        _userManager.DeleteAsync(Arg.Any<ApplicationUser>()).Returns(IdentityResult.Failed(new IdentityError()));

        // Act
        bool result = await _userService.DeleteUserAsync(user);

        // Assert
        result.Should().BeFalse();
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
        bool result = await _userService.IsLoginValidAsync(userInfo);

        // Assert
        result.Should().BeFalse();
    }
}

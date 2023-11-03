using System.Security.Claims;
using FluentAssertions;
using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Server.Features.Groups;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Serilog;
using Xunit;
using Claim = System.Security.Claims.Claim;

namespace Jordnaer.Server.Tests.Groups;

[Trait("Category", "IntegrationTest")]
public class GroupServiceTests : IClassFixture<SqlServerContainer<JordnaerDbContext>>
{
    private readonly IGroupService _groupService;
    private readonly JordnaerDbContext _context;
    private readonly string _userProfileId;

    public GroupServiceTests(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
    {
        _userProfileId = Guid.NewGuid().ToString();

        _context = sqlServerContainer.Context;

        var currentUser = new CurrentUser
        {
            Principal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, _userProfileId) }))
        };

        _groupService = new GroupService(_context,
            currentUser,
            Substitute.For<ILogger<GroupService>>(),
            Substitute.For<IDiagnosticContext>());
    }

    [Fact]
    public async Task GetGroupByIdAsync_ReturnsNotFound_WhenGroupDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _groupService.GetGroupByIdAsync(id);

        // Assert
        result.Result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetGroupByIdAsync_ReturnsGroupDto_WhenGroupExists()
    {
        // Arrange
        AddUserProfile();
        var group = AddGroup();
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.GetGroupByIdAsync(group.Id);

        // Assert
        result.Result.Should().BeOfType<Ok<GroupDto>>();
        var groupDto = (result.Result as Ok<GroupDto>)?.Value;
        groupDto.Should().NotBeNull();
        groupDto!.Name.Should().Be(group.Name);
        groupDto.Description.Should().Be(group.Description);
        groupDto.ShortDescription.Should().Be(group.ShortDescription);
        groupDto.City.Should().Be(group.City);
        groupDto.ZipCode.Should().Be(group.ZipCode);
        groupDto.MemberCount.Should().Be(group.Memberships.Count);
    }

    [Fact]
    public async Task CreateGroupAsync_ReturnsCreatedAtRoute_WhenGroupIsCreated()
    {
        // Arrange
        AddUserProfile();
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Description = "Test Group Description",
            ShortDescription = "Test Group Short Description",
            City = "Test City",
            ZipCode = 1234
        };

        // Act
        var result = await _groupService.CreateGroupAsync(group);

        // Assert
        result.Should().BeOfType<CreatedAtRoute>();
    }

    [Fact]
    public async Task UpdateGroupAsync_ReturnsBadRequest_WhenIdDoesNotMatch()
    {
        // Arrange
        var id = Guid.NewGuid();
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Description = "Test Group Description",
            ShortDescription = "Test Group Short Description",
            City = "Test City",
            ZipCode = 1234
        };

        // Act
        var result = await _groupService.UpdateGroupAsync(id, group);

        // Assert
        result.Result.Should().BeOfType<BadRequest>();
    }

    [Fact]
    public async Task UpdateGroupAsync_ReturnsNotFound_WhenGroupDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var group = new Group
        {
            Id = id,
            Name = "Test Group",
            Description = "Test Group Description",
            ShortDescription = "Test Group Short Description",
            City = "Test City",
            ZipCode = 1234
        };

        // Act
        var result = await _groupService.UpdateGroupAsync(id, group);

        // Assert
        result.Result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task UpdateGroupAsync_ReturnsNoContent_WhenGroupIsUpdated()
    {
        // Arrange
        AddUserProfile();
        var group = AddGroup();
        await _context.SaveChangesAsync();
        _context.Entry(group).State = EntityState.Detached;

        var updatedGroup = new Group
        {
            Id = group.Id,
            Name = "Updated Test Group",
            Description = "Updated Test Group Description",
            ShortDescription = "Updated Test Group Short Description",
            City = "Updated Test City",
            ZipCode = 4321
        };

        // Act
        var result = await _groupService.UpdateGroupAsync(group.Id, updatedGroup);

        // Assert
        result.Result.Should().BeOfType<NoContent>();
    }

    [Fact]
    public async Task DeleteGroupAsync_ReturnsNotFound_WhenGroupDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _groupService.DeleteGroupAsync(id);

        // Assert
        result.Result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task DeleteGroupAsync_ReturnsUnauthorized_WhenGroupHasNoOwner()
    {
        // Arrange
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Description = "Test Group Description",
            ShortDescription = "Test Group Short Description",
            City = "Test City",
            ZipCode = 1234
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.DeleteGroupAsync(group.Id);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task DeleteGroupAsync_ReturnsUnauthorized_WhenRequestIsNotFromOwner()
    {
        // Arrange
        string userId = Guid.NewGuid().ToString();
        AddUserProfile(userId);
        var group = AddGroup(null, userId);
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.DeleteGroupAsync(group.Id);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task DeleteGroupAsync_ReturnsNoContent_WhenGroupIsDeleted()
    {
        // Arrange
        AddUserProfile();
        var group = AddGroup();
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.DeleteGroupAsync(group.Id);

        // Assert
        result.Result.Should().BeOfType<NoContent>();
    }

    public Group AddGroup(Guid? groupId = null, string? userId = null)
    {
        groupId ??= Guid.NewGuid();
        userId ??= _userProfileId;

        var group = new Group
        {
            Id = groupId.Value,
            Name = "Test Group",
            Description = "Test Group Description",
            ShortDescription = "Test Group Short Description",
            City = "Test City",
            ZipCode = 1234,
            Memberships = new List<GroupMembership>
            {
                new()
                {
                    GroupId = groupId.Value,
                    UserProfileId = userId,
                    OwnershipLevel = OwnershipLevel.Owner,
                    MembershipStatus = MembershipStatus.Active,
                    PermissionLevel = PermissionLevel.Read |
                                      PermissionLevel.Write |
                                      PermissionLevel.Moderator |
                                      PermissionLevel.Admin
                }
            }
        };
        _context.Groups.Add(group);

        return group;
    }

    private void AddUserProfile(string? userId = null)
        => _context.UserProfiles.Add(new UserProfile { Id = userId ?? _userProfileId });
}

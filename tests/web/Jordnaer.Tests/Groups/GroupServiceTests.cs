using Bogus;
using FluentAssertions;
using Jordnaer.Authorization;
using Jordnaer.Database;
using Jordnaer.Features.Groups;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Serilog;
using System.Security.Claims;
using Xunit;
using Claim = System.Security.Claims.Claim;

namespace Jordnaer.Tests.Groups;

[Trait("Category", "IntegrationTest")]
public class GroupServiceTests : IClassFixture<SqlServerContainer<JordnaerDbContext>>, IAsyncLifetime
{
	private readonly IGroupService _groupService;
	private readonly JordnaerDbContext _context;
	private readonly string _userProfileId;

	private readonly Faker<Group> _groupFaker = new Faker<Group>("nb_NO")
		.RuleFor(g => g.Id, _ => Guid.NewGuid())
		.RuleFor(g => g.Name, f => f.Company.CompanyName())
		.RuleFor(g => g.ProfilePictureUrl, f => f.Image.PicsumUrl())
		.RuleFor(g => g.ShortDescription, f => f.Lorem.Sentence())
		.RuleFor(u => u.ZipCode, f => f.Random.Int(1000, 9991))
		.RuleFor(u => u.City, f => f.Address.City())
		.RuleFor(g => g.Description, f => f.Lorem.Paragraph())
		.RuleFor(g => g.CreatedUtc, f => f.Date.Past(3));

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
		result.Should().BeOfType<NotFound>();
	}

	[Fact]
	public async Task GetGroupByIdAsync_ReturnsGroupDto_WhenGroupExists()
	{
		// Arrange
		AddUserProfile();
		var group = AddGroup();
		await _context.SaveChangesAsync();

		// Act
		var groupDto = await _groupService.GetGroupByIdAsync(group.Id);

		// Assert
		groupDto.Should().BeOfType<Group>();
		group = groupDto.AsT0;
		group.Should().NotBeNull();
		group.Name.Should().Be(group.Name);
		group.Description.Should().Be(group.Description);
		group.ShortDescription.Should().Be(group.ShortDescription);
		group.City.Should().Be(group.City);
		group.ZipCode.Should().Be(group.ZipCode);
	}

	[Fact]
	public async Task CreateGroupAsync_ReturnsNoContent_WhenGroupIsCreated()
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
		result.Should().BeOfType<NoContent>();
	}

	[Fact]
	public async Task CreateGroupAsync_ReturnsBadRequest_WhenGroupUsesNonUniqueName()
	{
		// Arrange
		AddUserProfile();
		var existingGroup = AddGroup();
		await _context.SaveChangesAsync();

		var group = new Group
		{
			Id = Guid.NewGuid(),
			Name = existingGroup.Name,
			Description = "Test Group Description",
			ShortDescription = "Test Group Short Description",
			City = "Test City",
			ZipCode = 1234
		};

		// Act
		var result = await _groupService.CreateGroupAsync(group);

		// Assert
		result.Should().BeOfType<BadRequest<string>>();
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
		result.Should().BeOfType<BadRequest<string>>();
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
		result.Should().BeOfType<NotFound>();
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
		result.Should().BeOfType<NoContent>();
	}

	[Fact]
	public async Task DeleteGroupAsync_ReturnsNotFound_WhenGroupDoesNotExist()
	{
		// Arrange
		var id = Guid.NewGuid();

		// Act
		var result = await _groupService.DeleteGroupAsync(id);

		// Assert
		result.Should().BeOfType<NotFound>();
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
		result.Should().BeOfType<UnauthorizedHttpResult>();
	}

	[Fact]
	public async Task DeleteGroupAsync_ReturnsUnauthorized_WhenRequestIsNotFromOwner()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();
		AddUserProfile(userId);
		var group = AddGroup(userId);
		await _context.SaveChangesAsync();

		// Act
		var result = await _groupService.DeleteGroupAsync(group.Id);

		// Assert
		result.Should().BeOfType<UnauthorizedHttpResult>();
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
		result.Should().BeOfType<NoContent>();
	}

	public Group AddGroup(string? userId = null)
	{
		var groupId = Guid.NewGuid();
		userId ??= _userProfileId;

		var group = _groupFaker.Generate();
		group.Memberships = new List<GroupMembership>
		{
			new()
			{
				GroupId = groupId,
				UserProfileId = userId,
				OwnershipLevel = OwnershipLevel.Owner,
				MembershipStatus = MembershipStatus.Active,
				PermissionLevel = PermissionLevel.Read |
								  PermissionLevel.Write |
								  PermissionLevel.Moderator |
								  PermissionLevel.Admin
			}
		};
		_context.Groups.Add(group);

		return group;
	}

	private void AddUserProfile(string? userId = null)
		=> _context.UserProfiles.Add(new UserProfile { Id = userId ?? _userProfileId });

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() => await _context.Groups.ExecuteDeleteAsync();
}

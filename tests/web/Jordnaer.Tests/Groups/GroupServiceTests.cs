using Bogus;
using FluentAssertions;
using Jordnaer.Database;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Groups;
using Jordnaer.Shared;
using Jordnaer.Tests.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf.Types;
using Serilog;
using Xunit;
using NotFound = OneOf.Types.NotFound;

namespace Jordnaer.Tests.Groups;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(SqlServerContainerCollection))]
public class GroupServiceTests : IAsyncLifetime
{
	private readonly GroupService _groupService;
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory = Substitute.For<IDbContextFactory<JordnaerDbContext>>();
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

	private readonly SqlServerContainer<JordnaerDbContext> _sqlServerContainer;

	public GroupServiceTests(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
	{
		_sqlServerContainer = sqlServerContainer;

		_context = _sqlServerContainer.CreateContext();

		_userProfileId = Guid.NewGuid().ToString();

		_contextFactory.CreateDbContextAsync().ReturnsForAnyArgs(sqlServerContainer.CreateContext());

		_groupService = new GroupService(_contextFactory,
			Substitute.For<ILogger<GroupService>>(),
			Substitute.For<IDiagnosticContext>(),
			new CurrentUser());
	}

	[Fact]
	public async Task GetGroupByIdAsync_ReturnsNotFound_WhenGroupDoesNotExist()
	{
		// Arrange
		var id = Guid.NewGuid();

		// Act
		var result = await _groupService.GetGroupByIdAsync(id);

		// Assert
		result.Value.Should().BeOfType<NotFound>();
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
		groupDto.Value.Should().BeOfType<Group>();
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
		await _context.SaveChangesAsync();
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
		var result = await _groupService.CreateGroupAsync(_userProfileId, group);

		// Assert
		result.Value.Should().BeOfType<Success>();
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
		var result = await _groupService.CreateGroupAsync(_userProfileId, group);

		// Assert
		result.Value.Should().BeOfType<Error<string>>();
	}

	[Fact]
	public async Task UpdateGroupAsync_ReturnsNotFound_WhenGroupDoesNotExist()
	{
		// Arrange
		var group = new Group
		{
			Id = NewId.NextGuid(),
			Name = "Test Group",
			Description = "Test Group Description",
			ShortDescription = "Test Group Short Description",
			City = "Test City",
			ZipCode = 1234
		};

		// Act
		var result = await _groupService.UpdateGroupAsync(_userProfileId, group);

		// Assert
		result.Value.Should().BeOfType<NotFound>();
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
		var result = await _groupService.UpdateGroupAsync(_userProfileId, updatedGroup);

		// Assert
		result.Value.Should().BeOfType<Success>();
	}

	[Fact]
	public async Task DeleteGroupAsync_ReturnsNotFound_WhenGroupDoesNotExist()
	{
		// Arrange
		var id = Guid.NewGuid();

		// Act
		var result = await _groupService.DeleteGroupAsync(_userProfileId, id);

		// Assert
		result.Value.Should().BeOfType<NotFound>();
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
		var result = await _groupService.DeleteGroupAsync(_userProfileId, group.Id);

		// Assert
		result.Value.Should().BeOfType<Error>();
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
		var result = await _groupService.DeleteGroupAsync(NewId.NextGuid().ToString(), group.Id);

		// Assert
		result.Value.Should().BeOfType<Error>();
	}

	[Fact]
	public async Task DeleteGroupAsync_ReturnsNoContent_WhenGroupIsDeleted()
	{
		// Arrange
		AddUserProfile();
		var group = AddGroup();
		await _context.SaveChangesAsync();

		// Act
		var result = await _groupService.DeleteGroupAsync(_userProfileId, group.Id);

		// Assert
		result.Value.Should().BeOfType<Success>();
	}

	public Group AddGroup(string? userId = null)
	{
		var groupId = Guid.NewGuid();
		userId ??= _userProfileId;

		var group = _groupFaker.Generate();
		group.Memberships =
		[
			new GroupMembership
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
		];
		_context.Groups.Add(group);

		return group;
	}

	private void AddUserProfile(string? userId = null) =>
		_context.UserProfiles.Add(new UserProfile { Id = userId ?? _userProfileId });

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		await using var context = _sqlServerContainer.CreateContext();
		await context.Groups.ExecuteDeleteAsync();
		await _context.DisposeAsync();
	}
}

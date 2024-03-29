using Bogus;
using FluentAssertions;
using Jordnaer.Database;
using Jordnaer.Features.UserSearch;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Refit;
using System.Net;
using Xunit;

namespace Jordnaer.Tests.UserSearch;

[Trait("Category", "UnitTest")]
public class UserSearchService_Should : IClassFixture<SqlServerContainer<JordnaerDbContext>>, IAsyncLifetime
{
	private readonly JordnaerDbContext _context;
	private readonly IDataForsyningenClient _dataForsyningenClientMock = Substitute.For<IDataForsyningenClient>();
	private readonly IUserSearchService _sut;
	private readonly Faker _faker = new();

	public UserSearchService_Should(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
	{
		_context = sqlServerContainer.Context;

		_sut = new UserSearchService(
			Substitute.For<ILogger<UserSearchService>>(),
			_context,
			_dataForsyningenClientMock,
			Options.Create(new DataForsyningenOptions { BaseUrl = string.Empty }));
	}

	[Fact]
	public async Task Return_UserSearchResult_Given_Valid_Filter()
	{
		// Arrange
		var filter = new UserSearchFilter();

		// Act
		var result = await _sut.GetUsersAsync(filter);

		// Assert
		result.Should().BeOfType<UserSearchResult>();
		result.TotalCount.Should().Be(0);
		result.Users.Should().BeEquivalentTo(new List<UserDto>());
	}

	[Fact]
	public async Task Return_UserSearchResult_With_Category_Filter()
	{
		// Arrange
		var filter = new UserSearchFilter { Categories = new[] { _faker.Lorem.Word() } };
		var users = CreateTestUsers(5);
		// Ensure at least one user is looking for the specified activity
		users[0].Categories.Add(new Shared.Category { Name = filter.Categories.First() });
		_context.UserProfiles.AddRange(users);
		await _context.SaveChangesAsync();

		// Act
		var result = await _sut.GetUsersAsync(filter);

		// Assert
		result.TotalCount.Should().Be(1);
		result.Users.Should().ContainSingle(u => u.Categories.Contains(filter.Categories.First()));
	}

	[Fact]
	public async Task Return_UserSearchResult_With_FirstName_Filter()
	{
		// Arrange
		string? firstName = _faker.Name.FirstName();
		var filter = new UserSearchFilter { Name = firstName };
		var users = CreateTestUsers(5);
		// Ensure at least one user has the specified name in their SearchableName
		users[0].FirstName = firstName;
		_context.UserProfiles.AddRange(users);
		await _context.SaveChangesAsync();

		// Act
		var result = await _sut.GetUsersAsync(filter);

		// Assert
		result.TotalCount.Should().Be(1);
		result.Users.Should().ContainSingle(user => user.FirstName == firstName);
	}

	[Fact]
	public async Task Return_UserSearchResult_With_LastName_Filter()
	{
		// Arrange
		string? lastName = _faker.Name.LastName();
		var filter = new UserSearchFilter { Name = lastName };
		var users = CreateTestUsers(5);
		// Ensure at least one user has the specified name in their SearchableName
		users[0].LastName = lastName;
		_context.UserProfiles.AddRange(users);
		await _context.SaveChangesAsync();

		// Act
		var result = await _sut.GetUsersAsync(filter);

		// Assert
		result.TotalCount.Should().Be(1);
		result.Users.Should().ContainSingle(user => user.LastName == lastName);
	}

	[Fact]
	public async Task Return_UserSearchResult_With_ProfileName_Filter()
	{
		// Arrange
		string? userName = _faker.Internet.UserName();
		var filter = new UserSearchFilter { Name = userName };
		var users = CreateTestUsers(5);
		// Ensure at least one user has the specified name in their SearchableName
		users[0].UserName = userName;
		_context.UserProfiles.AddRange(users);
		await _context.SaveChangesAsync();

		// Act
		var result = await _sut.GetUsersAsync(filter);

		// Assert
		result.TotalCount.Should().Be(1);
		result.Users.Should().ContainSingle(user => user.UserName == userName);
	}

	[Fact]
	public async Task Return_UserSearchResult_With_CombinedName_Filter()
	{
		// Arrange
		string? firstName = _faker.Name.FirstName();
		string? lastName = _faker.Name.LastName();

		var filter = new UserSearchFilter { Name = $"{firstName} {lastName}" };
		var users = CreateTestUsers(5);
		// Ensure at least one user has the specified name in their SearchableName
		users[0].FirstName = firstName;
		users[0].LastName = lastName;
		_context.UserProfiles.AddRange(users);
		await _context.SaveChangesAsync();

		// Act
		var result = await _sut.GetUsersAsync(filter);

		// Assert
		result.TotalCount.Should().Be(1);
		result.Users.Should().ContainSingle(user => user.FirstName == firstName &&
													user.LastName == lastName);
	}

	[Fact]
	public async Task Return_UserSearchResult_With_ChildGender_Filter()
	{
		// Arrange
		var filter = new UserSearchFilter { ChildGender = _faker.PickRandom<Gender>() };
		var users = CreateTestUsers(5);

		// Ensure at least one user has a child with the specified gender
		users[0].ChildProfiles.Add(new ChildProfile
		{
			Gender = filter.ChildGender.Value,
			FirstName = _faker.Name.FirstName()
		});
		_context.UserProfiles.AddRange(users);
		await _context.SaveChangesAsync();

		// Act
		var result = await _sut.GetUsersAsync(filter);

		// Assert
		result.TotalCount.Should().Be(1);
		result.Users.Should().ContainSingle(u => u.Children.Any(c => c.Gender == filter.ChildGender));
	}

	[Fact]
	public async Task Return_UserSearchResult_With_MinimumChildAge_Filter()
	{
		// Arrange
		var filter = new UserSearchFilter { MinimumChildAge = _faker.Random.Number(1, 10) };
		var users = CreateTestUsers(5);
		// Ensure at least one user has a child with the specified minimum age
		users[0].ChildProfiles.Add(new ChildProfile
		{
			DateOfBirth = DateTime.UtcNow.AddYears(-filter.MinimumChildAge.Value),
			FirstName = _faker.Name.FirstName()
		});
		_context.UserProfiles.AddRange(users);
		await _context.SaveChangesAsync();

		// Act
		var result = await _sut.GetUsersAsync(filter);

		// Assert
		result.TotalCount.Should().Be(1);
		result.Users.Should()
			.ContainSingle(user => user.Children.Any(child =>
				DateTime.UtcNow.Year - child.DateOfBirth.GetValueOrDefault().Year >= filter.MinimumChildAge));
	}

	[Fact]
	public async Task Return_UserSearchResult_With_MaximumChildAge_Filter()
	{
		// Arrange
		var filter = new UserSearchFilter { MaximumChildAge = _faker.Random.Number(1, 10) };
		var users = CreateTestUsers(5);
		// Ensure at least one user has a child with the specified maximum age
		users[0].ChildProfiles.Add(new ChildProfile
		{
			// 3 minutes is added to ensure the test does not suffer from timing issues
			DateOfBirth = DateTime.UtcNow.AddYears(-filter.MaximumChildAge.Value).AddMinutes(3),
			FirstName = _faker.Name.FirstName()
		});
		_context.UserProfiles.AddRange(users);
		await _context.SaveChangesAsync();

		// Act
		var result = await _sut.GetUsersAsync(filter);

		// Assert
		result.TotalCount.Should().Be(1);
		result.Users
			.Should()
			.ContainSingle(user => user.Children.Any(child =>
				DateTime.UtcNow.Year - child.DateOfBirth.GetValueOrDefault().Year <= filter.MaximumChildAge));
	}

	[Fact]
	public async Task Return_UserSearchResult_With_Location_Filter()
	{
		// Arrange
		const int zipCode = 8000;
		const string location = "Park Allé 1, 8000 Aarhus";
		var filter = new UserSearchFilter { Location = location, WithinRadiusKilometers = 3 };
		var users = CreateTestUsers(5);

		users[0].ZipCode = zipCode;
		_context.UserProfiles.AddRange(users);
		await _context.SaveChangesAsync();

		_dataForsyningenClientMock.GetAddressesWithAutoComplete(location)
			.Returns(new ApiResponse<IEnumerable<AddressAutoCompleteResponse>>(
				new HttpResponseMessage(HttpStatusCode.OK),
				new[] { new AddressAutoCompleteResponse(location, new Adresse { Postnr = zipCode.ToString() }) },
				new RefitSettings()));

		_dataForsyningenClientMock.GetZipCodesWithinCircle(Arg.Any<string>())
			.Returns(new ApiResponse<IEnumerable<ZipCodeSearchResponse>>(
				new HttpResponseMessage(HttpStatusCode.OK),
				new[] { new ZipCodeSearchResponse { Nr = zipCode.ToString() } },
				new RefitSettings()));

		// Act
		var result = await _sut.GetUsersAsync(filter);

		// Assert
		result.TotalCount.Should().Be(1);
		result.Users
			.Should()
			.ContainSingle(user => user.ZipCode != null &&
								   filter.Location.Contains(user.ZipCode.ToString()!));
	}

	private static List<UserProfile> CreateTestUsers(int count)
	{
		var users = new Faker<UserProfile>()
			.RuleFor(u => u.FirstName, f => f.Name.FirstName())
			.RuleFor(u => u.LastName, f => f.Name.LastName())
			.RuleFor(u => u.UserName, f => f.Internet.UserName())
			.RuleFor(u => u.SearchableName, (_, user) => $"{user.FirstName}{user.LastName}{user.UserName}")
			.RuleFor(u => u.Id, _ => NewId.NextGuid().ToString())
			.Generate(count);

		return users;
	}

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() => await _context.UserProfiles.ExecuteDeleteAsync();
}

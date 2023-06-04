using System.Net;
using Bogus;
using FluentAssertions;
using Jordnaer.Server.Database;
using Jordnaer.Server.Features.UserSearch;
using Jordnaer.Shared;
using Jordnaer.Shared.UserSearch;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Refit;
using Xunit;

namespace Jordnaer.Server.Tests.UserSearch;

[Trait("Category", "UnitTest")]
public class UserSearchService_Should : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private JordnaerDbContext _context = null!;
    private IDataForsyningenClient _dataForsyningenClientMock = null!;
    private UserSearchService _sut = null!;
    private readonly Faker _faker = new();

    [Fact]
    public async Task Return_UserSearchResult_Given_Valid_Filter()
    {
        // Arrange
        var filter = new UserSearchFilter();

        // Act
        var result = await _sut.GetUsersAsync(filter, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UserSearchResult>();
        result.TotalCount.Should().Be(0);
        result.Users.Should().BeEquivalentTo(new List<UserDto>());
    }

    [Fact]
    public async Task Return_UserSearchResult_With_LookingFor_Filter()
    {
        // Arrange
        var filter = new UserSearchFilter { LookingFor = new[] { _faker.Lorem.Word() } };
        var users = CreateTestUsers(5);
        // Ensure at least one user is looking for the specified activity
        users[0].LookingFor.Add(new Shared.LookingFor { Name = filter.LookingFor.First() });
        _context.UserProfiles.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUsersAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Users.Should().ContainSingle(u => u.LookingFor.Contains(filter.LookingFor.First()));
    }

#pragma warning disable xUnit1004 // Test methods should not be skipped
    [Fact(Skip = "This test fails. SQLite does not support computed columns, which the name filter relies on.")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
    public async Task Return_UserSearchResult_With_Name_Filter()
    {
        // Arrange
        var filter = new UserSearchFilter { Name = _faker.Name.FirstName() };
        var users = CreateTestUsers(5);
        // Ensure at least one user has the specified name in their SearchableName
        users[0].FirstName = filter.Name;
        _context.UserProfiles.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUsersAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Users.Should().ContainSingle(user => user.FirstName != null &&
                                                    user.FirstName.Contains(filter.Name));
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
        var result = await _sut.GetUsersAsync(filter, CancellationToken.None);

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
        var result = await _sut.GetUsersAsync(filter, CancellationToken.None);

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
        var result = await _sut.GetUsersAsync(filter, CancellationToken.None);

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
        const string zipCode = "8000";
        const string location = "Park Allé 1, 8000 Aarhus";
        var filter = new UserSearchFilter { Location = location, WithinRadiusKilometers = 3 };
        var users = CreateTestUsers(5);

        users[0].ZipCode = zipCode;
        _context.UserProfiles.AddRange(users);
        await _context.SaveChangesAsync();

        _dataForsyningenClientMock.GetAddressesWithAutoComplete(location)
            .Returns(new ApiResponse<IEnumerable<AddressAutoCompleteResponse>>(
                new HttpResponseMessage(HttpStatusCode.OK),
                new[] { new AddressAutoCompleteResponse(location, new Adresse { Postnr = zipCode }) },
                new RefitSettings()));

        _dataForsyningenClientMock.GetZipCodesWithinCircle(Arg.Any<string>())
            .Returns(new ApiResponse<IEnumerable<ZipCodeSearchResponse>>(
                new HttpResponseMessage(HttpStatusCode.OK),
                new[] { new ZipCodeSearchResponse { Nr = zipCode } },
                new RefitSettings()));

        // Act
        var result = await _sut.GetUsersAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Users
            .Should()
            .ContainSingle(user => user.ZipCode != null &&
                                   filter.Location.Contains(user.ZipCode));
    }

    public async Task InitializeAsync()
    {
        _dataForsyningenClientMock = Substitute.For<IDataForsyningenClient>();

        // Open the connection manually because EF won't do it automatically
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<JordnaerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new JordnaerDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _sut = new UserSearchService(
            Substitute.For<ILogger<UserSearchService>>(),
            _context,
            _dataForsyningenClientMock,
            Options.Create(new DataForsyningenOptions { BaseUrl = string.Empty }));
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _connection.CloseAsync();
    }

    private static List<UserProfile> CreateTestUsers(int count)
    {
        var users = new Faker<UserProfile>()
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.UserName, f => f.Internet.UserName())
            .RuleFor(u => u.SearchableName, (_, user) => $"{user.FirstName}{user.LastName}{user.UserName}")
            .RuleFor(u => u.Id, _ => Guid.NewGuid().ToString())
            .Generate(count);

        return users;
    }
}

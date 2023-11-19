using BenchmarkDotNet.Attributes;
using Jordnaer.Client.Features.UserSearch;
using Jordnaer.Server.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Jordnaer.Server.Benchmarks;

//TODO: This fails because the search endpoint requires auth and has rate limiting
[MemoryDiagnoser]
public class UserSearchBenchmark
{
    private IUserSearchClient _client = null!;
    private JordnaerDbContext _context = null!;
    private UserProfile _randomUser = null!;
    private List<Category> _categories = new();

    [GlobalSetup]
    public async Task GlobalSetupAsync()
    {
        var factory = new BenchmarkWebApplicationFactory();
        factory.ClientOptions.AllowAutoRedirect = false;

        using var scope = factory.Services.CreateScope();

        _context = scope.ServiceProvider.GetRequiredService<JordnaerDbContext>();

        await _context.Database.MigrateAsync();

        _categories = await _context.InsertCategoriesAsync();

        await _context.InsertFakeUsersAsync(_categories);

        await _context.SaveChangesAsync();

        var httpClient = factory.CreateClient();

        _randomUser = (await _context.UserProfiles
            .Order()
            .Skip(Random.Shared.Next(0, 10000))
            .Take(1)
            .FirstOrDefaultAsync())!;

        _client = RestService.For<IUserSearchClient>(httpClient);
    }

    [Benchmark]
    public async Task UserSearch_No_Filter() =>
        await _client.GetUsers(new UserSearchFilter());

    [Benchmark]
    public async Task UserSearch_Filter_By_FirstName() =>
        await _client.GetUsers(new UserSearchFilter { Name = _randomUser.FirstName });

    [Benchmark]
    public async Task UserSearch_Filter_By_LastName() =>
        await _client.GetUsers(new UserSearchFilter { Name = _randomUser.LastName });

    [Benchmark]
    public async Task UserSearch_Filter_By_UserName() =>
        await _client.GetUsers(new UserSearchFilter { Name = _randomUser.UserName });

    [Benchmark]
    public async Task UserSearch_Filter_By_Age_Of_Children()
    {
        int minimumAge = Random.Shared.Next(0, 14);
        int maximumAge = Random.Shared.Next(minimumAge, 14);
        await _client.GetUsers(new UserSearchFilter { MinimumChildAge = minimumAge, MaximumChildAge = maximumAge });
    }

    [Benchmark]
    public async Task UserSearch_Filter_By_Category() =>
        await _client.GetUsers(new UserSearchFilter
        {
            Categories = _categories
                .Select(category => category.Name)
                .Skip(Random.Shared.Next(0, _categories.Count))
                .ToArray()
        });

    [Benchmark]
    public async Task UserSearch_Filter_By_Gender_Of_Children()
    {
        var genders = Enum.GetValues<Gender>();
        await _client.GetUsers(new UserSearchFilter { ChildGender = genders[Random.Shared.Next(0, genders.Length)] });
    }

    [Benchmark]
    public async Task UserSearch_Filter_By_Address_Within_20_Kilometers() =>
        await _client.GetUsers(new UserSearchFilter
        {
            Location = $"{_randomUser.Address}, {_randomUser.ZipCode} {_randomUser.City}",
            WithinRadiusKilometers = 20
        });
}

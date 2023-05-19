using BenchmarkDotNet.Attributes;
using Jordnaer.Client;
using Jordnaer.Client.Features.Search;
using Jordnaer.Server.Database;
using Jordnaer.Shared;
using Jordnaer.Shared.UserSearch;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jordnaer.Server.Benchmarks;

[MemoryDiagnoser]
public class UserSearchBenchmark
{
    private IUserSearchApi _client = null!;
    private JordnaerDbContext _context = null!;
    private UserProfile _randomUser = null!;
    private List<LookingFor> _lookingFor = new();

    [GlobalSetup]
    public async Task GlobalSetupAsync()
    {
        var factory = new WebApplicationFactory<Program>();

        factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddRefitClient<IUserSearchApi>(factory.Server.BaseAddress)));

        using var scope = factory.Services.CreateScope();

        _context = scope.ServiceProvider.GetRequiredService<JordnaerDbContext>();

        await _context.Database.MigrateAsync();

        _lookingFor = await _context.InsertLookingForDataAsync();

        await _context.InsertFakeUsersAsync(_lookingFor);

        await _context.SaveChangesAsync();

        _client = scope.ServiceProvider.GetRequiredService<IUserSearchApi>();

        _randomUser = (await _context.UserProfiles.Skip(Random.Shared.Next(0, 10000)).Take(1).FirstOrDefaultAsync())!;
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
    public async Task UserSearch_Filter_By_LookingFor() =>
        await _client.GetUsers(new UserSearchFilter
        {
            LookingFor = _lookingFor
                .Select(lookingFor => lookingFor.Name)
                .Skip(Random.Shared.Next(0, _lookingFor.Count))
                .ToList()
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
            Address = _randomUser.ZipCode,
            WithinRadiusMeters = 20000
        });

    [Benchmark]
    public async Task UserSearch_Filter_By_ZipCode_Within_20_Kilometers() =>
        await _client.GetUsers(new UserSearchFilter
        {
            ZipCode = _randomUser.ZipCode,
            WithinRadiusMeters = 20000
        });
}

using BenchmarkDotNet.Attributes;
using Jordnaer.Server.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jordnaer.Server.Benchmarks;

[MemoryDiagnoser]
public class RenderProfileBenchmark
{
    private string _userName = null!;
    private HttpClient _client = null!;

    [GlobalSetup]
    public async Task GlobalSetupAsync()
    {
        var factory = new WebApplicationFactory<Program>();

        using var scope = factory.Services.CreateScope();

        await using var context = scope.ServiceProvider.GetRequiredService<JordnaerDbContext>();

        await context.Database.MigrateAsync();

        var lookingFor = await context.InsertLookingForDataAsync();

        await context.InsertFakeUsersAsync(lookingFor, 100);

        await context.SaveChangesAsync();

        var user = await context.UserProfiles.FirstOrDefaultAsync();
        _userName = user!.UserName!;

        _client = factory.CreateClient();
    }

    [Benchmark]
    public async Task RenderProfileAsync() => await _client.GetAsync($"/{_userName}");
}

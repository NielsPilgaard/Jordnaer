using Bogus;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Database;

public static class SeedDatabase
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<JordnaerDbContext>();

        await context.Database.MigrateAsync();

        var lookingFor = await context.InsertLookingForDataAsync();

        await context.InsertFakeUsersAsync(lookingFor);

        await context.SaveChangesAsync();
    }

    public static async Task InsertFakeUsersAsync(
        this JordnaerDbContext context,
        List<LookingFor> lookingFor,
        int usersToGenerate = 10000)
    {
        int userProfileCount = await context.UserProfiles.CountAsync();
        if (userProfileCount > 1)
        {
            return;
        }

        // Danish locale is not available, nb_NO is norwegian. Close enough!
        var childProfileFaker = new Faker<ChildProfile>("nb_NO")
            .RuleFor(cp => cp.Id, _ => Guid.NewGuid())
            .RuleFor(cp => cp.FirstName, f => f.Name.FirstName())
            .RuleFor(cp => cp.LastName, f => f.Name.LastName())
            .RuleFor(cp => cp.Gender, f => f.PickRandom<Gender>())
            .RuleFor(cp => cp.DateOfBirth, f => f.Date.Between(DateTime.UtcNow.AddYears(-14), DateTime.UtcNow))
            .RuleFor(cp => cp.Description, f => f.Lorem.Paragraphs(f.Random.Int(1, 5)))
            .RuleFor(cp => cp.PictureUrl, f => f.Internet.Avatar());

        var userFaker = new Faker<UserProfile>("nb_NO")
            .RuleFor(u => u.Id, _ => Guid.NewGuid().ToString())
            .RuleFor(u => u.UserName, f => f.Internet.UserName())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(u => u.Address, f => f.Address.StreetAddress())
            .RuleFor(u => u.ZipCode, f => f.Random.Int(1000, 9991))
            .RuleFor(u => u.City, f => f.Address.City())
            .RuleFor(u => u.Description, f => f.Lorem.Paragraphs(f.Random.Int(1, 5)))
            .RuleFor(u => u.LookingFor,
                f => lookingFor
                    .OrderBy(_ => f.Random.Int(0, lookingFor.Count))
                    .Take(f.Random.Int(0, lookingFor.Count))
                    .ToList())
            .RuleFor(u => u.ChildProfiles, f => childProfileFaker.Generate(f.Random.Int(1, 3)))
            .RuleFor(u => u.DateOfBirth, f => f.Date.Between(DateTime.UtcNow.AddYears(-70), DateTime.UtcNow.AddYears(-16)))
            .RuleFor(u => u.ProfilePictureUrl, f => f.Internet.Avatar());

        Console.WriteLine("Generated {0} UserProfiles for testing.", usersToGenerate);

        var users = userFaker.Generate(usersToGenerate);

        context.AddRange(users);
    }

    public static async Task<List<LookingFor>> InsertLookingForDataAsync(this JordnaerDbContext context)
    {
        if (await context.LookingFor.AnyAsync())
        {
            return new List<LookingFor>();
        }

        var lookingFor = new List<LookingFor>()
        {
            new() {Name = "Legeaftaler"},
            new() {Name = "Legegrupper"},
            new() {Name = "Legestuer"},
            new() {Name = "Hjemmepasnings-grupper"},
            new() {Name = "Hjemmeundervisnings-grupper"},
            new() {Name = "Privat Dagpleje"},
            new() {Name = "Mødregrupper"},
            new() {Name = "Fædregrupper"},
            new() {Name = "Forældregrupper"},
            new() {Name = "Sportsaktiviteter"},
            new() {Name = "Andet"}
        };

        context.LookingFor.AddRange(lookingFor);

        return lookingFor;
    }
}

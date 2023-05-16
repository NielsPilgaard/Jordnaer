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

        await context.InsertLookingForDataAsync();

        await context.InsertFakeUsersAsync();
    }

    public static async Task InsertFakeUsersAsync(this JordnaerDbContext context)
    {
        int userProfileCount = await context.UserProfiles.CountAsync();
        if (userProfileCount > 1)
        {
            return;
        }

        // Danish locale is not available, nb_NO is norwegian. Close enough!
        var lookingForFaker = new Faker<LookingFor>("nb_NO")
            .RuleFor(lf => lf.Name, f => f.Commerce.Product())
            .RuleFor(lf => lf.Description, f => f.Lorem.Sentence())
            .RuleFor(lf => lf.CreatedUtc, f => f.Date.Past());

        var childProfileFaker = new Faker<ChildProfile>("nb_NO")
            .RuleFor(cp => cp.Id, f => Guid.NewGuid())
            .RuleFor(cp => cp.FirstName, f => f.Name.FirstName())
            .RuleFor(cp => cp.LastName, f => f.Name.LastName())
            .RuleFor(cp => cp.Gender, f => f.PickRandom<Gender>())
            .RuleFor(cp => cp.DateOfBirth, f => f.Date.Between(DateTime.UtcNow.AddYears(-14), DateTime.UtcNow))
            .RuleFor(cp => cp.Description, f => f.Lorem.Sentence())
            .RuleFor(cp => cp.PictureUrl, f => f.Internet.Avatar());

        var userFaker = new Faker<UserProfile>("nb_NO")
            .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
            .RuleFor(u => u.UserName, f => f.Internet.UserName())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(u => u.Address, f => f.Address.StreetAddress())
            .RuleFor(u => u.ZipCode, f => f.Random.Int(999, 9991).ToString())
            .RuleFor(u => u.City, f => f.Address.City())
            .RuleFor(u => u.Description, f => f.Lorem.Sentence())
            .RuleFor(u => u.LookingFor, f => lookingForFaker.Generate(f.Random.Int(1, 3)))
            .RuleFor(u => u.ChildProfiles, f => childProfileFaker.Generate(f.Random.Int(1, 3)))
            .RuleFor(u => u.DateOfBirth, f => f.Date.Between(DateTime.UtcNow.AddYears(-70), DateTime.UtcNow.AddYears(-16)))
            .RuleFor(u => u.ProfilePictureUrl, f => f.Internet.Avatar())
            .FinishWith((_, userProfile) => Console.WriteLine($"UserProfile Created! UserName={userProfile.UserName}"));

        var users = userFaker.Generate(10000);

        context.AddRange(users);

        await context.SaveChangesAsync();
    }

    private static async Task InsertLookingForDataAsync(this JordnaerDbContext context)
    {
        if (await context.LookingFor.AnyAsync())
        {
            return;
        }

        context.LookingFor.AddRange(
            new LookingFor { Name = "Legeaftaler" },
            new LookingFor { Name = "Legegrupper/Legestuer" },
            new LookingFor { Name = "Hjemmepasnings-grupper" },
            new LookingFor { Name = "Hjemmeundervisnings-grupper" },
            new LookingFor { Name = "Privat Dagpleje" },
            new LookingFor { Name = "Mødregrupper" },
            new LookingFor { Name = "Fædregrupper" },
            new LookingFor { Name = "Forældregrupper" },
            new LookingFor { Name = "Sportsaktiviteter" },
            new LookingFor { Name = "Kunst og håndværksværksaktiviteter" },
            new LookingFor { Name = "Musik og danseaktiviteter" },
            new LookingFor { Name = "Uddannelsesaktiviteter" },
            new LookingFor { Name = "Andet" });

        await context.SaveChangesAsync();
    }
}

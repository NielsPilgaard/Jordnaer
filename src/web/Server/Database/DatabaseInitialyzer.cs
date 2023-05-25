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
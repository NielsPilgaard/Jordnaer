using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<RemindMeDbContext>();

        await context.Database.MigrateAsync();
    }
}

using Microsoft.EntityFrameworkCore;

namespace RemindMeApp.Server.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<RemindMeDbContext>();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
            await context.Database.MigrateAsync();
    }
}

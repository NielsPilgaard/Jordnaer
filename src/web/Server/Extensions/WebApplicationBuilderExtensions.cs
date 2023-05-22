using Jordnaer.Server.Database;

namespace Jordnaer.Server.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        string dbConnectionString = GetConnectionString(builder.Configuration);

        builder.Services.AddSqlServer<JordnaerDbContext>(dbConnectionString);

        builder.Services.AddHealthChecks().AddSqlServer(dbConnectionString);

        return builder;
    }

    private static string GetConnectionString(IConfiguration configuration) =>
        Environment.GetEnvironmentVariable($"ConnectionStrings_{nameof(JordnaerDbContext)}")
        ?? configuration.GetConnectionString(nameof(JordnaerDbContext))
        ?? throw new InvalidOperationException(
            $"Connection string '{nameof(JordnaerDbContext)}' not found.");
}

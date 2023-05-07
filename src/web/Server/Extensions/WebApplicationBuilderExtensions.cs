using Jordnaer.Server.Database;

namespace Jordnaer.Server.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        string dbConnectionString = Environment.GetEnvironmentVariable($"ConnectionStrings_{nameof(JordnaerDbContext)}")
                                    ?? builder.Configuration.GetConnectionString(nameof(JordnaerDbContext))
                                    ?? throw new InvalidOperationException(
                                        $"Connection string '{nameof(JordnaerDbContext)}' not found.");
        builder.Services.AddSqlServer<JordnaerDbContext>(dbConnectionString);
        builder.Services.AddHealthChecks().AddSqlServer(dbConnectionString);

        return builder;
    }
}

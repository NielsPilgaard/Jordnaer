using Jordnaer.Server.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jordnaer.Server.Tests;

public class JordnaerWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services => services.RemoveAll<IHostedService>());

    public JordnaerWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable($"ConnectionStrings_{nameof(JordnaerDbContext)}",
            "Server=localhost;" +
            "Initial Catalog=jordnaer;" +
            "User Id=sa;" +
            "Password=6efe173b-3e33-4d6c-8f50-3e5f7cadd54c;" +
            "Persist Security Info=True;" +
            "MultipleActiveResultSets=False;" +
            "Encrypt=False;" +
            "TrustServerCertificate=True;" +
            "Connection Timeout=30;");
    }
}

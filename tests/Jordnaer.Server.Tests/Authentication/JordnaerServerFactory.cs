using Microsoft.AspNetCore.Mvc.Testing;

namespace Jordnaer.Server.Tests.Authentication;

public class JordnaerServerFactory : WebApplicationFactory<Program>
{
    public JordnaerServerFactory(WebApplicationFactory<Program> factory)
    {
        factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:RemindMeDbContext", "Data Source=:memory:"));
    }
}

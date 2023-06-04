using Xunit;

namespace Jordnaer.Server.Tests.Profile;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class ProfileApi_Should
{
    private readonly JordnaerWebApplicationFactory _factory;

    public ProfileApi_Should(JordnaerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // TODO: Test ProfileApi
}

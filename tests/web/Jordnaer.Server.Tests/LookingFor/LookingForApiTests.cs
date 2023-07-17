using Xunit;

namespace Jordnaer.Server.Tests.LookingFor;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class LookingForApi_Should
{
    private readonly JordnaerWebApplicationFactory _factory;

    public LookingForApi_Should(JordnaerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // TODO: Test LookingForApi
}

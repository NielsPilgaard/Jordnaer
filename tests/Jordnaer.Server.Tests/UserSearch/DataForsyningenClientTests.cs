using Xunit;

namespace Jordnaer.Server.Tests.UserSearch;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class LookingForApiTests
{
    private readonly JordnaerWebApplicationFactory _factory;

    public LookingForApiTests(JordnaerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // TODO: Test IDataForsyningenClient
}

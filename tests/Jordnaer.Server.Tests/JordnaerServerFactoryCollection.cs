using Xunit;

namespace Jordnaer.Server.Tests;

[CollectionDefinition(nameof(JordnaerServerFactoryCollection))]
public class JordnaerServerFactoryCollection : ICollectionFixture<JordnaerWebApplicationFactory>
{ }

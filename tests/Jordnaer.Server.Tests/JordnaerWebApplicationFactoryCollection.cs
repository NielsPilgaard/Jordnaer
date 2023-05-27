using Xunit;

namespace Jordnaer.Server.Tests;

[CollectionDefinition(nameof(JordnaerWebApplicationFactoryCollection))]
public class JordnaerWebApplicationFactoryCollection : ICollectionFixture<JordnaerWebApplicationFactory>
{ }

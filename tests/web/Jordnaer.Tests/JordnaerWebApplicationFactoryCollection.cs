using Xunit;

namespace Jordnaer.Tests;

[CollectionDefinition(nameof(JordnaerWebApplicationFactoryCollection))]
public class JordnaerWebApplicationFactoryCollection : ICollectionFixture<JordnaerWebApplicationFactory>
{ }

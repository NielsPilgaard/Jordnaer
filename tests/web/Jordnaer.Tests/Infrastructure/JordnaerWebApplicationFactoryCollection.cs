using Xunit;

namespace Jordnaer.Tests.Infrastructure;

[CollectionDefinition(nameof(JordnaerWebApplicationFactoryCollection))]
public class JordnaerWebApplicationFactoryCollection : ICollectionFixture<JordnaerWebApplicationFactory>;
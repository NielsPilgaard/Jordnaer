using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Jordnaer.Server.Tests.Authentication;

[CollectionDefinition(nameof(JordnaerServerFactory))]
public class JordnaerServerFactory : ICollectionFixture<WebApplicationFactory<Program>>
{ }

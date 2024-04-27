using Jordnaer.Database;
using Xunit;

namespace Jordnaer.Tests.Infrastructure;

[CollectionDefinition(nameof(SqlServerContainerCollection))]
public class SqlServerContainerCollection : ICollectionFixture<SqlServerContainer<JordnaerDbContext>>;
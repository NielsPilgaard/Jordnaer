using Jordnaer.Database;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Jordnaer.Server.Benchmarks;

public class BenchmarkWebApplicationFactory : WebApplicationFactory<Program>
{
	public BenchmarkWebApplicationFactory()
	{
		Environment.SetEnvironmentVariable($"ConnectionStrings_{nameof(JordnaerDbContext)}",
			"Server=localhost;" +
			"Initial Catalog=jordnaer;" +
			"User Id=sa;" +
			"Password=6efe173b-3e33-4d6c-8f50-3e5f7cadd54c;" +
			"Persist Security Info=True;" +
			"MultipleActiveResultSets=False;" +
			"Encrypt=False;" +
			"TrustServerCertificate=True;" +
			"Connection Timeout=30;");
	}
}

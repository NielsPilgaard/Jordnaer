using System.Reflection;
using BenchmarkDotNet.Running;

namespace Jordnaer.Benchmarks;

public class StartBenchmark
{
	public static void Main() => BenchmarkRunner.Run(Assembly.GetExecutingAssembly());
}

using BenchmarkDotNet.Running;
using FluentAssertions;
using Jordnaer.Server.Benchmarks;
using Xunit;
using Xunit.Abstractions;

namespace Jordnaer.Server.Regression.Tests;

[Trait("Category", "RegressionTest")]
public class RenderProfile_Should
{
    private readonly ITestOutputHelper _testOutputHelper;

    public RenderProfile_Should(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Be_Successfully_In_Less_Than_1_Millisecond()
    {
        // Arrange
        const double maxElapsedNanoseconds = 1_000_000; // 1 millisecond

        // Act
        var benchmarkSummary =
            BenchmarkRunner.Run<RenderProfileBenchmark>();

        var benchmarkReport = benchmarkSummary.BenchmarksCases.FirstOrDefault()?.Config.GetCompositeAnalyser()
            .Analyse(benchmarkSummary).FirstOrDefault()?.Report;

        // Assert
        benchmarkReport.Should().NotBeNull();
        benchmarkReport!.Success.Should().BeTrue();

        benchmarkReport.ResultStatistics.Should().NotBeNull();
        benchmarkReport.ResultStatistics!.Mean.Should().BeLessThan(maxElapsedNanoseconds);

        _testOutputHelper.WriteLine($"Mean time to render a profile was {benchmarkReport.ResultStatistics.Mean / 1_000_000}ms");
    }
}

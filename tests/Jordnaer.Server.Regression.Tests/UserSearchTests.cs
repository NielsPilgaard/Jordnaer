using BenchmarkDotNet.Running;
using FluentAssertions;
using Jordnaer.Server.Benchmarks;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Jordnaer.Server.Regression.Tests;

public class UserSearch_Should
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UserSearch_Should(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Return_Results_In_Less_Than_1_Second()
    {
        // Arrange
        const double maxElapsedNanoseconds = 1_000_000_000; // 1 second

        // Act
        var benchmarkSummary = BenchmarkRunner.Run<RenderProfileBenchmark>();

        // Assert
        var benchmarkCases = benchmarkSummary.BenchmarksCases;
        foreach (var benchmarkCase in benchmarkCases)
        {
            var benchmarkReport = benchmarkCase
                .Config
                .GetCompositeAnalyser()
                .Analyse(benchmarkSummary)
                .FirstOrDefault()
                ?.Report;

            benchmarkReport.Should().NotBeNull();
            benchmarkReport!.Success.Should().BeTrue();

            benchmarkReport.ResultStatistics.Should().NotBeNull();
            benchmarkReport.ResultStatistics!.Mean.Should().BeLessThan(maxElapsedNanoseconds);

            _testOutputHelper.WriteLine(
                $"BenchmarkCase: {JsonConvert.SerializeObject(benchmarkCase, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })}");

            _testOutputHelper.WriteLine($"{benchmarkCase.DisplayInfo}: Mean time to search for a user was {benchmarkReport.ResultStatistics.Mean / 1_000_000}ms");
        }
    }
}

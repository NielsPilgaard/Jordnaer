using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;
using NBomberRunner = NBomber.CSharp.NBomberRunner;
using Scenario = NBomber.CSharp.Scenario;

namespace Jordnaer.Load.Tests;

public class LandingPageLoadTest
{
	public static ScenarioProps MainScenario() =>
		Scenario.Create(nameof(LandingPageLoadTest), async _ =>
			{
				using var httpClient = new HttpClient();

				var request =
					Http.CreateRequest("GET", Constants.MainUrl)
						.WithHeader("Content-Type", "application/json");

				var response = await Http.Send(httpClient, request);

				return response;
			})
			.WithWarmUpDuration(TimeSpan.FromSeconds(3))
			.WithLoadSimulations(
				Simulation.RampingInject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
				Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
				Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
			);

	[Fact]
	public void RampUpTo50Users_Over1Minute_Then_50Users_Over1Minute_Then_RampDownTo0Users_Over1Minute()
	{
		var scenario = MainScenario();

		var result = NBomberRunner
			.RegisterScenarios(scenario)
			.Run();

		Assert.True(result.AllBytes > 0);
		Assert.True(result.AllRequestCount > 0);
		Assert.True(result.AllOkCount > 0);
		Assert.Equal(0, result.AllFailCount);

		var scnStats = result.GetScenarioStats(nameof(LandingPageLoadTest));

		Assert.True(scnStats.Ok.Latency.MinMs > 0);
		Assert.True(scnStats.Ok.Latency.MaxMs < 1_000);
		Assert.Equal(0, scnStats.Fail.Request.Count);
	}
}

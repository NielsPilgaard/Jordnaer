using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;
using NBomberRunner = NBomber.CSharp.NBomberRunner;
using Scenario = NBomber.CSharp.Scenario;

namespace Jordnaer.Load.Tests;

public class UserSearchLoadTests
{
	public static ScenarioProps MainScenario() =>
		Scenario.Create(nameof(UserSearchLoadTests), async _ =>
			{
				using var httpClient = new HttpClient();

				var request =
					Http.CreateRequest("GET", $"{Constants.MainUrl}/api/users/search?PageNumber=1&PageSize=10")
						.WithHeader("Content-Type", "application/json");

				var response = await Http.Send(httpClient, request);

				return response;
			})
			.WithWarmUpDuration(TimeSpan.FromSeconds(3))
			.WithLoadSimulations(
				Simulation.RampingInject(rate: 15, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
				Simulation.Inject(rate: 15, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
				Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
			);

	[Fact]
	public void RampUpTo15Users_Over1Minute_Then_15Users_Over1Minute_Then_RampDownTo0Users_Over1Minute()
	{
		var scenario = MainScenario();

		var result = NBomberRunner
			.RegisterScenarios(scenario)
			.Run();

		Assert.True(result.AllBytes > 0);
		Assert.True(result.AllRequestCount > 0);
		Assert.True(result.AllOkCount > 0);
		Assert.True(result.AllFailCount == 0);

		var scnStats = result.GetScenarioStats(nameof(UserSearchLoadTests));

		Assert.True(scnStats.Ok.Latency.MinMs > 0);
		Assert.True(scnStats.Ok.Latency.MaxMs < 1_000);
		Assert.True(scnStats.Fail.Request.Count == 0);
	}
}

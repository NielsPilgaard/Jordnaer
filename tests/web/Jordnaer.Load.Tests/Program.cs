// See https://aka.ms/new-console-template for more information

using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

SimpleHttpExample();

static void SimpleHttpExample()
{
    using var httpClient = new HttpClient();

    var scenario = Scenario.Create("http_scenario", async _ =>
        {
            var request =
                Http.CreateRequest("GET", "https://jordnaer.azurewebsites.net/")
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

    var result = NBomberRunner
        .RegisterScenarios(scenario)
        .Run();

    Assert.True(result.AllBytes > 0);
    Assert.True(result.AllRequestCount > 0);
    Assert.True(result.AllOkCount > 0);
    Assert.True(result.AllFailCount == 0);

    var scnStats = result.GetScenarioStats("http_scenario");

    Assert.True(scnStats.Ok.Request.RPS > 0);
    Assert.True(scnStats.Ok.Request.Count > 0);
    Assert.True(scnStats.Ok.Latency.MinMs > 0);
    Assert.True(scnStats.Ok.Latency.MaxMs > 0);
    Assert.True(scnStats.Fail.Request.Count == 0);
    Assert.True(scnStats.Fail.Request.Count == 0);
    Assert.True(scnStats.Fail.Latency.MinMs == 0);
}

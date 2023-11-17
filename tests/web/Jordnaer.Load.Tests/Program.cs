// See https://aka.ms/new-console-template for more information

using Jordnaer.Load.Tests;
using NBomber.CSharp;

NBomberRunner.RegisterScenarios(
    LandingPageLoadTest.MainScenario(),
    UserSearchLoadTests.MainScenario())
    .Run();

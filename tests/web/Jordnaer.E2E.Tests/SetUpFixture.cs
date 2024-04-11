using Microsoft.Playwright;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[SetUpFixture]
public class SetUpFixture
{
	[OneTimeSetUp]
	public void GlobalSetup()
	{
		//// We set this environment variable to false in the CI pipeline,
		//// so if it's null or true, run installation
		//var installPlaywright = Environment.GetEnvironmentVariable("InstallPlaywright");
		//if (string.IsNullOrEmpty(installPlaywright) ||
		//	installPlaywright.Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase))
		//{
		Program.Main(["install"]);
		//}
	}
}
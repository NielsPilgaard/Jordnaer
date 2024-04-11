using Microsoft.Playwright;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[SetUpFixture]
public class SetUpFixture
{
	[OneTimeSetUp]
	public void GlobalSetup()
	{
		// Always run install, it stops if Playwright is already installed
		Program.Main(["install"]);
	}
}
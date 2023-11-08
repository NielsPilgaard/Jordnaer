using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("UI")]
public class LoginTests : PageTest
{
    [Test]
    public void AuthenticationFlow()
    {
        // TODO: Run "playwright.ps1 codegen jordnaer.azurewebsites.net --save-storage=auth.json"

        // TODO: Subsequent tests should use "playwright.ps1 codegen --load-storage=auth.json jordnaer.azurewebsites.net
    }
}

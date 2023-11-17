using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("UITest")]
public class LoginTests : PageTest
{
    [Test]
    public void AuthenticationFlow()
    {
        // TODO: Run "powershell.exe .\playwright.ps1 codegen jordnaer.azurewebsites.net --save-storage=auth.json"

        // TODO: Subsequent tests should use "powershell.exe .\playwright.ps1 codegen --load-storage=auth.json jordnaer.azurewebsites.net
    }
}

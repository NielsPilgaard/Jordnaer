Run unit tests for the Jordnaer project.

If a test name or filter pattern is provided as $ARGUMENTS, run only matching tests.
Otherwise, run all unit tests (excluding tests marked SkipInCi).

## Steps

1. Run the appropriate test command:
   - **With filter**: `dotnet test tests/web/Jordnaer.Tests --filter "FullyQualifiedName~$ARGUMENTS" --filter Category!=SkipInCi`
   - **Without filter**: `dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi`
2. Report the results clearly: how many passed, failed, skipped.
3. If any tests fail, analyze the failure output and suggest fixes.

## E2E Tests

For frontend/UI changes, also run the Playwright E2E tests. The suite is self-contained — it starts the app, SQL Server, and Azurite automatically via Testcontainers and `WebApplicationFactory`. No manual setup required.

- **All E2E tests**: `dotnet test tests/web/Jordnaer.E2E.Tests`
- **Specific E2E test**: `dotnet test tests/web/Jordnaer.E2E.Tests --filter "FullyQualifiedName~$ARGUMENTS"`

E2E test options are configured via `appsettings.json` or environment variables under the `Playwright` section (headless mode, browser, slow-mo).

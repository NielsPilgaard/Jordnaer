Run unit tests for the Jordnaer project.

If a test name or filter pattern is provided as $ARGUMENTS, run only matching tests.
Otherwise, run all unit tests (excluding tests marked SkipInCi).

## Steps

1. Run the appropriate test command:
   - **With filter**: `dotnet test tests/web/Jordnaer.Tests --filter "FullyQualifiedName~$ARGUMENTS" --filter Category!=SkipInCi`
   - **Without filter**: `dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi`
2. Report the results clearly: how many passed, failed, skipped.
3. If any tests fail, analyze the failure output and suggest fixes.

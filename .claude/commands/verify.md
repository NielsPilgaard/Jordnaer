Run the full verification pipeline: build, lint, and test.

Use this after completing changes to ensure everything is clean.

## Steps

1. **Build**: Run `dotnet build` - stop if this fails.
2. **Lint**: Run `dotnet format analyzers --verify-no-changes --diagnostics` and `dotnet format style --verify-no-changes --diagnostics` - report issues but continue.
3. **Test**: Run `dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi` - report results.
4. **E2E Tests** *(only if frontend/UI changes were made)*: Run `dotnet test tests/web/Jordnaer.E2E.Tests` - self-contained, no manual app startup needed. Report results.
5. **Summary**: Report overall status (pass/fail) and list any issues that need attention.

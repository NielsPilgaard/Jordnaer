Analyze code quality using dotnet format analyzers.

If $ARGUMENTS is provided, treat it as a path or project to analyze.
Otherwise, analyze the entire solution.

## Steps

1. Run the appropriate lint command:
   - **With path**: `dotnet format analyzers $ARGUMENTS --verify-no-changes`
   - **Without path**: `dotnet format analyzers --verify-no-changes`
2. Also run style checks: `dotnet format style --verify-no-changes` (same scoping).
3. Report any violations found, grouped by severity (error, warning, info).
4. Suggest fixes for any issues found.

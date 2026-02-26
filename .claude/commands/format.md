Format C# code using dotnet format.

If $ARGUMENTS is provided, treat it as a path or project to format.
Otherwise, format the entire solution.

## Steps

1. Run the appropriate format command:
   - **With path**: `dotnet format $ARGUMENTS`
   - **Without path**: `dotnet format`
2. Report what files were changed (if any).
3. Summarize the formatting changes.

# AGENTS.md

Instructions for AI coding agents working on this repository.

## Project Overview

**Mini Møder** (codebase name: Jordnaer) - Blazor-based social platform helping parents find playgroups, parenting groups, and like-minded connections for their children. Built with ASP.NET Core 10.0 (.NET 10), using Interactive Server Components with SignalR for real-time features.

- **Frontend**: Blazor Interactive Server, Razor Components, MudBlazor UI framework
- **Backend**: ASP.NET Core 10.0, feature-based organization
- **Database**: EF Core 10 with SQL Server
- **Real-time**: SignalR hubs for chat and notifications
- **Messaging**: MassTransit v8.5.7 (pinned - v9+ requires commercial license)

## Build & Test

```bash
dotnet build                                                              # Build solution
dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi          # Unit tests
dotnet test tests/web/Jordnaer.Tests --filter "FullyQualifiedName~Name"   # Specific test
dotnet format                                                             # Format code
dotnet format analyzers --verify-no-changes --diagnostics                 # Lint check
dotnet format style --verify-no-changes --diagnostics                     # Style check
```

## Project Structure

```
src/
├── web/Jordnaer/                    # Main web application
│   ├── Features/                    # Feature modules (self-contained)
│   ├── Components/                  # Shared Razor components
│   ├── Consumers/                   # MassTransit message consumers
│   ├── Database/                    # JordnaerDbContext + migrations
│   ├── Extensions/                  # DI + middleware setup
│   └── SignalR/                     # Hub interfaces and clients
└── shared/Jordnaer.Shared/          # Models, DTOs, interfaces

tests/web/
├── Jordnaer.Tests/                  # Unit tests (xUnit)
└── Jordnaer.E2E.Tests/              # E2E tests (NUnit + Playwright)
```

## Code Style

- **Indentation**: Tabs (4-width) for C#, Razor, XML/csproj files
- **Line endings**: CRLF
- **Namespaces**: File-scoped (`namespace X;`)
- **Braces**: Required for all control flow (`if`, `for`, `foreach`, `while`)
- **Type inference**: Use `var` everywhere, especially for built-in types
- **Using directives**: Outside namespace
- **Primary constructors**: Enabled by convention
- **Nullable reference types**: Enabled globally

See `.editorconfig` for the full ruleset. Do not reformat code you didn't change.

## Critical Rules

1. **DbContext**: Always use `IDbContextFactory<JordnaerDbContext>`, never inject `JordnaerDbContext` directly. Always dispose with `await using`.
2. **Read-only queries**: Always use `AsNoTracking()` for queries that don't modify data.
3. **Feature registration**: DI setup goes through `WebApplicationBuilderExtensions.cs` in each feature folder, registered in `Program.cs`.
4. **MassTransit**: Keep at v8.5.7. Do NOT upgrade (v9+ requires commercial license).
5. **Minimal diffs**: Do not reformat unchanged code. Keep diffs focused on the actual change.
6. **After changes**: Run tests to ensure nothing is broken.

## Security Considerations

- Never hardcode secrets - use `.env` (local) or app settings (production)
- Never commit `.env`, `credentials.json`, or similar files
- All SignalR hubs require `[Authorize]`
- Validate user input at system boundaries

## Key Files

- Entry point: `src/web/Jordnaer/Program.cs`
- Database context: `src/web/Jordnaer/Database/JordnaerDbContext.cs`
- DI extensions: `src/web/Jordnaer/Extensions/WebApplicationBuilderExtensions.cs`
- Feature modules: `src/web/Jordnaer/Features/`
- Message consumers: `src/web/Jordnaer/Consumers/`

## Detailed Documentation

For in-depth guidance on specific topics, see:

- **Code patterns** (DbContext, OneOf, MassTransit, SignalR, feature modules): `.claude/docs/patterns.md`
- **Testing guide**: `.claude/docs/testing.md`
- **Design system** (colors, MudBlazor components): `docs/DESIGN_SYSTEM.md`
- **Feature-specific docs**: `docs/`

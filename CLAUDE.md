# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Mini Møder** (codebase name: Jordnaer) - Blazor-based social platform helping parents find playgroups, parenting groups, and like-minded connections for their children. Built with ASP.NET Core 10.0 (.NET 10), using Interactive Server Components with SignalR for real-time features.

## Development Commands

### Build & Run

```powershell
# Build the solution
dotnet build

# Run the web application (from src/web/Jordnaer/)
dotnet run
```

### Testing

```powershell
# Run unit tests (excludes tests marked SkipInCi)
dotnet test tests/web/Jordnaer.Tests

# Run all tests including E2E
dotnet test tests/web

# Run specific test
dotnet test tests/web/Jordnaer.Tests --filter FullyQualifiedName~YourTestName
```

### Database Migrations

```powershell
# Create a new migration (from src/web/Jordnaer/)
dotnet ef migrations add YourMigrationName

# Apply migrations (automatic in Development via InitializeDatabaseAsync)
dotnet ef database update
```

### Code Review

After completing work, run CodeRabbit for automated review:

```batch
scripts\coderabbit.bat
```

Review output appears in the `reviews/` folder. Apply relevant suggestions before committing.

## Architecture

### Technology Stack

- **Frontend**: Blazor Interactive Server, Razor Components, MudBlazor UI framework
- **Backend**: ASP.NET Core 10.0, feature-based organization
- **Database**: EF Core 10 with SQL Server (Azure SQL in production)
- **Messaging**: MassTransit v8.5.7 with in-memory transport + outbox pattern (pinned version - v9+ requires commercial license)
- **Real-time**: SignalR hubs for chat and group membership notifications
- **Storage**: Azure Blob Storage (profile images, attachments)
- **Observability**: Serilog + OpenTelemetry (Aspire Dashboard in dev, Grafana in prod)
- **External Services**: Azure Communication Email, DSFAPI (Danish civil registry), OAuth (Google/Microsoft/Facebook)

### Project Structure

```
src/
├── web/Jordnaer/                       # Main web application
│   ├── Features/                       # Feature modules (self-contained)
│   │   ├── Groups/                     # Group management
│   │   ├── Chat/                       # Real-time messaging
│   │   ├── Posts/                      # User posts
│   │   ├── Authentication/             # Auth + UserCircuitHandler
│   │   └── [Feature]/WebApplicationBuilderExtensions.cs  # DI setup
│   ├── Components/                     # Shared Razor components
│   ├── Consumers/                      # MassTransit message consumers
│   ├── Database/                       # JordnaerDbContext + migrations
│   ├── Extensions/                     # DI + middleware setup
│   └── SignalR/                        # Hub interfaces and clients
└── shared/
    └── Jordnaer.Shared/                # Models, DTOs, interfaces

tests/web/
├── Jordnaer.Tests/                     # Unit tests
└── Jordnaer.E2E.Tests/                 # Playwright E2E tests
```

## Core Patterns

### Feature Module Pattern

Each feature in `Features/` is self-contained with its own DI registration:

```csharp
// Features/YourFeature/WebApplicationBuilderExtensions.cs
public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddYourFeatureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<YourFeatureService>();
        // Register other services...
        return builder;
    }
}
```

Register in [Program.cs](src/web/Jordnaer/Program.cs):

```csharp
builder.AddYourFeatureServices();
```

### Database Access with IDbContextFactory

**CRITICAL**: Always use `IDbContextFactory<JordnaerDbContext>` and dispose contexts properly:

```csharp
public class YourService(IDbContextFactory<JordnaerDbContext> contextFactory)
{
    public async Task<OneOf<Data, NotFound>> GetDataAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var data = await context.YourTable
            .AsNoTracking()  // For read-only queries
            .FirstOrDefaultAsync(x => x.Id == id);

        return data is not null ? data : new NotFound();
    }
}
```

**Rules**:

- Always use `await using var context = await contextFactory.CreateDbContextAsync()`
- Use `AsNoTracking()` for read-only queries to improve performance
- Never inject `JordnaerDbContext` directly - always use the factory

### Result Types with OneOf

Use `OneOf<TSuccess, TError>` for explicit error handling:

```csharp
public async Task<OneOf<Group, NotFound>> GetGroupAsync(Guid id)
{
    await using var context = await contextFactory.CreateDbContextAsync();
    var group = await context.Groups.FindAsync(id);
    return group ?? new NotFound();
}

// Usage in components
var result = await groupService.GetGroupAsync(id);
result.Match(
    group => HandleSuccess(group),
    notFound => HandleNotFound()
);
```

### MassTransit Consumers

Background processing via message consumers (auto-discovered in `Jordnaer.Consumers` namespace):

```csharp
public class SendEmailConsumer(ILogger<SendEmailConsumer> logger, EmailClient client)
    : IConsumer<SendEmail>
{
    public async Task Consume(ConsumeContext<SendEmail> context)
    {
        var message = context.Message;
        // Process message
        // Track metrics: JordnaerMetrics.Counter.Add()
    }
}
```

**Note**: Using in-memory transport with outbox pattern. Future plan: migrate to Azure Container Apps when chat reaches 100+ messages/hour.

### SignalR Pattern

Define hub interface + use `IHubContext` for server-to-client communication:

```csharp
// Hub interface
public interface IChatHub
{
    Task ReceiveChatMessage(ChatMessageDto message);
}

// Hub implementation
[Authorize]
public class ChatHub : Hub<IChatHub> { }

// Broadcasting from consumers/services
public class YourConsumer(IHubContext<ChatHub, IChatHub> chatHub) : IConsumer<YourMessage>
{
    public async Task Consume(ConsumeContext<YourMessage> context)
    {
        await chatHub.Clients.Users(recipientIds).ReceiveChatMessage(message);
    }
}
```

Register hubs in [Program.cs:174-175](src/web/Jordnaer/Program.cs#L174-L175):

```csharp
app.MapHub<ChatHub>("/hubs/chat");
```

### Circuit State Management

`UserCircuitHandler` (see [Features/Authentication/UserCircuitHandler.cs](src/web/Jordnaer/Features/Authentication/UserCircuitHandler.cs)) manages Blazor Server circuit lifecycle:

- Tracks current user and profile in `CurrentUser` scoped service
- Subscribes to authentication and profile changes
- Critical for maintaining user state across SignalR reconnections

## Development Workflow

### Adding a New Feature

1. Create `Features/YourFeature/` directory
2. Add service class with `IDbContextFactory<JordnaerDbContext>` dependency
3. Create `WebApplicationBuilderExtensions.cs` with `AddYourFeatureServices()` method
4. Register in [Program.cs](src/web/Jordnaer/Program.cs)
5. Add Razor components in `Components/` if needed
6. Write tests in `tests/web/Jordnaer.Tests/`
7. Run tests: `dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi`
8. Run CodeRabbit: `scripts\coderabbit.bat`

### After Completing Changes

1. **Run tests** to ensure nothing is broken
2. **Create acceptance test checklist** - identify what needs manual testing
3. **Run CodeRabbit** via `scripts\coderabbit.bat` to generate code review
4. **Review and apply** relevant CodeRabbit suggestions

## Critical Rules

1. **DbContext Disposal**: Always `await using var context = await contextFactory.CreateDbContextAsync()`
2. **No Reformatting**: Do not reformat unchanged code - keep diffs minimal
3. **Feature Registration**: All DI setup goes through `WebApplicationBuilderExtensions.cs` in feature folders
4. **MassTransit Version**: Keep MassTransit at v8.5.7 (v9+ requires commercial license)
5. **SignalR State**: `UserCircuitHandler` manages circuit state - see [Features/Authentication/](src/web/Jordnaer/Features/Authentication/)
6. **Environment Setup**: Development uses `InitializeDatabaseAsync()` for auto-migrations; production requires manual migration
7. **Read-Only Queries**: Always use `AsNoTracking()` for queries that don't modify data

## Environment Configuration

### Local Development

- Uses `docker-compose.yml` for SQL Server + Azurite blob storage
- Connection strings in `.env` (see `.env.example` for template)
- OpenTelemetry exports to Aspire Dashboard (OTLP endpoint)

### Production

- Azure SQL Database
- Azure Blob Storage
- Grafana for observability
- OAuth providers configured via app settings

## Useful File Paths

- Main entry point: [src/web/Jordnaer/Program.cs](src/web/Jordnaer/Program.cs)
- Database context: [src/web/Jordnaer/Database/JordnaerDbContext.cs](src/web/Jordnaer/Database/JordnaerDbContext.cs)
- DI extensions: [src/web/Jordnaer/Extensions/WebApplicationBuilderExtensions.cs](src/web/Jordnaer/Extensions/WebApplicationBuilderExtensions.cs)
- Message consumers: [src/web/Jordnaer/Consumers/](src/web/Jordnaer/Consumers/)
- Feature modules: [src/web/Jordnaer/Features/](src/web/Jordnaer/Features/)

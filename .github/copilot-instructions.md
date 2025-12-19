# Jordnaer Codebase Instructions

## Architecture Overview

**Mini Møder** is a Blazor-based social platform helping parents find playgroups and communities. The architecture uses:

- **Frontend**: Interactive Razor components with MudBlazor UI library  
- **Backend**: ASP.NET Core web API with feature-based folder structure
- **Messaging**: MassTransit with in-memory bus (planned: Azure Container Apps for scaling)
- **Real-time**: SignalR hubs for chat and notifications
- **Database**: EF Core with SQL Server, Azure SQL in production
- **Observability**: Serilog + OpenTelemetry (Grafana in prod, Aspire dashboard in dev)

## Project Structure

```
src/
├── web/Jordnaer/              # Main Blazor app
│   ├── Features/              # Feature modules (Groups, Chat, Posts, etc.)
│   ├── Components/            # Razor components
│   ├── Extensions/            # DI setup extension methods
│   ├── Consumers/             # MassTransit message handlers
│   ├── Database/              # EF Core context
│   └── SignalR/               # Hub definitions
├── shared/
│   ├── Jordnaer.Shared/       # Shared models, DTOs, interfaces
│   └── Jordnaer.Shared.Infrastructure/  # Infrastructure abstractions
└── container_apps/
    └── Jordnaer.Chat/         # (Future) Chat service for Container Apps

tests/
├── web/Jordnaer.Tests/        # Unit tests
├── web/Jordnaer.E2E.Tests/    # Playwright-based E2E tests
└── web/Jordnaer.UI.Tests/     # UI-specific tests
```

## Core Patterns & Conventions

### 1. Feature Module Setup

Each feature (e.g., Groups, Chat, Posts) follows a standard pattern:

```csharp
// src/web/Jordnaer/Features/Groups/WebApplicationBuilderExtensions.cs
public static WebApplicationBuilder AddGroupServices(this WebApplicationBuilder builder)
{
    builder.Services.AddScoped<GroupService>();
    // DI registrations...
    return builder;
}
```

**Key principle**: All DI setup happens in extension methods called from `Program.cs`. Search `Program.cs` to see all registered features.

### 2. Data Access with Factory Pattern

Services use `IDbContextFactory<JordnaerDbContext>` to create scoped context instances:

```csharp
public class GroupService(IDbContextFactory<JordnaerDbContext> contextFactory) 
{
    public async Task<OneOf<Group, NotFound>> GetGroupByIdAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        // EF queries with AsNoTracking() for read-only operations
        var group = await context.Groups.AsNoTracking().FirstOrDefaultAsync(/* ... */);
        return group is null ? new NotFound() : group;
    }
}
```

**Pattern**: Always use `await using var context` for proper disposal. Use `AsNoTracking()` for read-only queries.

### 3. Result Types with OneOf

The codebase uses the OneOf library for explicit error handling:

```csharp
// Success returns business object or error discriminator
public async Task<OneOf<GroupSlim, NotFound>> GetSlimGroupByNameAsync(string name)
{
    // ... 
    return group is null ? new NotFound() : group;
}

// Usage in consumers/handlers
var result = await groupService.GetGroupByIdAsync(id);
return result.Match(
    group => HandleSuccess(group),
    notFound => HandleNotFound()
);
```

### 4. MassTransit Message Consumers

Background tasks are handled by consumers for async/deferred operations:

```csharp
// src/web/Jordnaer/Consumers/SendEmailConsumer.cs
public class SendEmailConsumer(
    ILogger<SendEmailConsumer> logger,
    EmailClient emailClient)
    : IConsumer<SendEmail>
{
    public async Task Consume(ConsumeContext<SendEmail> consumeContext)
    {
        var message = consumeContext.Message;
        // Process message, use Polly retry policies
        // Track metrics with JordnaerMetrics.Counter.Add()
    }
}
```

**Configuration**: `WebApplicationBuilderExtensions.AddMassTransit()` auto-discovers consumers in `Jordnaer.Consumers` namespace. Uses in-memory transport with outbox pattern for reliability.

**Future**: Move chat consumers to Azure Container Apps when traffic exceeds 100 msg/hour consistently (see `Consumers/README.md`).

### 5. Real-time Communication with SignalR

Chat and notifications use SignalR hubs:

```csharp
// src/web/Jordnaer/SignalR/ChatHub.cs
public interface IChatHub
{
    Task ReceiveChatMessage(ChatMessageDto message);
    Task StartChat(ChatDto chat);
}

// Consumers publish to clients
await chatHub.Clients.Users(recipientIds).ReceiveChatMessage(message);
```

**Pattern**: Hub methods defined in shared interface, consumers use `IHubContext<ChatHub, IChatHub>` to broadcast.

## Development Workflow

### Building & Testing

```powershell
# Restore and build (from repo root)
dotnet build

# Run unit tests (skips slow E2E and SkipInCi marked tests)
dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi

# Run all tests including E2E
dotnet test tests/web

# Run specific test file
dotnet test tests/web/Jordnaer.Tests --filter FullyQualifiedName~SendEmailConsumerTests
```

**CI Configuration**: GitHub Actions workflows check push events filtered by path (e.g., changes to `src/web/Jordnaer/**` trigger backend tests).

### Database Migrations

Managed with EF Core tooling:

```powershell
cd src/web/Jordnaer
# Create migration (check add-migration.ps1 script)
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

### Logging & Observability

Use Serilog's LogContext and structured logging:

```csharp
logger.LogFunctionBegan();  // Custom extension in LoggerExtensions.cs
logger.LogError(exception, "Error context with {Variable}", variable);

// Diagnostic context for correlation
diagnosticContext.Set("GroupId", groupId);

// Metrics
JordnaerMetrics.ChatMessagesSentCounter.Add(1);
```

**Development**: Aspire dashboard at `http://localhost:4318` (auto-configured)  
**Production**: Grafana with OpenTelemetry exporter (configured in `WebApplicationBuilderExtensions.AddOpenTelemetry()`)

## External Integrations

- **Azure Communication Email**: Email service (consumers in `Features/Email/`)
- **Azure Blob Storage**: Profile pictures and attachments
- **DSFAPI (DataForsyningen)**: Danish civil registry for user search
- **OAuth**: Google, Microsoft, Facebook authentication
- **MudBlazor**: Component library (v8.11.0)

## Common Tasks

### Adding a New Feature

1. Create `src/web/Jordnaer/Features/YourFeature/` folder
2. Add domain service class with `IDbContextFactory<JordnaerDbContext>`
3. Create `WebApplicationBuilderExtensions.cs` with `AddYourFeatureServices()`
4. Call in `Program.cs`: `builder.AddYourFeatureServices()`
5. Add Razor components in `Components/`
6. Write tests in `tests/web/Jordnaer.Tests/`

### Publishing Async Messages

Use `IPublishEndpoint` to trigger async work:

```csharp
// In service
await publishEndpoint.Publish<SendEmail>(/* ... */, cancellationToken);

// In consumer
public class EmailConsumer : IConsumer<SendEmail> { /* ... */ }
```

### Testing Consumer Logic

Use NSubstitute for mocking (see `SendEmailConsumerTests.cs`). Create mock `ConsumeContext<T>` to verify consumer behavior.

## Important Notes

- **Feature Flags**: Uses `IFeatureManager` for A/B testing and gradual rollouts
- **Validation**: FluentValidation for input validation across features
- **Rate Limiting**: Custom `RateLimitExtensions` in middleware pipeline
- **Security Headers**: `NetEscapades.AspNetCore.SecurityHeaders` applied in middleware
- **Image Processing**: SixLabors.ImageSharp for profile pictures (handles resizing, validation)
- **Markdown Support**: Markdig library for rich text content
- **HealthChecks**: Endpoint at `/health` (includes SQL Server and self checks)
- **DO NOT**: Re-format code that you have not otherwise changed

## Gotchas & Tips

1. **Always dispose DbContext**: Use `await using var context = await contextFactory.CreateDbContextAsync()`
2. **Slow Email Test**: `SendEmailConsumerTests.Consume_ShouldSendEmailSuccessfully` is marked as `Skip="35 min execution"` — investigate if needed
3. **Consumers Auto-Discovery**: MassTransit auto-discovers `IConsumer<T>` implementations; ensure they're in `Jordnaer.Consumers` namespace
4. **Chat Migration**: Monitor chat volume; start planning Azure Container Apps migration at 100+ msg/hour consistency
5. **SignalR Circuit Handler**: `UserCircuitHandler` manages circuit state; check `Features/Authentication/` for auth context preservation

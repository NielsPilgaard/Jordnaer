# Mini Møder Codebase Instructions

Our project is called Jordnaer but has been rebranded to Mini Møder. This document outlines the architecture, project structure, and core patterns used in the codebase to help new contributors get up to speed quickly.

# Upon completion of an instruction, please:

1. Run the tests to ensure nothing is broken
2. Start CodeRabbit with the following command to generate a code review based on the latest committed changes:

`wsl -d Ubuntu bash -c "cd /mnt/c/Users/Niels/Documents/GitHub/Jordnaer && ~/.local/bin/coderabbit --prompt-only --type committed" > reviews/review-committed-$(date +%Y%m%d-%H%M%S).txt 2>&1`

3. When CodeRabbit review is generated, please review and apply any relevant suggestions to improve code quality.

## Architecture

Blazor social platform for parents to find playgroups. Stack:

- **Frontend**: Razor components + MudBlazor
- **Backend**: ASP.NET Core, feature-based structure
- **Messaging**: MassTransit (in-memory, outbox pattern)
- **Real-time**: SignalR for chat/notifications
- **Database**: EF Core + SQL Server (Azure SQL in prod)
- **Observability**: Serilog + OpenTelemetry

## Project Structure

```
src/web/Jordnaer/
├── Features/          # Feature modules (Groups, Chat, Posts)
├── Components/        # Razor components
├── Extensions/        # DI setup (WebApplicationBuilderExtensions)
├── Consumers/         # MassTransit message handlers
├── Database/          # EF Core context
└── SignalR/          # Hub definitions

src/shared/
├── Jordnaer.Shared/                    # Models, DTOs, interfaces
└── Jordnaer.Shared.Infrastructure/     # Infrastructure abstractions

tests/web/
├── Jordnaer.Tests/        # Unit tests
├── Jordnaer.E2E.Tests/    # Playwright E2E
└── Jordnaer.UI.Tests/     # UI tests
```

## Core Patterns

### 1. Feature Module Setup

Each feature has `WebApplicationBuilderExtensions.cs` for DI registration:

```csharp
public static WebApplicationBuilder AddGroupServices(this WebApplicationBuilder builder)
{
    builder.Services.AddScoped<GroupService>();
    return builder;
}
```

**Rule**: All DI setup in extension methods called from `Program.cs`.

### 2. Data Access

Use `IDbContextFactory<JordnaerDbContext>` for scoped contexts:

```csharp
public class GroupService(IDbContextFactory<JordnaerDbContext> contextFactory)
{
    public async Task<OneOf<Group, NotFound>> GetGroupByIdAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var group = await context.Groups.AsNoTracking().FirstOrDefaultAsync(/* ... */);
        return group ?? new NotFound();
    }
}
```

**Rules**:

- Always `await using var context`
- Use `AsNoTracking()` for read-only queries

### 3. Result Types

Use OneOf for explicit error handling:

```csharp
public async Task<OneOf<Group, NotFound>> GetGroupAsync(Guid id) { /* ... */ }

// Usage
var result = await service.GetGroupAsync(id);
return result.Match(
    group => HandleSuccess(group),
    notFound => HandleNotFound()
);
```

### 4. MassTransit Consumers

Background tasks via consumers:

```csharp
public class SendEmailConsumer(ILogger<SendEmailConsumer> logger, EmailClient client)
    : IConsumer<SendEmail>
{
    public async Task Consume(ConsumeContext<SendEmail> context)
    {
        // Process message, track metrics: JordnaerMetrics.Counter.Add()
    }
}
```

**Auto-discovery**: Consumers in `Jordnaer.Consumers` namespace auto-registered.
**Future**: Migrate chat to Azure Container Apps at 100+ msg/hour.

### 5. SignalR

Hub interface + `IHubContext` for broadcasting:

```csharp
public interface IChatHub
{
    Task ReceiveChatMessage(ChatMessageDto message);
}

// In consumer
await chatHub.Clients.Users(recipientIds).ReceiveChatMessage(message);
```

## Development

### Build & Test

```powershell
dotnet build
dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi  # Unit tests
dotnet test tests/web  # All tests including E2E
```

## External Services

- **Azure Communication Email**: Email sending
- **Azure Blob Storage**: Profile pics, attachments
- **DSFAPI**: Danish civil registry search
- **OAuth**: Google, Microsoft, Facebook

## Common Tasks

### Add Feature

1. Create `Features/YourFeature/` folder
2. Add service with `IDbContextFactory<JordnaerDbContext>`
3. Create `WebApplicationBuilderExtensions.cs` with `AddYourFeatureServices()`
4. Register in `Program.cs`
5. Add components in `Components/`
6. Write tests

## Critical Rules

1. **Always dispose DbContext**: `await using var context = await contextFactory.CreateDbContextAsync()`
2. **DO NOT**: Reformat unchanged code
3. **SignalR**: `UserCircuitHandler` manages circuit state (see `Features/Authentication/`)

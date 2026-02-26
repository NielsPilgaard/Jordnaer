# Code Patterns

Detailed coding patterns used throughout the Jordnaer codebase. Refer to these when implementing new features or modifying existing code.

## Feature Module Pattern

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

Register in `Program.cs`:

```csharp
builder.AddYourFeatureServices();
```

### Adding a New Feature

1. Create `Features/YourFeature/` directory
2. Add service class with `IDbContextFactory<JordnaerDbContext>` dependency
3. Create `WebApplicationBuilderExtensions.cs` with `AddYourFeatureServices()` method
4. Register in `Program.cs`
5. Add Razor components in `Components/` if needed
6. Write tests in `tests/web/Jordnaer.Tests/`

## Database Access with IDbContextFactory

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

## Result Types with OneOf

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

## MassTransit Consumers

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

**Important**: MassTransit is pinned to v8.5.7. Do NOT upgrade to v9+ (requires commercial license).

## SignalR Pattern

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

Register hubs in `Program.cs`:

```csharp
app.MapHub<ChatHub>("/hubs/chat");
```

## Circuit State Management

`UserCircuitHandler` (see `Features/Authentication/UserCircuitHandler.cs`) manages Blazor Server circuit lifecycle:

- Tracks current user and profile in `CurrentUser` scoped service
- Subscribes to authentication and profile changes
- Critical for maintaining user state across SignalR reconnections

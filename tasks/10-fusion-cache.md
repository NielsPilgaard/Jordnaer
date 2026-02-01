# Task: Migrate from IMemoryCache to FusionCache

## Overview
Replace all `IMemoryCache` usage with `IFusionCache` to gain advanced caching features like:
- **Fail-safe**: Return stale data when the underlying data source fails
- **Stampede protection**: Prevent multiple concurrent requests from hitting the database simultaneously
- **Background refresh**: Refresh cache in the background before expiration (eager refresh)
- **Adaptive caching**: Automatically handles cache serialization and distributed scenarios

## Current State

### Caching Infrastructure
- **Package**: `Microsoft.Extensions.Caching.Memory` (IMemoryCache)
- **Registration**: `builder.Services.AddMemoryCache();` in [Program.cs:112](src/web/Jordnaer/Program.cs#L112)
- **No distributed cache** currently configured

### Files to Migrate (4 total)

#### 1. CategoryCache
- **File**: [CategoryCache.cs](src/web/Jordnaer/Features/Category/CategoryCache.cs)
- **Interface**: `ICategoryCache`
- **What it caches**: Application categories (global, shared across all users)
- **Current expiration**: 15 minutes absolute
- **Key**: `"Category"`
- **Service lifetime**: Scoped (in [WebApplicationBuilderExtensions.cs](src/web/Jordnaer/Features/Category/WebApplicationBuilderExtensions.cs))

#### 2. ProfileCache
- **File**: [ProfileCache.cs](src/web/Jordnaer/Features/Profile/ProfileCache.cs)
- **Interface**: `IProfileCache`
- **What it caches**: User profiles with ChildProfiles and Categories (per-user)
- **Current expiration**:
  - Profile found: 1 hour
  - Profile not found (retry): 15 seconds
  - Anonymous user: immediate expiration
- **Key pattern**: `"UserProfile:{userId}"`
- **Special behavior**:
  - Has `ProfileChanged` event that fires when cache is updated via `SetProfile()`
  - Used by `UserCircuitHandler` to react to profile changes
- **Service lifetime**: Scoped (in [WebApplicationBuilderExtensions.cs](src/web/Jordnaer/Features/Profile/WebApplicationBuilderExtensions.cs))

#### 3. ChatMessageCache
- **File**: [ChatMessageCache.cs](src/web/Jordnaer/Features/Chat/ChatMessageCache.cs)
- **Interface**: `IChatMessageCache`
- **What it caches**: Chat messages per user/chat pair
- **Current expiration**: 7 days
- **Key pattern**: `"{userId}:chatmessages:{chatId}"`
- **Special behavior**: Incremental loading - first call loads all messages, subsequent calls fetch only new messages and append
- **Service lifetime**: Scoped (in [WebApplicationBuilderExtensions.cs](src/web/Jordnaer/Features/Chat/WebApplicationBuilderExtensions.cs))

#### 4. AdProvider (NEW - add caching)
- **File**: [AdProvider.cs](src/web/Jordnaer/Features/Ad/AdProvider.cs)
- **Interface**: `IAdProvider`
- **What to cache**: Partner ads from database (line 29-42)
- **TODO comment**: Line 55 says "This is a prime use-case for caching"
- **Suggested expiration**: 5-15 minutes (ads don't change frequently)
- **Key**: `"PartnerAds"` (global, shared across all users)
- **Service lifetime**: Currently not cached at all

## Implementation Steps

### Step 1: Install FusionCache NuGet Package
Add to `src/web/Jordnaer/Jordnaer.csproj`:
```xml
<PackageReference Include="ZiggyCreatures.FusionCache" Version="2.0.0" />
```

### Step 2: Configure FusionCache in Program.cs
Replace `builder.Services.AddMemoryCache();` (line 112) with:
```csharp
builder.Services.AddFusionCache()
    .WithDefaultEntryOptions(new FusionCacheEntryOptions
    {
        Duration = TimeSpan.FromMinutes(5),

        // Fail-safe: return stale data if refresh fails
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(2),
        FailSafeThrottleDuration = TimeSpan.FromSeconds(30),

        // Eager refresh: refresh in background before expiration
        EagerRefreshThreshold = 0.9f
    });
```

### Step 3: Migrate CategoryCache
**File**: [CategoryCache.cs](src/web/Jordnaer/Features/Category/CategoryCache.cs)

Replace:
```csharp
using Microsoft.Extensions.Caching.Memory;
```
With:
```csharp
using ZiggyCreatures.Caching.Fusion;
```

Change constructor parameter from `IMemoryCache` to `IFusionCache`.

Replace the `GetOrCreateCategoriesAsync` implementation:
```csharp
public async ValueTask<List<Shared.Category>> GetOrCreateCategoriesAsync(CancellationToken cancellationToken = default) =>
    await _fusionCache.GetOrSetAsync(
        "Category",
        async (ctx, ct) =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            return await context.Categories.AsNoTracking().ToListAsync(ct);
        },
        new FusionCacheEntryOptions { Duration = TimeSpan.FromMinutes(15) },
        cancellationToken) ?? [];
```

### Step 4: Migrate ProfileCache
**File**: [ProfileCache.cs](src/web/Jordnaer/Features/Profile/ProfileCache.cs)

Replace `IMemoryCache` with `IFusionCache`.

Key considerations:
- Keep the `ProfileChanged` event mechanism
- Handle the different expiration scenarios (found vs not found vs anonymous)
- The `SetProfile` method should use `fusionCache.Set()` instead of `memoryCache.Set()`

```csharp
public async ValueTask<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default)
{
    if (currentUser.Id is null)
    {
        return null;
    }

    return await fusionCache.GetOrSetAsync(
        $"UserProfile:{currentUser.Id}",
        async (ctx, ct) =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);
            return await context.UserProfiles
                .AsNoTracking()
                .AsSingleQuery()
                .Include(userProfile => userProfile.ChildProfiles)
                .Include(userProfile => userProfile.Categories)
                .FirstOrDefaultAsync(userProfile => userProfile.Id == currentUser.Id, ct);
        },
        new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromHours(1),
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromHours(4)
        },
        cancellationToken);
}

public void SetProfile(UserProfile userProfile)
{
    if (currentUser.Id is null)
    {
        return;
    }

    fusionCache.Set(
        $"UserProfile:{currentUser.Id}",
        userProfile,
        new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

    ProfileChanged?.Invoke(this, userProfile);
}
```

### Step 5: Migrate ChatMessageCache
**File**: [ChatMessageCache.cs](src/web/Jordnaer/Features/Chat/ChatMessageCache.cs)

This one is more complex due to the incremental message loading pattern. The cache needs to:
1. On first access: Load all messages
2. On subsequent access: Fetch new messages and append to cached list

Replace `IMemoryCache` with `IFusionCache` and adapt the incremental loading logic:

```csharp
public async ValueTask<List<ChatMessageDto>> GetChatMessagesAsync(string userId, Guid chatId, CancellationToken cancellationToken = default)
{
    var key = $"{userId}:chatmessages:{chatId}";

    // Try to get existing cached messages
    var maybeValue = await fusionCache.TryGetAsync<List<ChatMessageDto>>(key, cancellationToken: cancellationToken);

    if (!maybeValue.HasValue)
    {
        // First time: load all messages
        var getChatMessagesResponse = await chatService.GetChatMessagesAsync(userId, chatId, 0, int.MaxValue, cancellationToken);
        var messages = getChatMessagesResponse.Match(m => m, _ => new List<ChatMessageDto>());

        await fusionCache.SetAsync(key, messages,
            new FusionCacheEntryOptions { Duration = TimeSpan.FromDays(7) },
            cancellationToken);

        return messages;
    }

    var cachedMessages = maybeValue.Value ?? new List<ChatMessageDto>();

    // Fetch new messages since last cached
    var newMessagesResponse = await chatService.GetChatMessagesAsync(userId, chatId, cachedMessages.Count, int.MaxValue, cancellationToken);
    var newMessages = newMessagesResponse.Match(m => m, _ => new List<ChatMessageDto>());

    if (newMessages.Count > 0)
    {
        cachedMessages.AddRange(newMessages);
        await fusionCache.SetAsync(key, cachedMessages,
            new FusionCacheEntryOptions { Duration = TimeSpan.FromDays(7) },
            cancellationToken);
    }

    return cachedMessages;
}
```

### Step 6: Add Caching to AdProvider
**File**: [AdProvider.cs](src/web/Jordnaer/Features/Ad/AdProvider.cs)

Inject `IFusionCache` and cache the partner ads query:

```csharp
public class AdProvider(
    IDbContextFactory<JordnaerDbContext> contextFactory,
    IFusionCache fusionCache,
    ILogger<AdProvider> logger) : IAdProvider
{
    public async Task<OneOf<List<AdData>, Error<string>>> GetAdsAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return new List<AdData>();
        }

        // Cache the partner ads from database
        var partnerAds = await fusionCache.GetOrSetAsync(
            "PartnerAds",
            async (ctx, ct) =>
            {
                await using var context = await contextFactory.CreateDbContextAsync(ct);
                return await context.Partners
                    .AsNoTracking()
                    .Where(p => p.CanHaveAd && p.AdImageUrl != null && p.AdImageUrl != "")
                    .Select(p => new AdData
                    {
                        Title = p.Name ?? "Partner",
                        Description = p.Description,
                        ImagePath = p.AdImageUrl!,
                        Link = p.AdLink ?? p.PartnerPageLink ?? "#",
                        PartnerId = p.Id,
                        LabelColor = p.AdLabelColor
                    })
                    .ToListAsync(ct);
            },
            new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(10),
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromHours(1)
            },
            cancellationToken);

        var allAds = new List<AdData>(partnerAds ?? []);

        // Add hardcoded ads as fallback/supplement
        allAds.AddRange(HardcodedAds.GetAll());

        if (allAds.Count is 0)
        {
            return new List<AdData>();
        }

        // Randomly select the requested count of ads
        return allAds
            .Shuffle()
            .Take(count)
            .ToList();
    }
}
```

Remove the try/catch around the database query since FusionCache's fail-safe handles failures gracefully.

## Testing

After migration, verify:
1. Categories still load correctly on pages using `ICategoryCache`
2. User profiles load and the `ProfileChanged` event still fires in `UserCircuitHandler`
3. Chat messages load incrementally (new messages append correctly)
4. Ads display correctly with caching (check that database isn't hit on every page load)

## Files Changed Summary
| File | Change Type |
|------|-------------|
| `Jordnaer.csproj` | Add FusionCache package |
| `Program.cs` | Replace `AddMemoryCache()` with `AddFusionCache()` |
| `CategoryCache.cs` | Migrate to IFusionCache |
| `ProfileCache.cs` | Migrate to IFusionCache |
| `ChatMessageCache.cs` | Migrate to IFusionCache |
| `AdProvider.cs` | Add caching with IFusionCache |

## Optional Future Enhancements
- Add Redis as a distributed cache layer for multi-instance deployments
- Configure different cache durations per cache type using named caches
- Add cache invalidation when partners update their ads in the partner dashboard

using Jordnaer.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace Jordnaer.Client.Features.Profile;

public interface IProfileCache
{
    ValueTask<UserProfile?> GetOrCreateProfileAsync();
    void SetProfile(UserProfile userProfile);
}

public class ProfileCache : IProfileCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly IProfileApiClient _profileApiClient;

    public ProfileCache(IMemoryCache memoryCache, IProfileApiClient profileApiClient)
    {
        _memoryCache = memoryCache;
        _profileApiClient = profileApiClient;
    }

    public async ValueTask<UserProfile?> GetOrCreateProfileAsync() =>
        await _memoryCache.GetOrCreateAsync(nameof(UserProfile), async entry =>
        {
            var result = await _profileApiClient.GetOwnProfile();
            if (result.IsSuccessStatusCode)
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return result.Content!;
            }

            if (entry.Value is UserProfile oldEntry)
            {
                // Set this cache entry to expire in quickly to retry early
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return oldEntry;
            }

            return null;
        });

    public void SetProfile(UserProfile userProfile) => _memoryCache.Set(nameof(UserProfile), userProfile);
}

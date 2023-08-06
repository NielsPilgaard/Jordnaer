using Microsoft.Extensions.Caching.Memory;

namespace Jordnaer.Client.Features.LookingFor;

public interface ILookingForCache
{
    Task<List<Jordnaer.Shared.LookingFor>> GetOrCreateLookingForAsync();
}

public class LookingForCache : ILookingForCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILookingForClient _lookingForApi;

    public LookingForCache(IMemoryCache memoryCache, ILookingForClient lookingForApi)
    {
        _memoryCache = memoryCache;
        _lookingForApi = lookingForApi;
    }

#pragma warning disable CS8603 // Possible null reference return.
    public async Task<List<Jordnaer.Shared.LookingFor>> GetOrCreateLookingForAsync() =>
        await _memoryCache.GetOrCreateAsync(nameof(LookingFor), async entry =>
        {
            var result = await _lookingForApi.GetLookingFor();
            if (result.IsSuccessStatusCode)
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                return result.Content!;
            }

            if (entry.Value is List<Jordnaer.Shared.LookingFor> oldEntry)
            {
                // Set this cache entry to expire in quickly to retry early
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return oldEntry;
            }

            return new List<Jordnaer.Shared.LookingFor>();
        });
#pragma warning restore CS8603 // Possible null reference return.
}

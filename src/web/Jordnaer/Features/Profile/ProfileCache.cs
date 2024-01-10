using Jordnaer.Authorization;
using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jordnaer.Features.Profile;

public interface IProfileCache
{
	ValueTask<UserProfile?> GetOrCreateProfileAsync(CancellationToken cancellationToken = default);
	void SetProfile(UserProfile userProfile);
}

public class ProfileCache : IProfileCache
{
	private readonly IMemoryCache _memoryCache;
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory;
	private readonly CurrentUser _currentUser;

	public ProfileCache(IMemoryCache memoryCache, IDbContextFactory<JordnaerDbContext> contextFactory, CurrentUser currentUser)
	{
		_memoryCache = memoryCache;
		_contextFactory = contextFactory;
		_currentUser = currentUser;
	}

	public async ValueTask<UserProfile?> GetOrCreateProfileAsync(CancellationToken cancellationToken = default) =>
		await _memoryCache.GetOrCreateAsync(nameof(UserProfile), async entry =>
		{
			await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
			var profile = await context
								.UserProfiles
								.AsNoTracking()
								.AsSingleQuery()
								.Include(userProfile => userProfile.ChildProfiles)
								.Include(userProfile => userProfile.Categories)
								.FirstOrDefaultAsync(userProfile => userProfile.Id == _currentUser.Id,
													 cancellationToken);

			if (profile is not null)
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
				return profile;
			}

			if (entry.Value is UserProfile oldEntry)
			{
				// Set this cache entry to expire in quickly to retry early
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15);
				return oldEntry;
			}

			return null;
		});

	public void SetProfile(UserProfile userProfile) => _memoryCache.Set(nameof(UserProfile), userProfile);
}

using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jordnaer.Features.Profile;

public interface IProfileCache
{
	ValueTask<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default);
	void SetProfile(UserProfile userProfile);
}

public class ProfileCache(
	IMemoryCache memoryCache,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	AuthenticationStateProvider authenticationStateProvider)
	: IProfileCache
{
	// TODO: Add CurrentUser to get userId
	public async ValueTask<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default) =>
		await memoryCache.GetOrCreateAsync(nameof(UserProfile), async entry =>
		{
			var currentUserId = await authenticationStateProvider.GetCurrentUserId();
			if (currentUserId is null)
			{
				return null;
			}

			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var profile = await context.UserProfiles
									   .AsNoTracking()
									   .AsSingleQuery()
									   .Include(userProfile => userProfile.ChildProfiles)
									   .Include(userProfile => userProfile.Categories)
									   .FirstOrDefaultAsync(userProfile => userProfile.Id == currentUserId,
															cancellationToken);

			if (profile is not null)
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
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

	public void SetProfile(UserProfile userProfile) => memoryCache.Set(nameof(UserProfile), userProfile, TimeSpan.FromHours(1));
}

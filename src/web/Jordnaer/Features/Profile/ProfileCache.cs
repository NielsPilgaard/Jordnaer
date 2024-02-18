using Jordnaer.Database;
using Jordnaer.Features.Authentication;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jordnaer.Features.Profile;

public interface IProfileCache
{
	ValueTask<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default);
	void SetProfile(UserProfile userProfile);
	event EventHandler<UserProfile> ProfileChanged;
}

public class ProfileCache(
	IMemoryCache memoryCache,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	CurrentUser currentUser)
	: IProfileCache
{
	// TODO: Add CurrentUser to get userId
	public async ValueTask<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default) =>
		await memoryCache.GetOrCreateAsync($"{nameof(UserProfile)}:{currentUser.Id}", async entry =>
		{
			if (currentUser.Id is null)
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromTicks(1);
				return null;
			}

			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var profile = await context.UserProfiles
									   .AsNoTracking()
									   .AsSingleQuery()
									   .Include(userProfile => userProfile.ChildProfiles)
									   .Include(userProfile => userProfile.Categories)
									   .FirstOrDefaultAsync(userProfile => userProfile.Id == currentUser.Id,
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

	public void SetProfile(UserProfile userProfile)
	{
		if (currentUser.Id is null)
		{
			return;
		}

		memoryCache.Set($"{nameof(UserProfile)}:{currentUser.Id}", userProfile, TimeSpan.FromHours(1));
		ProfileChanged?.Invoke(this, userProfile);
	}

	public event EventHandler<UserProfile>? ProfileChanged;
}

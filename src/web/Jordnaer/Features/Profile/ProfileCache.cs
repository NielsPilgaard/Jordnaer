using Jordnaer.Database;
using Jordnaer.Features.Authentication;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace Jordnaer.Features.Profile;

public interface IProfileCache
{
	ValueTask<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default);
	void SetProfile(UserProfile userProfile);
	event EventHandler<UserProfile> ProfileChanged;
}

public class ProfileCache(
	IFusionCache fusionCache,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	CurrentUser currentUser)
	: IProfileCache
{
	public async ValueTask<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default)
	{
		if (currentUser.Id is null)
		{
			return null;
		}

		return await fusionCache.GetOrSetAsync<UserProfile?>(
			$"{nameof(UserProfile)}:{currentUser.Id}",
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
			token: cancellationToken);
	}

	public void SetProfile(UserProfile userProfile)
	{
		if (currentUser.Id is null)
		{
			return;
		}

		fusionCache.Set(
			$"{nameof(UserProfile)}:{currentUser.Id}",
			userProfile,
			new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

		ProfileChanged?.Invoke(this, userProfile);
	}

	public event EventHandler<UserProfile>? ProfileChanged;
}

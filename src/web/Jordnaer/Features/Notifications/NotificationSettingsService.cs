using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Notifications;

public interface INotificationSettingsService
{
	Task<ChatNotificationPreference> GetChatPreferenceAsync(string userId, CancellationToken cancellationToken = default);
	Task<OneOf<Success, NotFound>> SetChatPreferenceAsync(string userId, ChatNotificationPreference preference, CancellationToken cancellationToken = default);
	Task<OneOf<Success, NotFound>> SetGroupPostPreferenceAsync(string userId, Guid groupId, bool enabled, CancellationToken cancellationToken = default);
	Task<OneOf<Success, NotFound>> SetAllGroupPostPreferencesAsync(string userId, bool enabled, CancellationToken cancellationToken = default);
	Task<List<GroupMembership>> GetGroupPreferencesAsync(string userId, CancellationToken cancellationToken = default);
	Task<GroupNotificationPreferences> GetGroupMembershipPreferencesAsync(string userId, CancellationToken cancellationToken = default);
	Task<OneOf<Success, NotFound>> SetGroupMembershipPreferencesAsync(string userId, GroupNotificationPreferences preferences, CancellationToken cancellationToken = default);
}

public record GroupNotificationPreferences
{
	public bool EmailOnGroupMembershipRequest { get; init; } = true;
	public bool EmailOnGroupInvitation { get; init; } = true;
	public bool EmailOnGroupMembershipResponse { get; init; } = true;
}

public class NotificationSettingsService(IDbContextFactory<JordnaerDbContext> contextFactory) : INotificationSettingsService
{
	public async Task<ChatNotificationPreference> GetChatPreferenceAsync(string userId, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var userProfile = await context.UserProfiles
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

		return userProfile?.ChatNotificationPreference ?? ChatNotificationPreference.FirstMessageOnly;
	}

	public async Task<OneOf<Success, NotFound>> SetChatPreferenceAsync(string userId, ChatNotificationPreference preference, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var userProfile = await context.UserProfiles
			.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

		if (userProfile is null)
		{
			return new NotFound();
		}

		userProfile.ChatNotificationPreference = preference;
		await context.SaveChangesAsync(cancellationToken);
		return new Success();
	}

	public async Task<OneOf<Success, NotFound>> SetGroupPostPreferenceAsync(string userId, Guid groupId, bool enabled, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var membership = await context.GroupMemberships
			.FirstOrDefaultAsync(x => x.UserProfileId == userId && x.GroupId == groupId, cancellationToken);

		if (membership is null)
		{
			return new NotFound();
		}

		membership.EmailOnNewPost = enabled;
		await context.SaveChangesAsync(cancellationToken);
		return new Success();
	}

	public async Task<OneOf<Success, NotFound>> SetAllGroupPostPreferencesAsync(string userId, bool enabled, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var rowsAffected = await context.GroupMemberships
			.Where(x => x.UserProfileId == userId && x.MembershipStatus == MembershipStatus.Active)
			.ExecuteUpdateAsync(setters => setters.SetProperty(m => m.EmailOnNewPost, enabled), cancellationToken);

		return rowsAffected > 0 ? new Success() : new NotFound();
	}

	public async Task<List<GroupMembership>> GetGroupPreferencesAsync(string userId, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		// Get all active memberships with group information
		return await context.GroupMemberships
			.AsNoTracking()
			.Where(x => x.UserProfileId == userId && x.MembershipStatus == MembershipStatus.Active)
			.Include(x => x.Group)
			.ToListAsync(cancellationToken);
	}

	public async Task<GroupNotificationPreferences> GetGroupMembershipPreferencesAsync(string userId, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var profile = await context.UserProfiles
			.AsNoTracking()
			.Where(x => x.Id == userId)
			.Select(x => new GroupNotificationPreferences
			{
				EmailOnGroupMembershipRequest = x.EmailOnGroupMembershipRequest,
				EmailOnGroupInvitation = x.EmailOnGroupInvitation,
				EmailOnGroupMembershipResponse = x.EmailOnGroupMembershipResponse
			})
			.FirstOrDefaultAsync(cancellationToken);

		return profile ?? new GroupNotificationPreferences();
	}

	public async Task<OneOf<Success, NotFound>> SetGroupMembershipPreferencesAsync(string userId, GroupNotificationPreferences preferences, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var userProfile = await context.UserProfiles
			.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

		if (userProfile is null)
		{
			return new NotFound();
		}

		userProfile.EmailOnGroupMembershipRequest = preferences.EmailOnGroupMembershipRequest;
		userProfile.EmailOnGroupInvitation = preferences.EmailOnGroupInvitation;
		userProfile.EmailOnGroupMembershipResponse = preferences.EmailOnGroupMembershipResponse;
		await context.SaveChangesAsync(cancellationToken);

		return new Success();
	}
}

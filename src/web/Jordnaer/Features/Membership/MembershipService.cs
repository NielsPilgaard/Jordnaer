using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Authentication;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Jordnaer.Features.Membership;

public class MembershipService(CurrentUser currentUser,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<MembershipService> logger)
{
	public async Task<OneOf<OneOf.Types.Success, OneOf.Types.Error<string>>> RequestMembership(
		Guid groupId,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		if (await context.GroupMemberships.AnyAsync(x => x.GroupId == groupId &&
														 x.UserProfileId == currentUser.Id,
											   cancellationToken))
		{
			return new OneOf.Types.Error<string>("Du er allerede medlem af denne gruppe.");
		}
	}
}
using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.GroupPosts;



public class GroupPostService(IDbContextFactory<JordnaerDbContext> contextFactory)
{
	public async Task<OneOf<GroupPostDto, NotFound>> GetPostAsync(Guid postId,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var post = await context.GroupPosts
								.AsNoTracking()
								.Where(x => x.Id == postId)
								.Select(x => x.ToGroupPostDto())
								.FirstOrDefaultAsync(cancellationToken);

		return post is null
				   ? new NotFound()
				   : post;
	}

	public async Task<List<GroupPostDto>> GetPostsAsync(Guid groupId,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var posts = await context.GroupPosts
								.AsNoTracking()
								.Where(x => x.GroupId == groupId)
								.OrderByDescending(x => x.CreatedUtc)
								.Include(x => x.UserProfile)
								.ToListAsync(cancellationToken);

		return posts.Select(x => x.ToGroupPostDto()).ToList();
	}

	public async Task<OneOf<Success, Error<string>>> CreatePostAsync(GroupPost post,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		if (await context.GroupPosts
						 .AsNoTracking()
						 .AnyAsync(x => x.Id == post.Id,
								   cancellationToken))
		{
			return new Error<string>("Opslaget eksisterer allerede.");
		}

		context.GroupPosts.Add(post);

		await context.SaveChangesAsync(cancellationToken);

		return new Success();
	}

	public async Task<OneOf<Success, Error<string>>> DeletePostAsync(Guid postId, string userId,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var rowsDeleted = await context.GroupPosts
					 .Where(x => x.Id == postId && x.UserProfileId == userId)
					 .ExecuteDeleteAsync(cancellationToken);

		return rowsDeleted > 0
			? new Success()
			: new Error<string>("Opslaget blev ikke fundet eller du har ikke rettigheder til at slette det.");
	}
}

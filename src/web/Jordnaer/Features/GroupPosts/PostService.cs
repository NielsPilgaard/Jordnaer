using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.GroupPosts;

public interface IGroupPostService
{
	Task<OneOf<PostDto, NotFound>> GetPostAsync(Guid postId,
		CancellationToken cancellationToken = default);

	Task<OneOf<Success, Error<string>>> CreatePostAsync(GroupPost post,
		CancellationToken cancellationToken = default);

	Task<OneOf<Success, Error<string>>> DeletePostAsync(Guid postId,
		CancellationToken cancellationToken = default);
}
// TODO: This is just a copy of PostService, make it Group specific
public class GroupPostService(IDbContextFactory<JordnaerDbContext> contextFactory) : IGroupPostService
{
	public async Task<OneOf<PostDto, NotFound>> GetPostAsync(Guid postId,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var post = await context.Posts
								.Where(x => x.Id == postId)
								.Select(x => x.ToPostDto())
								.FirstOrDefaultAsync(cancellationToken);

		return post is null
				   ? new NotFound()
				   : post;
	}

	public async Task<OneOf<Success, Error<string>>> CreatePostAsync(GroupPost post,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		if (await context.Posts
						 .AsNoTracking()
						 .AnyAsync(x => x.Id == post.Id &&
										x.UserProfileId == post.UserProfileId,
								   cancellationToken))
		{
			return new Error<string>("Opslaget eksisterer allerede.");
		}

		context.GroupPosts.Add(post);

		await context.SaveChangesAsync(cancellationToken);

		return new Success();
	}

	public async Task<OneOf<Success, Error<string>>> DeletePostAsync(Guid postId,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var post = await context.Posts.FindAsync([postId], cancellationToken);
		if (post is null)
		{
			return new Error<string>("Opslaget blev ikke fundet.");
		}

		await context.Posts
					 .Where(x => x.Id == postId)
					 .ExecuteDeleteAsync(cancellationToken);

		return new Success();
	}
}

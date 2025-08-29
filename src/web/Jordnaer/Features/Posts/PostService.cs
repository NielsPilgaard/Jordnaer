using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Posts;

public class PostService(IDbContextFactory<JordnaerDbContext> contextFactory)
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

	public async Task<OneOf<Success, Error<string>>> CreatePostAsync(Post post,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		if (await context.Posts
						 .AsNoTracking()
						 .AnyAsync(x => x.Id == post.Id,
								   cancellationToken))
		{
			return new Error<string>("Opslaget eksisterer allerede.");
		}

		if (string.IsNullOrWhiteSpace(post.Text))
		{
			return new Error<string>("Opslaget skal indeholde tekst.");
		}

		// Mark Categories as Modified, so they're not re-added to the Categories table
		post.Categories.ForEach(x => context.Entry(x).State = EntityState.Modified);

		context.Posts.Add(post);

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

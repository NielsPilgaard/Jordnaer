using Jordnaer.Database;
using Jordnaer.Features.Authentication;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Posts;

public class PostService(IDbContextFactory<JordnaerDbContext> contextFactory, CurrentUser currentUser)
{
	public async Task<OneOf<PostDto, NotFound>> GetPostAsync(Guid postId,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var post = await context.Posts
								.AsNoTracking()
								.Include(x => x.UserProfile)
								.Include(x => x.Categories)
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

	public async Task<OneOf<Success, Error<string>>> UpdatePostAsync(Guid postId, string userId, string text, List<Jordnaer.Shared.Category> categories,
		CancellationToken cancellationToken = default)
	{
		// Validate that the authenticated user matches the provided userId
		if (currentUser.Id != userId)
		{
			return new Error<string>("Du har ikke rettigheder til at redigere dette opslag.");
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var post = await context.Posts
								.Include(x => x.Categories)
								.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken);

		if (post is null)
		{
			return new Error<string>("Opslaget blev ikke fundet.");
		}

		if (post.UserProfileId != userId)
		{
			return new Error<string>("Du har ikke rettigheder til at redigere dette opslag.");
		}

		post.Text = text;

		// Clear existing and add new categories to properly track relationship changes
		post.Categories.Clear();
		foreach (var categoryDto in categories)
		{
			var category = await context.Categories.FindAsync([categoryDto.Id], cancellationToken);
			if (category is null)
			{
				post.Categories.Add(categoryDto);
				context.Entry(categoryDto).State = EntityState.Added;
			}
			else
			{
				post.Categories.Add(category);
			}
		}

		await context.SaveChangesAsync(cancellationToken);

		return new Success();
	}

	public async Task<OneOf<Success, Error<string>>> DeletePostAsync(Guid postId, string userId,
		CancellationToken cancellationToken = default)
	{
		// Validate that the authenticated user matches the provided userId
		if (currentUser.Id != userId)
		{
			return new Error<string>("Du har ikke rettigheder til at slette dette opslag.");
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var rowsDeleted = await context.Posts
					 .Where(x => x.Id == postId && x.UserProfileId == userId)
					 .ExecuteDeleteAsync(cancellationToken);

		return rowsDeleted > 0
			? new Success()
			: new Error<string>("Opslaget blev ikke fundet eller du har ikke rettigheder til at slette det.");
	}
}

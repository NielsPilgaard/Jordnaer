using Jordnaer.Database;
using Jordnaer.Features.Metrics;
using Jordnaer.Features.Search;
using Jordnaer.Models;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.PostSearch;

public class PostSearchService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	IZipCodeService zipCodeService,
	ILogger<PostSearchService> logger)
{
	public async Task<OneOf<PostSearchResult, Error<string>>> GetPostsAsync(PostSearchFilter filter,
		CancellationToken cancellationToken = default)
	{
		JordnaerMetrics.PostSearchesCounter.Add(1, MakeTagList(filter));

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var query = context.Posts
						   .AsNoTracking()
						   .AsQueryable();

		query = ApplyCategoryFilter(filter, query);
		query = await ApplyLocationFilterAsync(filter, query, cancellationToken);
		query = ApplyContentFilter(filter.Contents, query);

		var postsToSkip = filter.PageNumber == 1
							  ? 0
							  : (filter.PageNumber - 1) * filter.PageSize;

		try
		{
			var posts = await query.Include(x => x.UserProfile)
								   .Include(x => x.Categories)
								   .OrderByDescending(x => x.CreatedUtc)
								   .Skip(postsToSkip)
								   .Take(filter.PageSize)
								   .Select(x => x.ToPostDto())
								   .ToListAsync(cancellationToken);

			var totalCount = await query.CountAsync(cancellationToken);

			return new PostSearchResult
			{
				Posts = posts,
				TotalCount = totalCount
			};
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Exception occurred during post search. " +
									   "Query: {Query}", query.ToQueryString());
		}

		return new Error<string>(ErrorMessages.Something_Went_Wrong_Try_Again);
	}

	internal async Task<IQueryable<Post>> ApplyLocationFilterAsync(
		PostSearchFilter filter,
		IQueryable<Post> posts,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(filter.Location) || filter.WithinRadiusKilometers is null)
		{
			return posts;
		}

		var (zipCodesWithinCircle, searchedZipCode) = await zipCodeService.GetZipCodesNearLocationAsync(
														  filter.Location,
														  filter.WithinRadiusKilometers.Value,
														  cancellationToken);

		if (zipCodesWithinCircle.Count is 0 || searchedZipCode is null)
		{
			return posts;
		}

		posts = posts.Where(user => user.ZipCode != null &&
									zipCodesWithinCircle.Contains(user.ZipCode.Value));

		return posts;
	}

	internal static IQueryable<Post> ApplyCategoryFilter(PostSearchFilter filter, IQueryable<Post> posts)
	{
		if (filter.Categories is not null && filter.Categories.Length > 0)
		{
			posts = posts.Where(
				user => user.Categories.Any(category => filter.Categories.Contains(category.Name)));
		}

		return posts;
	}

	internal static IQueryable<Post> ApplyContentFilter(string? filter, IQueryable<Post> posts)
	{
		if (!string.IsNullOrWhiteSpace(filter))
		{
			posts = posts.Where(post => EF.Functions.Like(post.Text, $"%{filter}%"));
		}

		return posts;
	}

	private static ReadOnlySpan<KeyValuePair<string, object?>> MakeTagList(PostSearchFilter filter)
	{
		return new KeyValuePair<string, object?>[]
		{
			new(nameof(filter.Location), filter.Location),
			new(nameof(filter.Categories), string.Join(',', filter.Categories ?? [])),
			new(nameof(filter.WithinRadiusKilometers), filter.WithinRadiusKilometers)
		};
	}
}
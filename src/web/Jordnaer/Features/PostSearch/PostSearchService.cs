using Jordnaer.Database;
using Jordnaer.Features.Metrics;
using Jordnaer.Features.Profile;
using Jordnaer.Models;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.PostSearch;

public class PostSearchService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILocationService locationService,
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

		// Get location from the search location
		var searchLocation = await locationService.GetLocationFromZipCodeAsync(filter.Location, cancellationToken);

		if (searchLocation is null)
		{
			return posts;
		}

		var location = searchLocation.Location;
		var radiusMeters = filter.WithinRadiusKilometers.Value * 1000;

		// Use SQL Server's built-in distance calculation with geography type
		posts = posts.Where(p => p.Location != null && p.Location.IsWithinDistance(location, radiusMeters));

		return posts;
	}

	internal static IQueryable<Post> ApplyCategoryFilter(PostSearchFilter filter, IQueryable<Post> posts)
	{
		if (filter.Categories is null || filter.Categories.Length is 0)
		{
			return posts;
		}

		// This ToList prevents a LINQ translation issue on Ubuntu
		var categories = filter.Categories.ToList();

		posts = posts.Where(user => user.Categories.Any(category => categories.Contains(category.Name)));

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
using Jordnaer.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.Category;

public static class CategoryApi
{
	public static RouteGroupBuilder MapCategories(this IEndpointRouteBuilder routes)
	{
		var group = routes.MapGroup("api/categories");

		group.MapGet("", async Task<List<Shared.Category>> ([FromServices] JordnaerDbContext context) =>
			await context.Categories.AsNoTracking().ToListAsync())
			.CacheOutput(builder => builder
				.Cache()
				.Expire(TimeSpan.FromHours(1))
				.Tag(nameof(Shared.Category)));

		return group;
	}
}

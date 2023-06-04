using Jordnaer.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Features.LookingFor;

public static class LookingForApi
{
    public static RouteGroupBuilder MapLookingFor(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/looking-for");

        group.MapGet("", async Task<List<Shared.LookingFor>> ([FromServices] JordnaerDbContext context) =>
            await context.LookingFor.AsNoTracking().ToListAsync());

        return group;
    }
}

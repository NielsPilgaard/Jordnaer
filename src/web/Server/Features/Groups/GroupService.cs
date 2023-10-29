using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Features.Groups;

public class GroupService
{
    private readonly JordnaerDbContext _context;
    private readonly CurrentUser _currentUser;
    private readonly ILogger<GroupService> _logger;

    public GroupService(JordnaerDbContext context,
        CurrentUser currentUser,
        ILogger<GroupService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Results<Ok<GroupDto>, NotFound>> GetGroupByIdAsync(Guid id)
    {
        var group = await _context.Groups
            .AsNoTracking()
            .Select(group => new GroupDto
            {
                Name = group.Name,
                Description = group.Description,
                Categories = group.Categories.Select(category => category.Name).ToList(),
                Id = group.Id,
                CreatedUtc = group.CreatedUtc,
                ShortDescription = group.ShortDescription,
                City = group.City,
                ZipCode = group.ZipCode,
                MemberCount = group.Memberships.Count(membership => membership.MembershipStatus == MembershipStatus.Active)
            })
            .FirstOrDefaultAsync(e => e.Id == id);

        return group is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(group);
    }

    public async Task<CreatedAtRoute> CreateGroupAsync(Group group)
    {
        group.Memberships = new List<GroupMembership>
        {
            new()
            {
                UserProfileId = _currentUser.Id,
                GroupId = group.Id,
                OwnershipLevel = OwnershipLevel.Owner,
                MembershipStatus = MembershipStatus.Active,
                PermissionLevel = PermissionLevel.Read |
                                  PermissionLevel.Write |
                                  PermissionLevel.Moderator |
                                  PermissionLevel.Admin
            }
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        return TypedResults.CreatedAtRoute("/api/groups", new { id = group.Id });
    }

    public async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, BadRequest>> UpdateGroupAsync(Guid id, Group group)
    {
        if (id != group.Id)
        {
            return TypedResults.BadRequest();
        }

        // TODO: Perform mapping like in UserProfile

        _context.Entry(group).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, UnauthorizedHttpResult, NotFound>> DeleteGroupAsync(Guid id)
    {
        // TODO: Error logging
        var group = await _context.Groups.FindAsync(id);
        if (group is null)
        {
            return TypedResults.NotFound();
        }

        var groupOwner = await _context.GroupMemberships
            .SingleOrDefaultAsync(e => e.OwnershipLevel == OwnershipLevel.Owner);
        if (groupOwner is null)
        {
            return TypedResults.Unauthorized();
        }

        if (groupOwner.UserProfileId != _currentUser.Id)
        {
            return TypedResults.Unauthorized();
        }

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}

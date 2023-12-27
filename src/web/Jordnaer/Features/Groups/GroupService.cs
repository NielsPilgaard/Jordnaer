using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Server.Extensions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Jordnaer.Server.Features.Groups;

public interface IGroupService
{
    Task<Results<Ok<Group>, NotFound>> GetGroupByIdAsync(Guid id);
    Task<Results<Ok<GroupSlim>, NotFound>> GetSlimGroupByNameAsync(string name);
    Task<Results<NoContent, BadRequest<string>>> CreateGroupAsync(Group group);
    Task<Results<NoContent, UnauthorizedHttpResult, NotFound, BadRequest<string>>> UpdateGroupAsync(Guid id, Group group);
    Task<Results<NoContent, UnauthorizedHttpResult, NotFound>> DeleteGroupAsync(Guid id);
    Task<List<UserGroupAccess>> GetSlimGroupsForUserAsync();
}

public class GroupService : IGroupService
{
    private readonly JordnaerDbContext _context;
    private readonly CurrentUser _currentUser;
    private readonly ILogger<GroupService> _logger;
    private readonly IDiagnosticContext _diagnosticContext;

    public GroupService(JordnaerDbContext context,
        CurrentUser currentUser,
        ILogger<GroupService> logger,
        IDiagnosticContext diagnosticContext)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
        _diagnosticContext = diagnosticContext;
    }

    public async Task<Results<Ok<Group>, NotFound>> GetGroupByIdAsync(Guid id)
    {
        _logger.LogFunctionBegan();

        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(group => group.Id == id);

        return group is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(group);
    }

    public async Task<Results<Ok<GroupSlim>, NotFound>> GetSlimGroupByNameAsync(string name)
    {
        _logger.LogFunctionBegan();

        var group = await _context.Groups
            .AsNoTracking()
            .Select(x => new GroupSlim
            {
                Id = x.Id,
                Name = x.Name,
                ShortDescription = x.ShortDescription,
                ZipCode = x.ZipCode,
                City = x.City,
                ProfilePictureUrl = x.ProfilePictureUrl,
                MemberCount = x.Memberships.Count(membership => membership.MembershipStatus == MembershipStatus.Active),
                Categories = x.Categories.Select(category => category.Name).ToArray()
            })
            .FirstOrDefaultAsync(group => group.Name == name);

        return group is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(group);
    }
    public async Task<List<UserGroupAccess>> GetSlimGroupsForUserAsync()
    {
        _logger.LogFunctionBegan();

        var groups = await _context.GroupMemberships
            .AsNoTracking()
            .Where(membership => membership.UserProfileId == _currentUser.Id &&
                                 membership.MembershipStatus != MembershipStatus.Rejected)
            .Select(x => new UserGroupAccess
            {
                Group = new GroupSlim
                {
                    Id = x.Group.Id,
                    Name = x.Group.Name,
                    ShortDescription = x.Group.ShortDescription,
                    ZipCode = x.Group.ZipCode,
                    City = x.Group.City,
                    ProfilePictureUrl = x.Group.ProfilePictureUrl,
                    MemberCount =
                        x.Group.Memberships.Count(membership =>
                            membership.MembershipStatus == MembershipStatus.Active),
                    Categories = x.Group.Categories.Select(category => category.Name).ToArray()
                },
                MembershipStatus = x.MembershipStatus,
                OwnershipLevel = x.OwnershipLevel,
                PermissionLevel = x.PermissionLevel,
                LastUpdatedUtc = x.LastUpdatedUtc
            })
            .OrderByDescending(x => x.OwnershipLevel)
            .ThenByDescending(x => x.PermissionLevel)
            .ThenByDescending(x => x.LastUpdatedUtc)
            .ToListAsync();

        return groups;
    }

    public async Task<Results<NoContent, BadRequest<string>>> CreateGroupAsync(Group group)
    {
        _logger.LogFunctionBegan();

        if (await _context.Groups.AnyAsync(x => x.Name == group.Name))
        {
            return TypedResults.BadRequest($"Gruppenavnet '{group.Name}' er allerede taget.");
        }

        group.Memberships = new List<GroupMembership>
        {
            new ()
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

        var selectedCategories = group.Categories.ToArray();
        group.Categories.Clear();
        foreach (var categoryDto in selectedCategories)
        {
            var category = await _context.Categories.FindAsync(categoryDto.Id);
            if (category is null)
            {
                group.Categories.Add(categoryDto);
                _context.Entry(categoryDto).State = EntityState.Added;
            }
            else
            {
                category.LoadValuesFrom(categoryDto);
                group.Categories.Add(category);
                _context.Entry(category).State = EntityState.Modified;
            }
        }

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        _logger.LogInformation("{userIdentifier} created group '{groupName}'",
            _currentUser.User?.Email ?? _currentUser.User?.UserName, group.Name);

        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, BadRequest<string>>> UpdateGroupAsync(Guid id, Group group)
    {
        _logger.LogFunctionBegan();

        if (id != group.Id)
        {
            return TypedResults.BadRequest(string.Empty);
        }

        if (await _context.Groups.AnyAsync(x => x.Name == group.Name))
        {
            return TypedResults.BadRequest($"Gruppenavnet '{group.Name}' er allerede taget.");
        }

        var existingGroup = await _context.Groups
            .AsNoTracking()
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (existingGroup is null)
        {
            return TypedResults.NotFound();
        }

        await UpdateExistingGroupAsync(existingGroup, group, _context);
        _context.Entry(group).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, UnauthorizedHttpResult, NotFound>> DeleteGroupAsync(Guid id)
    {
        _logger.LogFunctionBegan();

        _diagnosticContext.Set("group_id", id);

        var group = await _context.Groups.FindAsync(id);
        if (group is null)
        {
            _logger.LogInformation("Failed to find group by id.");
            return TypedResults.NotFound();
        }

        _diagnosticContext.Set("group_name", group.Name);

        var groupOwner = await _context.GroupMemberships
            .SingleOrDefaultAsync(e => e.UserProfileId == _currentUser.Id &&
                                       e.OwnershipLevel == OwnershipLevel.Owner);

        if (groupOwner is null)
        {
            _logger.LogError("Failed to delete group because it has no owner.");
            return TypedResults.Unauthorized();
        }

        if (groupOwner.UserProfileId != _currentUser.Id)
        {
            _logger.LogError("Failed to delete group because the request came from someone other than the owner. " +
                             "The deletion was requested by the user: {@currentUser}", _currentUser.User);
            return TypedResults.Unauthorized();
        }

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted group");

        return TypedResults.NoContent();
    }

    private static async Task UpdateExistingGroupAsync(Group group, Group dto, JordnaerDbContext context)
    {
        group.Name = dto.Name;
        group.Address = dto.Address;
        group.City = dto.City;
        group.ZipCode = dto.ZipCode;
        group.ShortDescription = dto.ShortDescription;
        group.Description = dto.Description;

        group.Categories.Clear();
        foreach (var categoryDto in dto.Categories)
        {
            var category = await context.Categories.FindAsync(categoryDto.Id);
            if (category is null)
            {
                group.Categories.Add(categoryDto);
                context.Entry(categoryDto).State = EntityState.Added;
            }
            else
            {
                category.LoadValuesFrom(categoryDto);
                group.Categories.Add(category);
                context.Entry(category).State = EntityState.Modified;
            }
        }
    }
}

using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Jordnaer.Server.Features.Groups;

public interface IGroupService
{
    Task<Results<Ok<GroupDto>, NotFound>> GetGroupByIdAsync(Guid id);
    Task<Results<NoContent, BadRequest<string>>> CreateGroupAsync(Group group);
    Task<Results<NoContent, UnauthorizedHttpResult, NotFound, BadRequest<string>>> UpdateGroupAsync(Guid id, Group group);
    Task<Results<NoContent, UnauthorizedHttpResult, NotFound>> DeleteGroupAsync(Guid id);
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

    public async Task<Results<NoContent, BadRequest<string>>> CreateGroupAsync(Group group)
    {
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

        group.Categories.Clear();
        foreach (var categoryDto in group.Categories)
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
        _diagnosticContext.Set("groupId", id);

        var group = await _context.Groups.FindAsync(id);
        if (group is null)
        {
            _logger.LogInformation("Failed to find group by id.");
            return TypedResults.NotFound();
        }

        _diagnosticContext.Set("groupName", group.Name);

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
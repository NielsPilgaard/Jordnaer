using Jordnaer.Server.Database;
using Jordnaer.Shared;
using Jordnaer.Shared.UserSearch;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Features.Search;

public interface IUserSearchService
{
    Task<UserSearchResult> GetUsersAsync(UserSearchFilter filter, CancellationToken cancellationToken);
}

public class UserSearchService : IUserSearchService
{
    private readonly ILogger<UserSearchService> _logger;
    private readonly JordnaerDbContext _context;

    public UserSearchService(ILogger<UserSearchService> logger, JordnaerDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<UserSearchResult> GetUsersAsync(UserSearchFilter filter, CancellationToken cancellationToken)
    {
        var users = _context.UserProfiles.AsQueryable();

        users = ApplyChildFilters(filter, users);
        users = ApplyNameFilter(filter, users);
        users = ApplyLookingForFilter(filter, users);
        users = ApplyLocationFilter(filter, users);

        _logger.LogDebug("GetUsersAsync query: {query}", users.ToQueryString());

        int usersToSkip = filter.PageNumber == 1 ? 0 : (filter.PageNumber - 1) * filter.PageSize;
        var paginatedUsers = await users
            .OrderBy(user => user.CreatedUtc)
            .Skip(usersToSkip)
            .Take(filter.PageSize)
            .Include(user => user.LookingFor)
            .Include(user => user.ChildProfiles)
            .AsSingleQuery()
            .Select(user => new UserDto
            {
                ProfilePictureUrl = user.ProfilePictureUrl,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ZipCode = user.ZipCode,
                City = user.City,
                LookingFor = user.LookingFor.Select(lookingFor => lookingFor.Name).ToList(),
                Children = user.ChildProfiles.Select(child => new ChildDto
                {
                    FirstName = child.FirstName,
                    LastName = child.LastName,
                    Gender = child.Gender,
                    DateOfBirth = child.DateOfBirth
                }).ToList()
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new UserSearchResult { TotalCount = users.Count(), Users = paginatedUsers };
    }

    private static IQueryable<UserProfile> ApplyLocationFilter(UserSearchFilter filter, IQueryable<UserProfile> users)
    {
        //TODO: Implement location filter
        if (!string.IsNullOrEmpty(filter.Location))
        {
            // Query the api with the location, extract returned coordinates
            // Query the api with the coordinates and filter.Radius, extract returned zip codes
            // Fetch all users that match any of the returned zip codes
        }

        return users;
    }

    private static IQueryable<UserProfile> ApplyLookingForFilter(UserSearchFilter filter, IQueryable<UserProfile> users)
    {
        if (filter.LookingFor is not null && filter.LookingFor.Any())
        {
            users = users.Where(user =>
                user.LookingFor.Any(userLookingFor => filter.LookingFor.Contains(userLookingFor.Name)));
        }

        return users;
    }

    private static IQueryable<UserProfile> ApplyNameFilter(UserSearchFilter filter, IQueryable<UserProfile> users)
    {
        if (!string.IsNullOrEmpty(filter.Name))
        {
            users = users.Where(user => !string.IsNullOrEmpty(user.SearchableName) &&
                                        EF.Functions.Like(user.SearchableName, $"%{filter.Name}%"));
        }

        return users;
    }

    private static IQueryable<UserProfile> ApplyChildFilters(UserSearchFilter filter, IQueryable<UserProfile> users)
    {
        if (filter.ChildGender is not null)
        {
            users = users.Where(user => user.ChildProfiles.Any(child => child.Gender == filter.ChildGender));
        }

        if (filter.MinimumChildAge is not null)
        {
            var minimumDateOfBirth = DateTime.UtcNow.AddYears(-filter.MinimumChildAge.Value);
            users = users.Where(user => user.ChildProfiles.Any(child => child.DateOfBirth != null &&
                                                                        child.DateOfBirth <= minimumDateOfBirth));
        }

        if (filter.MaximumChildAge is not null)
        {
            var maximumDateOfBirth = DateTime.UtcNow.AddYears(-filter.MaximumChildAge.Value);
            users = users.Where(user => user.ChildProfiles.Any(child => child.DateOfBirth != null &&
                                                                        child.DateOfBirth >= maximumDateOfBirth));
        }

        return users;
    }
}

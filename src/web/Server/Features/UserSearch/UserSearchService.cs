using System.Globalization;
using Jordnaer.Server.Database;
using Jordnaer.Shared;
using Jordnaer.Shared.UserSearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jordnaer.Server.Features.UserSearch;

public interface IUserSearchService
{
    Task<UserSearchResult> GetUsersAsync(UserSearchFilter filter, CancellationToken cancellationToken);
}

public class UserSearchService : IUserSearchService
{
    private readonly ILogger<UserSearchService> _logger;
    private readonly JordnaerDbContext _context;
    private readonly IDataForsyningenClient _dataForsyningenClient;
    private readonly DataForsyningenOptions _options;

    public UserSearchService(
        ILogger<UserSearchService> logger,
        JordnaerDbContext context,
        IDataForsyningenClient dataForsyningenClient,
        IOptions<DataForsyningenOptions> options)
    {
        _logger = logger;
        _context = context;
        _dataForsyningenClient = dataForsyningenClient;
        _options = options.Value;
    }

    public async Task<UserSearchResult> GetUsersAsync(UserSearchFilter filter, CancellationToken cancellationToken)
    {
        var users = _context.UserProfiles.AsQueryable();

        users = ApplyChildFilters(filter, users);
        users = ApplyNameFilter(filter, users);
        users = ApplyLookingForFilter(filter, users);
        users = await ApplyLocationFilterAsync(filter, users);

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

        int totalCount = await users.AsNoTracking().CountAsync(cancellationToken);

        return new UserSearchResult { TotalCount = totalCount, Users = paginatedUsers };
    }

    private async Task<IQueryable<UserProfile>> ApplyLocationFilterAsync(UserSearchFilter filter, IQueryable<UserProfile> users)
    {
        if (string.IsNullOrEmpty(filter.Location) || filter.WithinRadiusKilometers is null)
        {
            return users;
        }

        var searchResponse = await _dataForsyningenClient.GetAddressesWithAutoComplete(filter.Location);
        if (!searchResponse.IsSuccessStatusCode)
        {
            _logger.LogError(searchResponse.Error, searchResponse.Error?.Message);
            return users;
        }

        var addressDetails = searchResponse.Content?.FirstOrDefault().Adresse;
        if (addressDetails is null)
        {
            _logger.LogError("{responseName} was null.", $"{nameof(AddressAutoCompleteResponse)}.{nameof(AddressAutoCompleteResponse.Adresse)}");
            return users;
        }

        int searchRadiusMeters = Math.Min(filter.WithinRadiusKilometers ?? 0, _options.MaxSearchRadiusKilometers) * 1000;

        var circle = Circle.FromAddress(addressDetails.Value, searchRadiusMeters);

        var zipCodeSearchResponse = await _dataForsyningenClient.GetZipCodesWithinCircle(circle.ToString());
        if (!zipCodeSearchResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get zip codes within {radius}m of the coordinates x={x}, y={y}", filter.WithinRadiusKilometers, addressDetails.Value.X, addressDetails.Value.Y);
            return users;
        }

        var zipCodesWithinCircle = zipCodeSearchResponse.Content!.Select(x => x.Nr).ToList();

        _logger.LogDebug("Returned zip codes: {@zipCodes}", zipCodesWithinCircle.Count);

        users = users.Where(user => zipCodesWithinCircle.Contains(user.ZipCode));

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
            users = users.Where(user =>
                user.ChildProfiles.Any(child => child.Gender == filter.ChildGender));
        }

        if (filter.MinimumChildAge is not null)
        {
            var minimumDateOfBirth = DateTime.UtcNow.AddYears(-filter.MinimumChildAge.Value);
            users = users.Where(user =>
                user.ChildProfiles.Any(child => child.DateOfBirth != null &&
                                                child.DateOfBirth <= minimumDateOfBirth));
        }

        if (filter.MaximumChildAge is not null)
        {
            var maximumDateOfBirth = DateTime.UtcNow.AddYears(-filter.MaximumChildAge.Value);
            users = users.Where(user =>
                user.ChildProfiles.Any(child => child.DateOfBirth != null &&
                                                child.DateOfBirth >= maximumDateOfBirth));
        }

        return users;
    }
}

public readonly record struct Circle(float X, float Y, int RadiusMeters)
{
    private static readonly NumberFormatInfo _floatNumberFormat = new() { CurrencyDecimalSeparator = "." };

    /// <summary>
    /// Required querystring format is "cirkel=11.111,11.111,10000", or "cirkel=x,y,radius"
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"{X.ToString(_floatNumberFormat)}," +
                                         $"{Y.ToString(_floatNumberFormat)}," +
                                         $"{RadiusMeters}";

    public static Circle FromAddress(Adresse address, int radiusMeters) => new(address.X, address.Y, radiusMeters);
}
using Jordnaer.Server.Database;
using Jordnaer.Server.Features.UserSearch;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jordnaer.Server.Features.GroupSearch;

public interface IGroupSearchService
{
    Task<GroupSearchResult> GetGroupsAsync(GroupSearchFilter filter, CancellationToken cancellationToken);
}

public class GroupSearchService : IGroupSearchService
{
    private readonly JordnaerDbContext _context;
    private readonly ILogger<GroupSearchService> _logger;
    private readonly IDataForsyningenClient _dataForsyningenClient;
    private readonly int _maxSearchRadiusKilometers;

    public GroupSearchService(JordnaerDbContext context,
        ILogger<GroupSearchService> logger,
        IDataForsyningenClient dataForsyningenClient,
        IOptions<DataForsyningenOptions> options)
    {
        _context = context;
        _logger = logger;
        _maxSearchRadiusKilometers = options.Value.MaxSearchRadiusKilometers;
        _dataForsyningenClient = dataForsyningenClient;
    }

    public async Task<GroupSearchResult> GetGroupsAsync(GroupSearchFilter filter, CancellationToken cancellationToken)
    {
        // TODO: Convert to Dapper
        var groups = _context.Groups
            .AsNoTracking()
            .AsQueryable()
            .ApplyNameFilter(filter.Name)
            .ApplyCategoryFilter(filter.Categories);

        (groups, bool isOrdered) = await ApplyLocationFilterAsync(groups, filter);

        if (!isOrdered)
        {
            groups = groups.OrderBy(user => user.CreatedUtc);
        }

        int groupsToSkip = filter.PageNumber == 1 ? 0 : (filter.PageNumber - 1) * filter.PageSize;
        var paginatedGroups = await groups
            .Skip(groupsToSkip)
            .Take(filter.PageSize)
            .Include(user => user.Categories)
            .AsSingleQuery()
            .Select(group => new GroupSlim
            {
                ProfilePictureUrl = group.ProfilePictureUrl,
                Name = group.Name,
                ShortDescription = group.ShortDescription,
                ZipCode = group.ZipCode,
                City = group.City,
                Categories = group.Categories.Select(category => category.Name).ToArray(),
                MemberCount = group.Memberships.Count(e => e.MembershipStatus == MembershipStatus.Active),
                Id = group.Id
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        int totalCount = await groups.AsNoTracking().CountAsync(cancellationToken);

        return new GroupSearchResult { TotalCount = totalCount, Groups = paginatedGroups };
    }

    internal async Task<(IQueryable<Group> Groups, bool AppliedOrdering)>
        ApplyLocationFilterAsync(IQueryable<Group> groups, GroupSearchFilter filter)
    {
        if (string.IsNullOrEmpty(filter.Location) || filter.WithinRadiusKilometers is null)
        {
            return (groups, false);
        }

        var searchResponse = await _dataForsyningenClient.GetAddressesWithAutoComplete(filter.Location);
        if (!searchResponse.IsSuccessStatusCode)
        {
            _logger.LogError(searchResponse.Error,
                "Exception occurred in function {functionName} " +
                "while calling external API through {externalApiFunction}. " +
                "{statusCode}: {reasonPhrase}",
                nameof(ApplyLocationFilterAsync),
                nameof(IDataForsyningenClient.GetAddressesWithAutoComplete),
                searchResponse.Error.StatusCode,
                searchResponse.Error.ReasonPhrase);
            return (groups, false);
        }

        var addressDetails = searchResponse.Content?.FirstOrDefault().Adresse;
        if (addressDetails is null)
        {
            _logger.LogError("{responseName} was null.", $"{nameof(AddressAutoCompleteResponse)}.{nameof(AddressAutoCompleteResponse.Adresse)}");
            return (groups, false);
        }

        int searchRadiusMeters = Math.Min(filter.WithinRadiusKilometers ?? 0, _maxSearchRadiusKilometers) * 1000;

        var circle = Circle.FromAddress(addressDetails.Value, searchRadiusMeters);

        var zipCodeSearchResponse = await _dataForsyningenClient.GetZipCodesWithinCircle(circle.ToString());
        if (!zipCodeSearchResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get zip codes within {radius}m of the coordinates x={x}, y={y}", filter.WithinRadiusKilometers, addressDetails.Value.X, addressDetails.Value.Y);
            return (groups, false);
        }

        int searchedZipCode = GetZipCodeFromLocation(filter.Location);

        _logger.LogDebug("ZipCode that was searched for is: {searchedZipCode}", searchedZipCode);

        var zipCodesWithinCircle = zipCodeSearchResponse.Content!
            .Select(zipCodeResponse => int.Parse(zipCodeResponse.Nr!))
            .ToList();

        _logger.LogDebug("Returned zip codes: {zipCodeCount}", zipCodesWithinCircle.Count);

        groups = groups
            .Where(group => group.ZipCode != null &&
                           zipCodesWithinCircle.Contains(group.ZipCode.Value))
            .OrderBy(group => Math.Abs(group.ZipCode!.Value - searchedZipCode));

        return (groups, true);
    }

    private static int GetZipCodeFromLocation(string location)
    {
        var span = location.AsSpan();

        int indexOfLastComma = span.LastIndexOf(',');

        // Start from last comma, move 2 to skip comma and whitespace, then take the next 4 chars
        var zipCodeSpan = span.Slice(indexOfLastComma + 2, 4);

        return int.Parse(zipCodeSpan);
    }
}

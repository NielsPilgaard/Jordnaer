using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Jordnaer.Features.UserSearch;

public interface IUserSearchService
{
	Task<UserSearchResult> GetUsersAsync(UserSearchFilter filter, CancellationToken cancellationToken = default);
	Task<List<UserSlim>> GetUsersByNameAsync(string currentUserId, string searchString, CancellationToken cancellationToken = default);
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

	public async Task<List<UserSlim>> GetUsersByNameAsync(string currentUserId, string searchString, CancellationToken cancellationToken)
	{
		var users = ApplyNameFilter(searchString, _context.UserProfiles);

		var firstTenUsers = await users
			.Where(user => user.Id != currentUserId)
			.OrderBy(user => searchString.StartsWith(searchString))
			.Take(11)
			.Select(user => new UserSlim
			{
				ProfilePictureUrl = user.ProfilePictureUrl,
				DisplayName = $"{user.FirstName} {user.LastName}",
				Id = user.Id,
				UserName = user.UserName
			})
		   .AsNoTracking()
		   .ToListAsync(cancellationToken);

		return firstTenUsers;
	}

	public async Task<UserSearchResult> GetUsersAsync(UserSearchFilter filter, CancellationToken cancellationToken)
	{
		var users = _context.UserProfiles.AsQueryable();

		users = ApplyChildFilters(filter, users);
		users = ApplyNameFilter(filter.Name, users);
		users = ApplyCategoryFilter(filter, users);
		(users, var isOrdered) = await ApplyLocationFilterAsync(filter, users);

		if (!isOrdered)
		{
			users = users.OrderBy(user => user.CreatedUtc);
		}

		// TODO: This uses a ton of memory, Dapper? (60+mb)
		var usersToSkip = filter.PageNumber == 1 ? 0 : (filter.PageNumber - 1) * filter.PageSize;
		var paginatedUsers = await users
			.Skip(usersToSkip)
			.Take(filter.PageSize)
			.Include(user => user.Categories)
			.Include(user => user.ChildProfiles)
			.AsSplitQuery()
			.Select(user => new UserDto
			{
				ProfilePictureUrl = user.ProfilePictureUrl,
				UserName = user.UserName,
				FirstName = user.FirstName,
				LastName = user.LastName,
				ZipCode = user.ZipCode,
				City = user.City,
				Categories = user.Categories.Select(category => category.Name).ToList(),
				Children = user.ChildProfiles.Select(child => new ChildDto
				{
					FirstName = child.FirstName,
					LastName = child.LastName,
					Gender = child.Gender,
					DateOfBirth = child.DateOfBirth,
					Age = child.Age
				}).ToList()
			})
			.AsNoTracking()
			.ToListAsync(cancellationToken);

		var totalCount = await users.AsNoTracking().CountAsync(cancellationToken);

		return new UserSearchResult { TotalCount = totalCount, Users = paginatedUsers };
	}

	private async Task<(IQueryable<UserProfile> UserProfiles, bool AppliedOrdering)> ApplyLocationFilterAsync(
		UserSearchFilter filter,
		IQueryable<UserProfile> users)
	{
		if (string.IsNullOrEmpty(filter.Location) || filter.WithinRadiusKilometers is null)
		{
			return (users, false);
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
			return (users, false);
		}

		var addressDetails = searchResponse.Content?.FirstOrDefault().Adresse;
		if (addressDetails is null)
		{
			_logger.LogError("{responseName} was null.", $"{nameof(AddressAutoCompleteResponse)}.{nameof(AddressAutoCompleteResponse.Adresse)}");
			return (users, false);
		}

		var searchRadiusMeters = Math.Min(filter.WithinRadiusKilometers ?? 0, _options.MaxSearchRadiusKilometers) * 1000;

		var circle = Circle.FromAddress(addressDetails.Value, searchRadiusMeters);

		var zipCodeSearchResponse = await _dataForsyningenClient.GetZipCodesWithinCircle(circle.ToString());
		if (!zipCodeSearchResponse.IsSuccessStatusCode)
		{
			_logger.LogError("Failed to get zip codes within {radius}m of the coordinates x={x}, y={y}", filter.WithinRadiusKilometers, addressDetails.Value.X, addressDetails.Value.Y);
			return (users, false);
		}

		var searchedZipCode = GetZipCodeFromLocation(filter.Location);

		_logger.LogDebug("ZipCode that was searched for is: {searchedZipCode}", searchedZipCode);

		var zipCodesWithinCircle = zipCodeSearchResponse.Content!
			.Select(zipCodeResponse => int.Parse(zipCodeResponse.Nr!))
			.ToList();

		_logger.LogDebug("Returned zip codes: {zipCodeCount}", zipCodesWithinCircle.Count);

		users = users
			.Where(user => user.ZipCode != null &&
						   zipCodesWithinCircle.Contains(user.ZipCode.Value))
			.OrderBy(user => Math.Abs(user.ZipCode!.Value - searchedZipCode));

		return (users, true);
	}

	private static int GetZipCodeFromLocation(string location)
	{
		var span = location.AsSpan();

		var indexOfLastComma = span.LastIndexOf(',');

		// Start from last comma, move 2 to skip comma and whitespace, then take the next 4 chars
		var zipCodeSpan = span.Slice(indexOfLastComma + 2, 4);

		return int.Parse(zipCodeSpan);
	}

	private static IQueryable<UserProfile> ApplyCategoryFilter(UserSearchFilter filter, IQueryable<UserProfile> users)
	{
		if (filter.Categories is not null && filter.Categories.Length > 0)
		{
			users = users.Where(user =>
				user.Categories.Any(category => filter.Categories.Contains(category.Name)));
		}

		return users;
	}

	private static IQueryable<UserProfile> ApplyNameFilter(string? filter, IQueryable<UserProfile> users)
	{
		if (!string.IsNullOrWhiteSpace(filter))
		{
			users = users.Where(user => !string.IsNullOrEmpty(user.SearchableName) &&
										EF.Functions.Like(user.SearchableName, $"%{filter}%"));
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

		if (filter is { MinimumChildAge: not null, MaximumChildAge: not null } &&
			filter.MinimumChildAge == filter.MaximumChildAge)
		{
			users = users.Where(user =>
				user.ChildProfiles.Any(child => child.Age != null &&
												child.Age == filter.MinimumChildAge));
			return users;
		}

		if (filter.MinimumChildAge is not null)
		{
			users = users.Where(user =>
				user.ChildProfiles.Any(child => child.Age != null &&
												child.Age >= filter.MinimumChildAge));
		}

		if (filter.MaximumChildAge is not null)
		{
			users = users.Where(user =>
				user.ChildProfiles.Any(child => child.Age != null &&
												child.Age <= filter.MaximumChildAge));
		}

		return users;
	}
}

public readonly record struct Circle(float X, float Y, int RadiusMeters)
{
	private static readonly NumberFormatInfo FloatNumberFormat = new() { CurrencyDecimalSeparator = "." };

	/// <summary>
	/// Required querystring format is "cirkel=11.111,11.111,10000", or "cirkel=x,y,radius"
	/// </summary>
	/// <returns></returns>
	public override string ToString() => $"{X.ToString(FloatNumberFormat)}," +
										 $"{Y.ToString(FloatNumberFormat)}," +
										 $"{RadiusMeters}";

	public static Circle FromAddress(Adresse address, int radiusMeters) => new(address.X, address.Y, radiusMeters);
}

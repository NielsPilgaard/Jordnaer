using BenchmarkDotNet.Attributes;
using Jordnaer.Database;
using Jordnaer.Features.UserSearch;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jordnaer.Benchmarks;

[MemoryDiagnoser]
public class UserSearchBenchmark
{
	private IUserSearchService _service = null!;
	private JordnaerDbContext _context = null!;
	private UserProfile _randomUser = null!;
	private List<Category> _categories = [];

	[GlobalSetup]
	public async Task GlobalSetupAsync()
	{
		var factory = new BenchmarkWebApplicationFactory();
		factory.ClientOptions.AllowAutoRedirect = false;

		const int userCount = 10000;

		using var scope = factory.Services.CreateScope();

		_context = scope.ServiceProvider.GetRequiredService<JordnaerDbContext>();

		await _context.Database.MigrateAsync();

		_categories = await _context.InsertCategoriesAsync();

		await _context.InsertFakeUsersAsync(_categories, userCount);

		await _context.SaveChangesAsync();

		_randomUser = (await _context.UserProfiles
			.OrderBy(x => x.Id)
			.Skip(Random.Shared.Next(userCount))
			.Take(1)
			.FirstOrDefaultAsync())!;

		_service = scope.ServiceProvider.GetRequiredService<IUserSearchService>();
	}

	[Benchmark]
	public async Task UserSearch_No_Filter() =>
		await _service.GetUsersAsync(new UserSearchFilter());

	[Benchmark]
	public async Task UserSearch_Filter_By_FirstName() =>
		await _service.GetUsersAsync(new UserSearchFilter { Name = _randomUser.FirstName });

	[Benchmark]
	public async Task UserSearch_Filter_By_LastName() =>
		await _service.GetUsersAsync(new UserSearchFilter { Name = _randomUser.LastName });

	[Benchmark]
	public async Task UserSearch_Filter_By_UserName() =>
		await _service.GetUsersAsync(new UserSearchFilter { Name = _randomUser.UserName });

	[Benchmark]
	public async Task UserSearch_Filter_By_Age_Of_Children()
	{
		var minimumAge = Random.Shared.Next(0, 14);
		var maximumAge = Random.Shared.Next(minimumAge, 14);
		await _service.GetUsersAsync(new UserSearchFilter { MinimumChildAge = minimumAge, MaximumChildAge = maximumAge });
	}

	[Benchmark]
	public async Task UserSearch_Filter_By_Category() =>
		await _service.GetUsersAsync(new UserSearchFilter
		{
			Categories = _categories
						 .Select(category => category.Name)
						 .Skip(Random.Shared.Next(0, _categories.Count))
						 .ToArray()
		});

	[Benchmark]
	public async Task UserSearch_Filter_By_Gender_Of_Children()
	{
		var genders = Enum.GetValues<Gender>();
		await _service.GetUsersAsync(new UserSearchFilter { ChildGender = genders[Random.Shared.Next(0, genders.Length)] });
	}

	[Benchmark]
	public async Task UserSearch_Filter_By_Address_Within_20_Kilometers() =>
		await _service.GetUsersAsync(new UserSearchFilter
		{
			Location = $"{_randomUser.Address}, {_randomUser.ZipCode} {_randomUser.City}",
			WithinRadiusKilometers = 20
		});
}

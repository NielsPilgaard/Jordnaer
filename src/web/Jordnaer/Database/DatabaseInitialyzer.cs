using Bogus;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Database;

public static class SeedDatabase
{
	public static async Task InitializeDatabaseAsync(this WebApplication app)
	{
		await using var scope = app.Services.CreateAsyncScope();

		await using var context = await scope.ServiceProvider
											 .GetRequiredService<IDbContextFactory<JordnaerDbContext>>()
											 .CreateDbContextAsync();

		await context.Database.MigrateAsync();

		var categories = await context.InsertCategoriesAsync();

		await context.InsertFakeUsersAsync(categories);

		await context.SaveChangesAsync();
	}

	public static async Task InsertFakeUsersAsync(
		this JordnaerDbContext context,
		List<Category> categories,
		int usersToGenerate = 500)
	{
		if (await context.UserProfiles.AnyAsync())
		{
			return;
		}

		// Danish locale is not available, nb_NO is norwegian. Close enough!
		var childProfileFaker = new Faker<ChildProfile>("nb_NO")
			.RuleFor(cp => cp.Id, _ => NewId.NextGuid())
			.RuleFor(cp => cp.FirstName, f => f.Name.FirstName())
			.RuleFor(cp => cp.LastName, f => f.Name.LastName())
			.RuleFor(cp => cp.Gender, f => f.PickRandom<Gender>())
			.RuleFor(cp => cp.DateOfBirth, f => f.Date.Between(DateTime.UtcNow.AddYears(-14), DateTime.UtcNow))
			.RuleFor(cp => cp.Description, f => f.Lorem.Paragraphs(f.Random.Int(1, 5)))
			.RuleFor(cp => cp.PictureUrl, f => f.Internet.Avatar());

		var userFaker = new Faker<UserProfile>("nb_NO")
			.RuleFor(u => u.Id, _ => NewId.NextGuid().ToString())
			.RuleFor(u => u.UserName, f => f.Internet.UserName())
			.RuleFor(u => u.FirstName, f => f.Name.FirstName())
			.RuleFor(u => u.LastName, f => f.Name.LastName())
			.RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
			.RuleFor(u => u.Address, f => f.Address.StreetAddress())
			.RuleFor(u => u.ZipCode, f => f.Random.Int(1000, 9991))
			.RuleFor(u => u.City, f => f.Address.City())
			.RuleFor(u => u.Description, f => f.Lorem.Paragraphs(f.Random.Int(1, 5)))
			.RuleFor(u => u.Categories,
				f => categories
					.OrderBy(_ => f.Random.Int(0, categories.Count))
					.Take(f.Random.Int(0, categories.Count))
					.ToList())
			.RuleFor(u => u.ChildProfiles, f => childProfileFaker.Generate(f.Random.Int(1, 3)))
			.RuleFor(u => u.DateOfBirth, f => f.Date.Between(DateTime.UtcNow.AddYears(-70), DateTime.UtcNow.AddYears(-16)))
			.RuleFor(u => u.ProfilePictureUrl, f => f.Internet.Avatar());

		Console.WriteLine("Generated {0} UserProfiles for testing.", usersToGenerate);

		var users = userFaker.Generate(usersToGenerate);

		context.AddRange(users);
	}

	public static async Task<List<Category>> InsertCategoriesAsync(this JordnaerDbContext context)
	{
		if (await context.Categories.AnyAsync())
		{
			return [];
		}

		var categories = new List<Category>
		{
			new() {Name = "Legeaftale"},
			new() {Name = "Hjemmeskole"},
			new() {Name = "Privat Dagpleje"},
			new() {Name = "Forældregruppe"},
			new() {Name = "Sportsaktiviteter"},
			new() {Name = "Andet"}
		};

		context.Categories.AddRange(categories);

		return categories;
	}
}

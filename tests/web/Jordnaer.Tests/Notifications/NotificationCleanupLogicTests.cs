using Bogus;
using FluentAssertions;
using Jordnaer.Database;
using Jordnaer.Features.Notifications;
using Jordnaer.Shared;
using Jordnaer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Jordnaer.Tests.Notifications;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(SqlServerContainerCollection))]
public class NotificationCleanupLogicTests : IAsyncLifetime
{
	private readonly SqlServerContainer<JordnaerDbContext> _sqlServerContainer;
	private readonly JordnaerDbContext _context;
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory = Substitute.For<IDbContextFactory<JordnaerDbContext>>();
	private readonly NotificationCleanupService _sut;

	private readonly string _userProfileId = Guid.NewGuid().ToString();

	private readonly Faker<Notification> _notificationFaker;

	public NotificationCleanupLogicTests(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
	{
		_sqlServerContainer = sqlServerContainer;
		_context = _sqlServerContainer.CreateContext();

		_contextFactory.CreateDbContextAsync().ReturnsForAnyArgs(_ => _sqlServerContainer.CreateContext());

		_sut = new NotificationCleanupService(_contextFactory);

		_notificationFaker = new Faker<Notification>("nb_NO")
			.RuleFor(n => n.Id, _ => Guid.NewGuid())
			.RuleFor(n => n.RecipientId, _ => _userProfileId)
			.RuleFor(n => n.Title, f => f.Lorem.Sentence(3))
			.RuleFor(n => n.Type, f => f.PickRandom<NotificationType>())
			.RuleFor(n => n.CreatedUtc, _ => DateTime.UtcNow);
	}

	[Fact]
	public async Task PurgeOldNotificationsAsync_DeletesNotificationsOlderThanRetentionDays()
	{
		// Arrange
		AddUserProfile();

		var oldNotification = _notificationFaker.Generate();
		oldNotification.CreatedUtc = DateTime.UtcNow.AddDays(-200);

		_context.Notifications.Add(oldNotification);
		await _context.SaveChangesAsync();

		// Act
		var deletedCount = await _sut.PurgeOldNotificationsAsync(retentionDays: 180);

		// Assert
		deletedCount.Should().Be(1);

		await using var verifyContext = _sqlServerContainer.CreateContext();
		var remaining = await verifyContext.Notifications.CountAsync();
		remaining.Should().Be(0);
	}

	[Fact]
	public async Task PurgeOldNotificationsAsync_DoesNotDeleteNotificationsWithinRetentionPeriod()
	{
		// Arrange
		AddUserProfile();

		var recentNotification = _notificationFaker.Generate();
		recentNotification.CreatedUtc = DateTime.UtcNow.AddDays(-10);

		_context.Notifications.Add(recentNotification);
		await _context.SaveChangesAsync();

		// Act
		var deletedCount = await _sut.PurgeOldNotificationsAsync(retentionDays: 180);

		// Assert
		deletedCount.Should().Be(0);

		await using var verifyContext = _sqlServerContainer.CreateContext();
		var remaining = await verifyContext.Notifications.CountAsync();
		remaining.Should().Be(1);
	}

	[Fact]
	public async Task PurgeOldNotificationsAsync_ReturnsZero_WhenNoNotificationsExist()
	{
		// Act
		var deletedCount = await _sut.PurgeOldNotificationsAsync(retentionDays: 180);

		// Assert
		deletedCount.Should().Be(0);
	}

	[Fact]
	public async Task PurgeOldNotificationsAsync_OnlyDeletesExpiredNotifications_WhenMixedAgesExist()
	{
		// Arrange
		AddUserProfile();

		var oldNotifications = _notificationFaker.Generate(3);
		foreach (var n in oldNotifications)
		{
			n.CreatedUtc = DateTime.UtcNow.AddDays(-200);
		}

		var recentNotifications = _notificationFaker.Generate(2);
		foreach (var n in recentNotifications)
		{
			n.CreatedUtc = DateTime.UtcNow.AddDays(-30);
		}

		_context.Notifications.AddRange(oldNotifications);
		_context.Notifications.AddRange(recentNotifications);
		await _context.SaveChangesAsync();

		// Act
		var deletedCount = await _sut.PurgeOldNotificationsAsync(retentionDays: 180);

		// Assert
		deletedCount.Should().Be(3);

		await using var verifyContext = _sqlServerContainer.CreateContext();
		var remaining = await verifyContext.Notifications.CountAsync();
		remaining.Should().Be(2);
	}

	[Fact]
	public async Task PurgeOldNotificationsAsync_RespectsCustomRetentionDays()
	{
		// Arrange
		AddUserProfile();

		var notification = _notificationFaker.Generate();
		notification.CreatedUtc = DateTime.UtcNow.AddDays(-10);

		_context.Notifications.Add(notification);
		await _context.SaveChangesAsync();

		// Act - with a short retention of 8 days, the 10-day-old notification should be deleted
		var deletedCount = await _sut.PurgeOldNotificationsAsync(retentionDays: 8);

		// Assert
		deletedCount.Should().Be(1);
	}

	private void AddUserProfile() =>
		_context.UserProfiles.Add(new UserProfile { Id = _userProfileId });

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		await using var context = _sqlServerContainer.CreateContext();
		await context.Notifications.ExecuteDeleteAsync();
		await context.UserProfiles.Where(u => u.Id == _userProfileId).ExecuteDeleteAsync();
		await _context.DisposeAsync();
	}
}

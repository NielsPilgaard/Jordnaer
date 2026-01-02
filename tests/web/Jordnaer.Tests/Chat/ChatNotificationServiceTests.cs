using FluentAssertions;
using Jordnaer.Consumers;
using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Chat;
using Jordnaer.Features.Email;
using Jordnaer.Shared;
using Jordnaer.Tests.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using Xunit;

namespace Jordnaer.Tests.Chat;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(SqlServerContainerCollection))]
public class ChatNotificationServiceTests : IAsyncLifetime
{
	private readonly JordnaerDbContext _context;
	private readonly Mock<IPublishEndpoint> _publishEndpointMock;
	private readonly IOptions<AppOptions> _appOptions;
	private readonly ChatNotificationService _service;
	private readonly SqlServerContainer<JordnaerDbContext> _sqlServerContainer;

	public ChatNotificationServiceTests(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
	{
		_sqlServerContainer = sqlServerContainer;
		_context = _sqlServerContainer.CreateContext();

		var contextFactoryMock = new Mock<IDbContextFactory<JordnaerDbContext>>();
		contextFactoryMock
			.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => _sqlServerContainer.CreateContext());

		_publishEndpointMock = new Mock<IPublishEndpoint>();

		_appOptions = Substitute.For<IOptions<AppOptions>>();
		_appOptions.Value.Returns(new AppOptions { BaseUrl = "http://localhost:5000" });

		_service = new ChatNotificationService(
			contextFactoryMock.Object,
			new NullLogger<StartChatConsumer>(),
			_publishEndpointMock.Object,
			_appOptions
		);
	}

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		await _context.DisposeAsync();
	}

	[Fact]
	public async Task NotifyRecipients_ShouldFetchRecipientsAndPublishEmails()
	{
		// Arrange
		var initiatorId = Guid.NewGuid().ToString();
		var recipientId1 = Guid.NewGuid().ToString();
		var recipientId2 = Guid.NewGuid().ToString();

		// Add users to database
		var users = new List<ApplicationUser>
		{
			new()
			{
				Id = initiatorId,
				Email = "initiator@example.com",
				UserName = "initiator"
			},
			new()
			{
				Id = recipientId1,
				Email = "recipient-1@example.com",
				UserName = "recipient1"
			},
			new()
			{
				Id = recipientId2,
				Email = "recipient-2@example.com",
				UserName = "recipient2"
			}
		};

		var userProfiles = new List<UserProfile>
		{
			new()
			{
				Id = initiatorId,
				ChatNotificationPreference = ChatNotificationPreference.AllMessages
			},
			new()
			{
				Id = recipientId1,
				ChatNotificationPreference = ChatNotificationPreference.FirstMessageOnly
			},
			new()
			{
				Id = recipientId2,
				ChatNotificationPreference = ChatNotificationPreference.AllMessages
			}
		};

		_context.Users.AddRange(users);
		_context.UserProfiles.AddRange(userProfiles);
		await _context.SaveChangesAsync();

		var startChat = new StartChat
		{
			Id = Guid.NewGuid(),
			InitiatorId = initiatorId,
			Recipients =
			[
				new UserSlim
				{
					Id = initiatorId,
					DisplayName = "Initiator",
					ProfilePictureUrl = null,
					UserName = null
				},
				new UserSlim
				{
					Id = recipientId1,
					DisplayName = "Recipient-1",
					ProfilePictureUrl = null,
					UserName = null
				},
				new UserSlim
				{
					Id = recipientId2,
					DisplayName = "Recipient-2",
					ProfilePictureUrl = null,
					UserName = null
				}
			]
		};

		// Act
		var act = async () => await _service.NotifyRecipients(startChat);

		// Assert
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public void CreateEmails_ShouldGenerateCorrectEmailContent()
	{
		// Arrange
		var startChat = new StartChat
		{
			Id = Guid.NewGuid(),
			InitiatorId = "initiator-id",
			Recipients =
			[
				new UserSlim
				{
					Id = "initiator-id",
					DisplayName = "Initiator",
					ProfilePictureUrl = null,
					UserName = null
				},
				new UserSlim
				{
					Id = "recipient-id",
					DisplayName = "Recipient",
					ProfilePictureUrl = null,
					UserName = null
				}
			]
		};

		var recipients = new Dictionary<string, string>
						{
							{ "initiator-id", "initiator@example.com" },
							{ "recipient-id", "recipient@example.com" }
						};

		var initiatorDisplayName = startChat.Recipients.First(x => x.Id == "initiator-id").DisplayName;

		// Act
		var emails = _service.CreateEmails(startChat, recipients).ToList();

		// Assert
		emails.Should().HaveCount(1);
		emails[0].To.Should().HaveCount(1);
		emails[0].To!.First().Email.Should().Be("recipient@example.com");
		emails[0].Subject.Should().Be($"Ny besked fra {initiatorDisplayName}");
	}

	[Fact]
	public void GetChatLink_ShouldReturnCorrectLink()
	{
		// Arrange
		var chatId = Guid.NewGuid();

		// Act
		var link = _service.GetChatLink(chatId);

		// Assert
		link.Should().Be($"http://localhost:5000/chat/{chatId}");
	}

	[Fact]
	public async Task NotifyRecipients_ShouldNotPublishEmails_WhenNoRecipients()
	{
		// Arrange
		var initiatorId = Guid.NewGuid().ToString();

		var startChat = new StartChat
		{
			Id = Guid.NewGuid(),
			InitiatorId = initiatorId,
			Recipients = [new UserSlim
				{
					Id = initiatorId,
					DisplayName = "initiator",
					ProfilePictureUrl = null,
					UserName = null
				}
			]
		};

		// Don't add any users or profiles to the database - simulating no recipients wanting notifications

		// Act
		var act = async () => await _service.NotifyRecipients(startChat);

		// Assert - should not throw when there are no recipients
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public void CreateEmails_ShouldThrow_WhenNoRecipients()
	{
		// Arrange
		var startChat = new StartChat
		{
			Id = Guid.NewGuid(),
			InitiatorId = "initiator-id",
			Recipients = []
		};

		var recipients = new Dictionary<string, string>();

		// Act
		var createEmailsAction = () => _service.CreateEmails(startChat, recipients).ToList();

		// Assert
		createEmailsAction.Should()
						  .Throw<InvalidOperationException>(
							  "We cannot create emails without sender/receiver");
	}

	[Fact]
	public void GetChatLink_ShouldReturnDefault_WhenServerAddressNotAvailable()
	{
		// Arrange
		var chatId = Guid.NewGuid();
		var nullAppOptions = Substitute.For<IOptions<AppOptions>>();
		nullAppOptions.Value.Returns(new AppOptions { BaseUrl = null });
		var serviceWithNullBaseUrl = new ChatNotificationService(
			Mock.Of<IDbContextFactory<JordnaerDbContext>>(),
			new NullLogger<StartChatConsumer>(),
			Mock.Of<IPublishEndpoint>(),
			nullAppOptions
		);
		var defaultLink = $"https://mini-moeder.dk/chat/{chatId}";

		// Act
		var link = serviceWithNullBaseUrl.GetChatLink(chatId);

		// Assert
		link.Should().Be(defaultLink);
	}
}

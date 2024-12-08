using FluentAssertions;
using Jordnaer.Consumers;
using Jordnaer.Database;
using Jordnaer.Features.Chat;
using Jordnaer.Features.Email;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Jordnaer.Tests.Chat;

[Trait("Category", "UnitTest")]
public class ChatNotificationServiceTests
{
	private readonly Mock<JordnaerDbContext> _contextMock;
	private readonly Mock<IPublishEndpoint> _publishEndpointMock;
	private readonly IServer _serverMock;
	private readonly ChatNotificationService _service;

	public ChatNotificationServiceTests()
	{
		_contextMock = new Mock<JordnaerDbContext>(
			new DbContextOptionsBuilder<JordnaerDbContext>().Options);
		
		var contextFactoryMock = new Mock<IDbContextFactory<JordnaerDbContext>>();
		contextFactoryMock
			.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(_contextMock.Object);

		_publishEndpointMock = new Mock<IPublishEndpoint>();
		_serverMock = Substitute.For<IServer>();

		_service = new ChatNotificationService(
			contextFactoryMock.Object,
			new NullLogger<StartChatConsumer>(),
			_publishEndpointMock.Object,
			_serverMock
		);
	}

	[Fact]
	public async Task NotifyRecipients_ShouldFetchRecipientsAndPublishEmails()
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
									Id = "recipient-id-1",
									DisplayName = "Recipient-1",
									ProfilePictureUrl = null,
									UserName = null
								},
								new UserSlim
								{
									Id = "recipient-id-2",
									DisplayName = "Recipient-2",
									ProfilePictureUrl = null,
									UserName = null
								}
			]
		};

		var users = new List<ApplicationUser>
						{
							new()
							{
								Id = "initiator-id",
								Email = "initiator@example.com"
							},
							new()
							{
								Id = "recipient-id-1",
								Email = "recipient-1@example.com"
							},
							new()
							{
								Id = "recipient-id-2",
								Email = "recipient-2@example.com"
							}
						};

		_contextMock.Setup(c => c.Users).ReturnsDbSet(users);

		// Act
		await _service.NotifyRecipients(startChat);

		// Assert
		_publishEndpointMock
			.Verify(p => p.Publish(It.IsAny<SendEmail>(), It.IsAny<CancellationToken>()),
					Times.Exactly(users.Count - 1)); // chat participants excluding the initiator
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

		var recipients = new Dictionary<string, string?>
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
		var serverAddressesFeatureMock = Substitute.For<IServerAddressesFeature>();
		serverAddressesFeatureMock.Addresses.Returns(new List<string> { "http://localhost:5000" });
		_serverMock.Features.Get<IServerAddressesFeature>().Returns(serverAddressesFeatureMock);

		// Act
		var link = _service.GetChatLink(chatId);

		// Assert
		link.Should().Be($"http://localhost:5000/chat/{chatId}");
	}

	[Fact]
	public async Task NotifyRecipients_ShouldNotPublishEmails_WhenNoRecipients()
	{
		// Arrange
		var startChat = new StartChat
		{
			Id = Guid.NewGuid(),
			InitiatorId = "initiator-id",
			Recipients = [new UserSlim
				{
					Id = "initiator-id",
					DisplayName = "initiator",
					ProfilePictureUrl = null,
					UserName = null
				}
			]
		};

		_contextMock.Setup(c => c.Users).ReturnsDbSet([]);

		// Act
		await _service.NotifyRecipients(startChat);

		// Assert
		_publishEndpointMock
			.Verify(p => p.Publish(It.IsAny<SendEmail>(),
								   It.IsAny<CancellationToken>()),
					Times.Never);
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

		var recipients = new Dictionary<string, string?>();

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
		_serverMock.Features.Get<IServerAddressesFeature>().Returns((IServerAddressesFeature?)null);
		var defaultLink = $"https://mini-moeder.dk/chat/{chatId}";

		// Act
		var link = _service.GetChatLink(chatId);

		// Assert
		link.Should().Be(defaultLink);
	}
}

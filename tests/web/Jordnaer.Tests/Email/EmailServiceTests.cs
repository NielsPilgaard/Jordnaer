using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Email;
using Jordnaer.Shared;
using Jordnaer.Tests.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Jordnaer.Tests.Email;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(SqlServerContainerCollection))]
public class EmailServiceTests : IAsyncLifetime
{
	private readonly JordnaerDbContext _context;
	private readonly Mock<IPublishEndpoint> _publishEndpointMock;
	private readonly EmailService _service;
	private readonly string _testUserId = $"test-user-email-{Guid.NewGuid()}";

	public EmailServiceTests(SqlServerContainer<JordnaerDbContext> sqlServerContainer)
	{
		_context = sqlServerContainer.CreateContext();

		var contextFactoryMock = new Mock<IDbContextFactory<JordnaerDbContext>>();
		contextFactoryMock
			.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => sqlServerContainer.CreateContext());

		_publishEndpointMock = new Mock<IPublishEndpoint>();

		var appOptions = Options.Create(new AppOptions { BaseUrl = "http://localhost:5000" });

		_service = new EmailService(
			_publishEndpointMock.Object,
			contextFactoryMock.Object,
			new NullLogger<EmailService>(),
			appOptions
		);
	}

	public async Task InitializeAsync()
	{
		// Create an ApplicationUser that Partner entities can reference
		var existingUser = await _context.Users.FindAsync(_testUserId);
		if (existingUser == null)
		{
			var user = new ApplicationUser
			{
				Id = _testUserId,
				UserName = "test@example.com",
				Email = "test@example.com",
				EmailConfirmed = true
			};
			_context.Users.Add(user);
			await _context.SaveChangesAsync();
		}
	}

	public async Task DisposeAsync()
	{
		await _context.DisposeAsync();
	}

	#region SendEmailFromContactForm Tests

	[Fact]
	public async Task SendEmailFromContactForm_ShouldPublishEmailWithNameInSubject_WhenNameIsProvided()
	{
		// Arrange
		var contactForm = new ContactForm
		{
			Name = "John Doe",
			Email = "john.doe@example.com",
			Message = "Test message"
		};

		// Act
		await _service.SendEmailFromContactForm(contactForm);

		// Assert
		_publishEndpointMock.Verify(
			x => x.Publish(
				It.Is<SendEmail>(email =>
					email.Subject == "Kontaktformular besked fra John Doe" &&
					email.ReplyTo!.Email == "john.doe@example.com" &&
					email.ReplyTo.DisplayName == "John Doe" &&
					email.HtmlContent == "Test message" &&
					email.To!.Count == 1 &&
					email.To[0].Email == EmailConstants.ContactEmail.Email
				),
				It.IsAny<CancellationToken>()
			),
			Times.Once
		);
	}

	[Fact]
	public async Task SendEmailFromContactForm_ShouldPublishEmailWithGenericSubject_WhenNameIsNull()
	{
		// Arrange
		var contactForm = new ContactForm
		{
			Name = null,
			Email = "anonymous@example.com",
			Message = "Anonymous message"
		};

		// Act
		await _service.SendEmailFromContactForm(contactForm);

		// Assert
		_publishEndpointMock.Verify(
			x => x.Publish(
				It.Is<SendEmail>(email =>
					email.Subject == "Kontaktformular" &&
					email.ReplyTo!.Email == "anonymous@example.com" &&
					email.ReplyTo.DisplayName == null &&
					email.HtmlContent == "Anonymous message" &&
					email.To!.Count == 1 &&
					email.To[0].Email == EmailConstants.ContactEmail.Email
				),
				It.IsAny<CancellationToken>()
			),
			Times.Once
		);
	}

	[Fact]
	public async Task SendEmailFromContactForm_ShouldSetCorrectReplyTo()
	{
		// Arrange
		var contactForm = new ContactForm
		{
			Name = "Jane Smith",
			Email = "jane.smith@example.com",
			Message = "Question about the service"
		};

		// Act
		await _service.SendEmailFromContactForm(contactForm);

		// Assert
		_publishEndpointMock.Verify(
			x => x.Publish(
				It.Is<SendEmail>(email =>
					email.ReplyTo != null &&
					email.ReplyTo.Email == "jane.smith@example.com" &&
					email.ReplyTo.DisplayName == "Jane Smith"
				),
				It.IsAny<CancellationToken>()
			),
			Times.Once
		);
	}

	#endregion

	#region SendGroupInviteEmail Tests

	[Fact]
	public async Task SendGroupInviteEmail_ShouldPublishEmailToInvitedUser()
	{
		// Arrange
		var groupName = $"InviteGroup_{Guid.NewGuid()}";
		var userId = Guid.NewGuid().ToString();

		var user = new ApplicationUser
		{
			Id = userId,
			Email = "invitee@example.com",
			UserName = "invitee"
		};

		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		// Act
		await _service.SendGroupInviteEmail(groupName, userId);

		// Assert
		_publishEndpointMock.Verify(
			x => x.Publish(
				It.Is<SendEmail>(email =>
					email.Subject == $"Du er inviteret til {groupName}" &&
					email.To != null &&
					email.To.Count == 1 &&
					email.To[0].Email == "invitee@example.com" &&
					email.To[0].DisplayName == "invitee" &&
					email.HtmlContent.Contains(groupName) &&
					email.HtmlContent.Contains($"http://localhost:5000/groups/{groupName}")
				),
				It.IsAny<CancellationToken>()
			),
			Times.Once
		);
	}

	[Fact]
	public async Task SendGroupInviteEmail_ShouldNotPublishEmail_WhenUserNotFound()
	{
		// Arrange
		var groupName = $"TestGroup_{Guid.NewGuid()}";
		var nonExistentUserId = Guid.NewGuid().ToString();

		// Act
		await _service.SendGroupInviteEmail(groupName, nonExistentUserId);

		// Assert
		_publishEndpointMock.Verify(
			x => x.Publish(It.IsAny<SendEmail>(), It.IsAny<CancellationToken>()),
			Times.Never
		);
	}

	[Fact]
	public async Task SendGroupInviteEmail_ShouldIncludeGroupLinkInEmail()
	{
		// Arrange
		var groupName = $"MySpecialGroup_{Guid.NewGuid()}";
		var userId = Guid.NewGuid().ToString();

		var user = new ApplicationUser
		{
			Id = userId,
			Email = "user@example.com",
			UserName = "testuser"
		};

		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		var expectedUrl = $"http://localhost:5000/groups/{groupName}";

		// Act
		await _service.SendGroupInviteEmail(groupName, userId);

		// Assert
		_publishEndpointMock.Verify(
			x => x.Publish(
				It.Is<SendEmail>(email =>
					email.HtmlContent.Contains(expectedUrl) &&
					email.HtmlContent.Contains(EmailTemplate.FooterSignature)
				),
				It.IsAny<CancellationToken>()
			),
			Times.Once
		);
	}

	[Fact]
	public async Task SendGroupInviteEmail_ShouldSetCorrectEmailRecipientDisplayName()
	{
		// Arrange
		var groupName = $"TestGroup_{Guid.NewGuid()}";
		var userId = Guid.NewGuid().ToString();

		var user = new ApplicationUser
		{
			Id = userId,
			Email = "displayname@example.com",
			UserName = "MyDisplayName"
		};

		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		// Act
		await _service.SendGroupInviteEmail(groupName, userId);

		// Assert
		_publishEndpointMock.Verify(
			x => x.Publish(
				It.Is<SendEmail>(email =>
					email.To != null &&
					email.To[0].DisplayName == "MyDisplayName"
				),
				It.IsAny<CancellationToken>()
			),
			Times.Once
		);
	}

	#endregion

	#region SendPartnerImageApprovalEmailAsync Tests

	[Fact]
	public async Task SendPartnerImageApprovalEmailAsync_ShouldPublishEmailToAdmin()
	{
		// Arrange
		var partnerId = Guid.NewGuid();
		var partnerName = $"Test Partner {Guid.NewGuid()}";

		var partner = new Partner
		{
			Id = partnerId,
			Name = partnerName,
			Description = "Test description",
			PartnerPageLink = "https://example.com",
			UserId = _testUserId,
			PendingAdImageUrl = "https://example.com/ad.png"
		};

		_context.Partners.Add(partner);
		await _context.SaveChangesAsync();

		// Act
		await _service.SendPartnerImageApprovalEmailAsync(partnerId, partnerName);

		// Assert
		_publishEndpointMock.Verify(
			x => x.Publish(
				It.Is<SendEmail>(email =>
					email.Subject == $"Ny partner godkendelse: {partnerName}" &&
					email.To != null &&
					email.To.Count == 1 &&
					email.To[0].Email == "kontakt@mini-moeder.dk" &&
					email.To[0].DisplayName == "Mini Møder Admin" &&
					email.HtmlContent.Contains(partnerName) &&
					email.HtmlContent.Contains($"http://localhost:5000/backoffice/partners/{partnerId}")
				),
				It.IsAny<CancellationToken>()
			),
			Times.Once
		);
	}

	[Fact]
	public async Task SendPartnerImageApprovalEmailAsync_ShouldNotPublishEmail_WhenPartnerNotFound()
	{
		// Arrange
		var nonExistentPartnerId = Guid.NewGuid();
		var partnerName = "Non-existent Partner";

		// Act
		await _service.SendPartnerImageApprovalEmailAsync(nonExistentPartnerId, partnerName);

		// Assert
		_publishEndpointMock.Verify(
			x => x.Publish(It.IsAny<SendEmail>(), It.IsAny<CancellationToken>()),
			Times.Never
		);
	}

	#endregion
}

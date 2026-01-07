using Azure;
using Azure.Communication.Email;
using Jordnaer.Consumers;
using Jordnaer.Features.Email;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jordnaer.Tests.Consumers;

[Trait("Category", "UnitTests")]
public class SendEmailConsumerTests
{
	private readonly EmailClient _mockEmailClient;
	private readonly SendEmailConsumer _consumer;

	public SendEmailConsumerTests()
	{
		_mockEmailClient = Substitute.For<EmailClient>();
		_consumer = new SendEmailConsumer(new NullLogger<SendEmailConsumer>(), _mockEmailClient);
	}

	[Fact]
	public async Task Consume_ShouldSendEmailSuccessfully()
	{
		// Arrange
		var sendEmail = new SendEmail
		{
			From = new EmailRecipient { Email = "test@test.com" },
			Subject = "Test Subject",
			HtmlContent = "Test Content",
			To = [new EmailRecipient { Email = "recipient@test.com" }]
		};

		var consumeContext = Substitute.For<ConsumeContext<SendEmail>>();
		consumeContext.Message.Returns(sendEmail);

		_mockEmailClient.SendAsync(WaitUntil.Completed, Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
						   .ReturnsForAnyArgs(new EmailSendOperation("test", _mockEmailClient));

		// Act
		await _consumer.Consume(consumeContext);

		// Assert
		await _mockEmailClient.Received(1).SendAsync(WaitUntil.Completed, Arg.Is<EmailMessage>(x => x.SenderAddress == sendEmail.From.Email &&
																x.Content.Html == sendEmail.HtmlContent &&
																x.Content.Subject == sendEmail.Subject &&
																x.Recipients.To.Select(r => r.Address).Contains(sendEmail.To.First().Email)), Arg.Any<CancellationToken>());
	}
}
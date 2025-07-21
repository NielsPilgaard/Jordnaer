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
	private readonly EmailClient _mockSendGridClient;
	private readonly SendEmailConsumer _consumer;

	public SendEmailConsumerTests()
	{
		_mockSendGridClient = Substitute.For<EmailClient>();
		_consumer = new SendEmailConsumer(new NullLogger<SendMessageConsumer>(), _mockSendGridClient);
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

		_mockSendGridClient.SendAsync(WaitUntil.Completed, Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
						   .Returns(new EmailSendOperation("test", _mockSendGridClient));

		// Act
		await _consumer.Consume(consumeContext);

		// Assert
		await _mockSendGridClient.Received(1).SendAsync(WaitUntil.Completed, Arg.Is<EmailMessage>(x => x.SenderAddress == sendEmail.From.Email &&
																x.Content.Html == sendEmail.HtmlContent &&
																x.Content.Subject == sendEmail.Subject &&
																x.Recipients.To.Select(r => r.Address).Contains(sendEmail.To.First().Email)), Arg.Any<CancellationToken>());
	}
}
using System.Net;
using Jordnaer.Consumers;
using Jordnaer.Features.Email;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SendGrid;
using SendGrid.Helpers.Mail;
using Xunit;
using Response = SendGrid.Response;

namespace Jordnaer.Tests.Consumers;

[Trait("Category", "UnitTests")]
public class SendEmailConsumerTests
{
	private readonly ISendGridClient _mockSendGridClient;
	private readonly SendEmailConsumer _consumer;

	public SendEmailConsumerTests()
	{
		_mockSendGridClient = Substitute.For<ISendGridClient>();
		_consumer = new SendEmailConsumer(new NullLogger<SendMessageConsumer>(), _mockSendGridClient);
	}

	[Fact]
	public async Task Consume_ShouldSendEmailSuccessfully()
	{
		// Arrange
		var sendEmail = new SendEmail
		{
			From = new EmailAddress("test@test.com"),
			Subject = "Test Subject",
			HtmlContent = "Test Content",
			To = [new EmailAddress("recipient@test.com")]
		};

		var consumeContext = Substitute.For<ConsumeContext<SendEmail>>();
		consumeContext.Message.Returns(sendEmail);

		_mockSendGridClient.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
						   .Returns(new Response(HttpStatusCode.Accepted, null, null));

		// Act
		await _consumer.Consume(consumeContext);

		// Assert
		await _mockSendGridClient.Received(1).SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>());
	}
}
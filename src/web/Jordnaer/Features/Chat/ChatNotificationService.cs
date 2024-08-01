using Jordnaer.Consumers;
using Jordnaer.Database;
using Jordnaer.Features.Email;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;

namespace Jordnaer.Features.Chat;

public class ChatNotificationService(
	JordnaerDbContext context,
	ILogger<StartChatConsumer> logger,
	IPublishEndpoint publishEndpoint,
	IServer server)
{
	public async Task NotifyRecipients(StartChat startChat, CancellationToken cancellationToken = default)
	{
		var recipientIds = startChat.Recipients.Select(recipient => recipient.Id);
		var recipients = await context.Users
									  .AsNoTracking()
									  .Where(x => recipientIds.Contains(x.Id))
									  .Select(x => new { x.Email, x.Id })
									  .ToDictionaryAsync(x => x.Id, x => x.Email, cancellationToken);

		var emailsToSend = CreateEmails(startChat, recipients);

		await publishEndpoint.PublishBatch(emailsToSend, cancellationToken);

		logger.LogInformation("Sent emails to users notifying them about a newly started chat.");
	}

	private IEnumerable<SendEmail> CreateEmails(StartChat startChat, Dictionary<string, string?> recipients)
	{
		var initiator = startChat.Recipients.First(x => x.Id == startChat.InitiatorId);

		foreach (var recipientId in recipients.Keys.Where(x => x != startChat.InitiatorId))
		{
			var user = startChat.Recipients.First(x => x.Id == recipientId);

			var email = recipients[recipientId];

			var recipientsEmailAddress = new EmailAddress(email, user.DisplayName);

			yield return new SendEmail
			{
				To = [recipientsEmailAddress],
				Subject = $"Ny besked fra {initiator.DisplayName}",
				HtmlContent = CreateNewChatEmailMessage(user.DisplayName, initiator.DisplayName, GetChatLink(startChat.Id))
			};
		}
	}

	private string GetChatLink(Guid chatId)
	{
		//TODO: Replace address look-up with config
		var serverAddressFeature = server.Features.Get<IServerAddressesFeature>();
		var serverAddress = serverAddressFeature?.Addresses.FirstOrDefault();

		if (serverAddress is null)
		{
			logger.LogError("No addresses found in the IServerAddressFeature. A link to the chat cannot be created.");
		}

		return serverAddress is null
				   ? $"https://mini-moeder.dk/chat/{chatId}"
				   : $"{serverAddress}/chat/{chatId}";
	}

	private static string CreateNewChatEmailMessage(string recipientDisplayName,
		string messageSenderDisplayName,
		string link) => $"""

						 <p>Hej {recipientDisplayName},</p>

						 <p>Du har f�et en ny besked fra {messageSenderDisplayName}</p>

						 <p>Hvis du vil g� direkte til beskeden, kan du klikke p� linket nedenfor:</p>

						 <p><a href="{link}">L�s besked</a></p>

						 <p>Venlig hilsen,</p>

						 <p>Mini M�der teamet</p>

						 """;
}
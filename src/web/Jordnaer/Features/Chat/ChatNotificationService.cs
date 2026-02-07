using Jordnaer.Consumers;
using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Email;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jordnaer.Features.Chat;

public class ChatNotificationService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<StartChatConsumer> logger,
	IPublishEndpoint publishEndpoint,
	IOptions<AppOptions> options)
{
	public async Task NotifyRecipients(StartChat startChat, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var recipientIds = startChat.Recipients.Select(recipient => recipient.Id);

		// Get users with email and their notification preferences
		var usersWithPreferences = await context.Users
			.AsNoTracking()
			.Where(x => recipientIds.Contains(x.Id) && !string.IsNullOrEmpty(x.Email))
			.Join(context.UserProfiles,
				user => user.Id,
				profile => profile.Id,
				(user, profile) => new { user.Id, user.Email, profile.ChatNotificationPreference })
			.Where(x => x.ChatNotificationPreference == ChatNotificationPreference.FirstMessageOnly
				|| x.ChatNotificationPreference == ChatNotificationPreference.AllMessages)
			.ToListAsync(cancellationToken);

		if (!usersWithPreferences.Any())
		{
			logger.LogInformation("No recipients want chat notifications for this message.");
			return;
		}

		var recipients = usersWithPreferences.ToDictionary(x => x.Id, x => x.Email!);
		var emailsToSend = CreateEmails(startChat, recipients);

		await publishEndpoint.PublishBatch(emailsToSend, cancellationToken);

		logger.LogInformation("Sent emails to users notifying them about a newly started chat.");
	}

	internal IEnumerable<SendEmail> CreateEmails(StartChat startChat, Dictionary<string, string> recipients)
	{
		var initiator = startChat.Recipients.First(x => x.Id == startChat.InitiatorId);

		foreach (var recipientId in recipients.Keys.Where(x => x != startChat.InitiatorId))
		{
			var user = startChat.Recipients.First(x => x.Id == recipientId);

			var email = recipients[recipientId];

			var recipientsEmailAddress = new EmailRecipient
			{
				Email = email,
				DisplayName = user.DisplayName
			};

			yield return new SendEmail
			{
				To = [recipientsEmailAddress],
				Subject = $"Ny besked fra {initiator.DisplayName}",
				HtmlContent = CreateNewChatEmailMessage(user.DisplayName, initiator.DisplayName, GetChatLink(startChat.Id))
			};
		}
	}

	internal string GetChatLink(Guid chatId)
	{
		var serverAddress = options.Value.BaseUrl?.TrimEnd('/');

		return serverAddress is null
				   ? $"https://mini-moeder.dk/chat/{chatId}"
				   : $"{serverAddress}/chat/{chatId}";
	}

	public async Task NotifyRecipientsOfNewMessage(SendMessage message, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		// Get all recipients of this chat (excluding the sender)
		var recipientIds = await context.UserChats
			.AsNoTracking()
			.Where(x => x.ChatId == message.ChatId && x.UserProfileId != message.SenderId)
			.Select(x => x.UserProfileId)
			.ToListAsync(cancellationToken);

		// Get users who want "AllMessages" notifications
		var usersWithPreferences = await context.Users
			.AsNoTracking()
			.Where(x => recipientIds.Contains(x.Id) && !string.IsNullOrEmpty(x.Email))
			.Join(context.UserProfiles,
				user => user.Id,
				profile => profile.Id,
				(user, profile) => new { user.Id, user.Email, User = user, profile.ChatNotificationPreference })
			.Where(x => x.ChatNotificationPreference == ChatNotificationPreference.AllMessages)
			.ToListAsync(cancellationToken);

		if (!usersWithPreferences.Any())
		{
			logger.LogInformation("No recipients want all-message notifications for this chat.");
			return;
		}

		// Get sender info
		var sender = await context.UserProfiles
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == message.SenderId, cancellationToken);

		if (sender is null)
		{
			logger.LogWarning("Sender profile not found for message notification.");
			return;
		}

		var chatLink = GetChatLink(message.ChatId);
		var emailsToSend = new List<SendEmail>();

		foreach (var userWithPref in usersWithPreferences)
		{
			var recipientEmailAddress = new EmailRecipient
			{
				Email = userWithPref.Email!,
				DisplayName = userWithPref.User.UserName ?? userWithPref.Email!
			};

			emailsToSend.Add(new SendEmail
			{
				To = [recipientEmailAddress],
				Subject = $"Ny besked fra {sender.DisplayName}",
				HtmlContent = CreateNewChatEmailMessage(recipientEmailAddress.DisplayName, sender.DisplayName, chatLink)
			});
		}

		await publishEndpoint.PublishBatch(emailsToSend, cancellationToken);

		logger.LogInformation("Sent {Count} emails for new chat message.", emailsToSend.Count);
	}

	private string CreateNewChatEmailMessage(string recipientDisplayName,
		string messageSenderDisplayName,
		string link)
	{
		var body = $"""
				   {EmailConstants.Greeting(recipientDisplayName)}

				   <p>Du har fået en ny besked fra <b>{messageSenderDisplayName}</b></p>

				   <p>Hvis du vil gå direkte til beskeden, kan du klikke på knappen nedenfor:</p>

				   {EmailTemplate.Button(link, "Læs besked")}
				   """;

		return EmailTemplate.Wrap(body, options.Value.BaseUrl, preheaderText: $"Ny besked fra {messageSenderDisplayName}");
	}
}
using Jordnaer.Database;
using Jordnaer.Features.Email;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Consumers;

public class GroupPostCreatedConsumer(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<GroupPostCreatedConsumer> logger,
	IPublishEndpoint publishEndpoint,
	NavigationManager navigationManager) : IConsumer<GroupPostCreated>
{
	public async Task Consume(ConsumeContext<GroupPostCreated> consumeContext)
	{
		logger.LogInformation("Consuming GroupPostCreated message. PostId: {PostId}, GroupId: {GroupId}",
			consumeContext.Message.PostId, consumeContext.Message.GroupId);

		var message = consumeContext.Message;

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(consumeContext.CancellationToken);

			// Get all active members excluding the post author
			var activeMembers = context.GroupMemberships
				.AsNoTracking()
				.Where(x => x.GroupId == message.GroupId &&
						   x.MembershipStatus == MembershipStatus.Active &&
						   x.UserProfileId != message.AuthorId)
				.Select(x => x.UserProfileId);

			// Get their email addresses
			var emails = await context.Users
				.AsNoTracking()
				.Where(user => activeMembers.Any(userId => userId == user.Id) &&
							  !string.IsNullOrEmpty(user.Email))
				.Select(user => new EmailRecipient
				{
					Email = user.Email!,
					DisplayName = user.UserName
				})
				.ToListAsync(consumeContext.CancellationToken);

			if (emails.Count == 0)
			{
				logger.LogInformation("No members to notify for new post in group {GroupName}", message.GroupName);
				return;
			}

			logger.LogInformation("Sending new post notification to {Count} members in group {GroupName}",
				emails.Count, message.GroupName);

			var groupUrl = $"{navigationManager.BaseUri}groups/{message.GroupName}";
			var postPreview = GetPostPreview(message.PostText);

			var email = new SendEmail
			{
				Subject = $"Nyt opslag i {message.GroupName}",
				HtmlContent = CreateNewPostEmailContent(message.AuthorDisplayName, postPreview, groupUrl),
				Bcc = emails
			};

			await publishEndpoint.Publish(email, consumeContext.CancellationToken);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to send new post notifications for post {PostId} in group {GroupId}",
				message.PostId, message.GroupId);
			// Don't rethrow - we don't want email failures to break post creation
		}
	}

	private static string GetPostPreview(string text)
	{
		// Strip HTML tags and limit to 200 characters
		var plainText = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
		return plainText.Length <= 200
			? plainText
			: plainText.Substring(0, 200) + "...";
	}

	private static string CreateNewPostEmailContent(string authorName, string postPreview, string groupUrl)
	{
		return $"""
			<h4>Nyt opslag i din gruppe</h4>

			<p><b>{authorName}</b> har oprettet et nyt opslag:</p>

			<blockquote style="border-left: 3px solid #ccc; padding-left: 10px; color: #666;">
				{postPreview}
			</blockquote>

			<p><a href="{groupUrl}">Klik her for at se opslaget</a></p>

			{EmailConstants.Signature}
			""";
	}
}

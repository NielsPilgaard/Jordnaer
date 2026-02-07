using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Email;
using Jordnaer.Features.Metrics;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Jordnaer.Consumers;

public partial class GroupPostCreatedConsumer(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<GroupPostCreatedConsumer> logger,
	IPublishEndpoint publishEndpoint,
	IOptions<AppOptions> appOptions) : IConsumer<GroupPostCreated>
{
	public async Task Consume(ConsumeContext<GroupPostCreated> consumeContext)
	{
		JordnaerMetrics.GroupPostCreatedConsumerReceivedCounter.Add(1);

		logger.LogInformation("Consuming GroupPostCreated message. PostId: {PostId}, GroupId: {GroupId}",
			consumeContext.Message.PostId, consumeContext.Message.GroupId);

		var message = consumeContext.Message;

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(consumeContext.CancellationToken);

			// Get all active members excluding the post author who have email notifications enabled
			var activeMembers = await context.GroupMemberships
				.AsNoTracking()
				.Where(x => x.GroupId == message.GroupId &&
						   x.MembershipStatus == MembershipStatus.Active &&
						   x.UserProfileId != message.AuthorId &&
						   x.EmailOnNewPost)
				.Select(x => x.UserProfileId)
				.ToListAsync(consumeContext.CancellationToken);

			// Get their email addresses
			var emails = await context.Users
				.AsNoTracking()
				.Where(user => activeMembers.Contains(user.Id) &&
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
				JordnaerMetrics.GroupPostCreatedConsumerSucceededCounter.Add(1);
				return;
			}

			logger.LogInformation("Sending new post notification to {Count} members in group {GroupName}",
				emails.Count, message.GroupName);

			var baseUrl = appOptions.Value.BaseUrl.TrimEnd('/');
			var groupUrl = $"{baseUrl}/groups/{message.GroupName}";
			var postPreview = GetPostPreview(message.PostText);

			var email = new SendEmail
			{
				Subject = $"Nyt opslag i {message.GroupName}",
				HtmlContent = CreateNewPostEmailContent(baseUrl, message.AuthorDisplayName, postPreview, groupUrl),
				Bcc = emails
			};

			await publishEndpoint.Publish(email, consumeContext.CancellationToken);

			JordnaerMetrics.GroupPostCreatedConsumerSucceededCounter.Add(1);
		}
		catch (Exception ex)
		{
			JordnaerMetrics.GroupPostCreatedConsumerFailedCounter.Add(1);

			logger.LogError(ex, "Failed to send new post notifications for post {PostId} in group {GroupId}",
				message.PostId, message.GroupId);
			// Don't rethrow - we don't want email failures to break post creation
		}
	}

	private static string GetPostPreview(string text)
	{
		// Strip HTML tags and limit to 200 characters
		var plainText = HtmlTagsRegex().Replace(text, string.Empty);
		return plainText.Length <= 200
			? plainText
			: plainText.Substring(0, 200) + "...";
	}

	private static string CreateNewPostEmailContent(string baseUrl, string authorName, string postPreview, string groupUrl) =>
		EmailContentBuilder.GroupPostNotification(baseUrl, authorName, postPreview, groupUrl);

	[GeneratedRegex("<.*?>")]
	private static partial Regex HtmlTagsRegex();
}

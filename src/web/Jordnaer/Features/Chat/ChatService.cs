using Jordnaer.Database;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Chat;

public interface IChatService
{
	Task<List<ChatDto>> GetChatsAsync(string userId, CancellationToken cancellationToken = default);

	Task<OneOf<Success, Error<string>>> MarkMessagesAsReadAsync(string userId, Guid chatId,
		CancellationToken cancellationToken = default);

	Task<int> GetUnreadMessageCountAsync(string userId, CancellationToken cancellationToken = default);

	Task<OneOf<List<ChatMessageDto>, Error<string>>> GetChatMessagesAsync(string userId, Guid chatId, int skip, int take, CancellationToken cancellationToken = default);

	ValueTask<OneOf<Guid, Error<string>>> StartChatAsync(StartChat chat,
		CancellationToken cancellationToken = default);

	ValueTask<OneOf<Success, Error<string>>> SendMessageAsync(ChatMessageDto chatMessage,
		CancellationToken cancellationToken = default);

	Task SetChatNameAsync(SetChatName setChatName,
		CancellationToken cancellationToken = default);

	Task<OneOf<Guid, NotFound>> GetChatByUserIdsAsync(string userId, ICollection<string> userIds,
		CancellationToken cancellationToken = default);
}

public class ChatService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	IPublishEndpoint publishEndpoint,
	ILogger<ChatService> logger)
	: IChatService
{
	public async Task<List<ChatDto>> GetChatsAsync(string userId, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var chats = await context
						  .Chats
						  .AsNoTracking()
						  .Where(chat => chat.Recipients.Any(recipient => recipient.Id == userId))
						  .OrderByDescending(chat => chat.LastMessageSentUtc)
						  .Select(chat => new ChatDto
						  {
							  DisplayName = chat.DisplayName,
							  Id = chat.Id,
							  LastMessageSentUtc = chat.LastMessageSentUtc,
							  StartedUtc = chat.StartedUtc,
							  Recipients = chat.Recipients.Select(recipient => recipient.ToUserSlim()).ToList(),
							  UnreadMessageCount = context.UnreadMessages.Count(unreadMessage =>
								  unreadMessage.ChatId == chat.Id && unreadMessage.RecipientId == userId)
						  })
						  .ToListAsync(cancellationToken);

		return chats;
	}

	public async Task<OneOf<Success, Error<string>>> MarkMessagesAsReadAsync(string userId, Guid chatId, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var rowsModified = await context
								 .UnreadMessages
								 .AsNoTracking()
								 .Where(unreadMessage => unreadMessage.ChatId == chatId && unreadMessage.RecipientId == userId)
								 .ExecuteDeleteAsync(cancellationToken);

		if (rowsModified > 0)
		{
			return new Success();
		}

		logger.LogWarning("No messages were marked as read for the chat {ChatId} for the User {UserId}",
						   chatId, userId);

		return new Error<string>($"Failed to mark messages as read for the chat {chatId}");
	}

	public async Task<int> GetUnreadMessageCountAsync(string userId, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var unreadMessageCount = await context
									   .UnreadMessages
									   .AsNoTracking()
									   .Where(unreadMessage => unreadMessage.RecipientId == userId)
									   .CountAsync(cancellationToken);

		return unreadMessageCount;
	}

	private const string NotFound = "Chat does not exist, unable to get messages for it.";

	public async Task<OneOf<List<ChatMessageDto>, Error<string>>> GetChatMessagesAsync(string userId, Guid chatId, int skip, int take, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var chat = await context.Chats
								.AsNoTracking()
								.AsSingleQuery()
								.Include(chat => chat.Recipients)
								.FirstOrDefaultAsync(x => x.Id == chatId, cancellationToken);
		if (chat is null)
		{
			logger.LogDebug(NotFound);
			return new Error<string>(NotFound);
		}

		// Check if any of the recipients are the user we're fetching messages for
		if (chat.Recipients.All(x => x.Id != userId))
		{
			logger.LogWarning(
				"Tried to get chat messages for chat {ChatId} and UserId {UserId}, but that user is not part of that chat. Access denied.", chatId, userId);

			return new Error<string>(
				$"Tried to get chat messages for chat {chatId} and UserId {userId}, but that user is not part of that chat. Access denied.");
		}

		var chatMessages = await context
								 .ChatMessages
								 .AsNoTracking()
								 .Where(message => message.ChatId == chatId)
								 .OrderBy(message => message.SentUtc)
								 .Skip(skip)
								 .Take(take)
								 .Select(message => message.ToChatMessageDto())
								 .ToListAsync(cancellationToken);
		return chatMessages;
	}

	public async ValueTask<OneOf<Guid, Error<string>>> StartChatAsync(StartChat chat, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var chatAlreadyExists = await context
									  .Chats
									  .AsNoTracking()
									  .AnyAsync(existingChat => existingChat.Id == chat.Id, cancellationToken);

		if (chatAlreadyExists)
		{
			logger.LogWarning("Failed to create chat with id {ChatId}, it has already been created.", chat.Id);

			return new Error<string>("Failed to create chat, it has already been created.");
		}

		await publishEndpoint.Publish(chat, cancellationToken);

		return chat.Id;
	}

	public async ValueTask<OneOf<Success, Error<string>>> SendMessageAsync(ChatMessageDto chatMessage, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var messageAlreadyExists = await context
										 .ChatMessages
										 .AsNoTracking()
										 .AnyAsync(message => message.Id == chatMessage.Id, cancellationToken);
		if (messageAlreadyExists)
		{
			logger.LogError("Message already exists.");
			return new Error<string>("Message already exists.");
		}

		await publishEndpoint.Publish(chatMessage.ToSendMessage(), cancellationToken);

		return new Success();
	}

	public async Task SetChatNameAsync(SetChatName setChatName, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var chat = await context.Chats.FindAsync([setChatName.ChatId], cancellationToken);
		if (chat is null)
		{
		}

		// TODO Set the chat name here, e.g., chat.Name = setChatName.NewName;
		// Then update the database context and save changes.

		await publishEndpoint.Publish(setChatName, cancellationToken);
	}

	public async Task<OneOf<Guid, NotFound>> GetChatByUserIdsAsync(string currentUserId, ICollection<string> userIds, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var existingChat = await context
								 .Chats
								 .AsNoTracking()
								 .Where(chat => chat.Recipients.Any(recipient => recipient.Id == currentUserId))
								 .FirstOrDefaultAsync(chat => chat.Recipients.Count == userIds.Count &&
															  chat.Recipients.All(recipient => userIds.Contains(recipient.Id)), cancellationToken);

		return existingChat is null
				   ? new NotFound()
				   : existingChat.Id;
	}
}
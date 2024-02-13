using Jordnaer.Database;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using OneOf;

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

public class ChatService : IChatService
{
	private readonly JordnaerDbContext _context;
	private readonly IPublishEndpoint _publishEndpoint;
	private readonly ILogger<ChatService> _logger;

	public ChatService(JordnaerDbContext context,
		IPublishEndpoint publishEndpoint,
		ILogger<ChatService> logger)
	{
		_context = context;
		_publishEndpoint = publishEndpoint;
		_logger = logger;
	}

	public async Task<List<ChatDto>> GetChatsAsync(string userId, CancellationToken cancellationToken = default)
	{
		var chats = await _context
						  .Chats
						  .AsNoTracking()
						  .Where(chat => chat.Recipients.Any(recipient => recipient.Id == userId))
						  .OrderByDescending(chat => chat.LastMessageSentUtc)
						  // TODO: This include might not be needed
						  //.Include(chat => chat.Recipients)
						  .Select(chat => new ChatDto
						  {
							  DisplayName = chat.DisplayName,
							  Id = chat.Id,
							  LastMessageSentUtc = chat.LastMessageSentUtc,
							  StartedUtc = chat.StartedUtc,
							  Recipients = chat.Recipients.Select(recipient => recipient.ToUserSlim()).ToList(),
							  UnreadMessageCount = _context.UnreadMessages.Count(unreadMessage =>
								  unreadMessage.ChatId == chat.Id && unreadMessage.RecipientId == userId)
						  })
						  .ToListAsync(cancellationToken);

		return chats;
	}

	public async Task<OneOf<Success, Error<string>>> MarkMessagesAsReadAsync(string userId, Guid chatId, CancellationToken cancellationToken = default)
	{
		var rowsModified = await _context
								 .UnreadMessages
								 .AsNoTracking()
								 .Where(unreadMessage => unreadMessage.ChatId == chatId && unreadMessage.RecipientId == userId)
								 .ExecuteDeleteAsync(cancellationToken);

		if (rowsModified > 0)
		{
			return new Success();
		}

		_logger.LogWarning("No messages were marked as read for the chat {ChatId} for the User {UserId}",
						   chatId, userId);

		return new Error<string>($"Failed to mark messages as read for the chat {chatId}");
	}

	public async Task<int> GetUnreadMessageCountAsync(string userId, CancellationToken cancellationToken = default)
	{
		var unreadMessageCount = await _context
									   .UnreadMessages
									   .AsNoTracking()
									   .Where(unreadMessage => unreadMessage.RecipientId == userId)
									   .CountAsync(cancellationToken);

		return unreadMessageCount;
	}

	public async Task<OneOf<List<ChatMessageDto>, Error<string>>> GetChatMessagesAsync(string userId, Guid chatId, int skip, int take, CancellationToken cancellationToken = default)
	{
		if (!await IsCurrentUserPartOfChat(userId, chatId, cancellationToken))
		{
			_logger.LogWarning(
				"Tried to get chat messages for chat {ChatId} and UserId {UserId}, but that user is not part of that chat. Access denied.", chatId, userId);

			return new Error<string>(
				$"Tried to get chat messages for chat {chatId} and UserId {userId}, but that user is not part of that chat. Access denied.");
		}

		var chatMessages = await _context
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
		var chatAlreadyExists = await _context
									  .Chats
									  .AsNoTracking()
									  .AnyAsync(existingChat => existingChat.Id == chat.Id, cancellationToken);

		if (chatAlreadyExists)
		{
			_logger.LogWarning("Failed to create chat with id {ChatId}, it has already been created.", chat.Id);

			return new Error<string>("Failed to create chat, it has already been created.");
		}

		await _publishEndpoint.Publish(chat, cancellationToken);

		return chat.Id;
	}

	public async ValueTask<OneOf<Success, Error<string>>> SendMessageAsync(ChatMessageDto chatMessage, CancellationToken cancellationToken = default)
	{
		var messageAlreadyExists = await _context
										 .ChatMessages
										 .AsNoTracking()
										 .AnyAsync(message => message.Id == chatMessage.Id, cancellationToken);
		if (messageAlreadyExists)
		{
			_logger.LogError("Message already exists.");
			return new Error<string>("Message already exists.");
		}

		await _publishEndpoint.Publish(chatMessage.ToSendMessage(), cancellationToken);

		return new Success();
	}

	public async Task SetChatNameAsync(SetChatName setChatName, CancellationToken cancellationToken = default)
	{
		var chat = await _context.Chats.FindAsync([setChatName.ChatId], cancellationToken);
		if (chat is null)
		{
		}

		// TODO Set the chat name here, e.g., chat.Name = setChatName.NewName;
		// Then update the database context and save changes.

		await _publishEndpoint.Publish(setChatName, cancellationToken);
	}

	public async Task<OneOf<Guid, NotFound>> GetChatByUserIdsAsync(string currentUserId, ICollection<string> userIds, CancellationToken cancellationToken = default)
	{
		var existingChat = await _context
								 .Chats
								 .AsNoTracking()
								 .Where(chat => chat.Recipients.Any(recipient => recipient.Id == currentUserId))
								 .FirstOrDefaultAsync(chat => chat.Recipients.Count == userIds.Count &&
															  chat.Recipients.All(recipient => userIds.Contains(recipient.Id)), cancellationToken);

		return existingChat is null
				   ? new NotFound()
				   : existingChat.Id;
	}

	private async Task<bool> IsCurrentUserPartOfChat(string userId, Guid chatId, CancellationToken cancellationToken = default)
	{
		var chat = await _context
						 .Chats
						 .AsNoTracking()
						 .FirstOrDefaultAsync(chat => chat.Id == chatId &&
													  chat.Recipients.Any(recipient => recipient.Id == userId), cancellationToken);

		return chat != null;
	}
}
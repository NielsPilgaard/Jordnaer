using Jordnaer.Authorization;
using Jordnaer.Database;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using OneOf;

namespace Jordnaer.Features.Chat;

public interface IChatService
{
	Task<List<ChatDto>> GetChatsAsync(int skip, int take,
		CancellationToken cancellationToken = default);

	Task<OneOf<Success, Error<string>>> MarkMessagesAsReadAsync(Guid chatId,
		CancellationToken cancellationToken = default);

	Task<int> GetUnreadMessageCountAsync(CancellationToken cancellationToken = default);

	Task<OneOf<List<ChatMessageDto>, Error<string>>> GetChatMessagesAsync(Guid chatId, int skip, int take, CancellationToken cancellationToken = default);

	ValueTask<OneOf<Guid, Error<string>>> StartChatAsync(StartChat chat,
		CancellationToken cancellationToken = default);

	ValueTask<OneOf<Success, Error<string>>> SendMessageAsync(ChatMessageDto chatMessage,
		CancellationToken cancellationToken = default);

	Task SetChatNameAsync(SetChatName setChatName,
		CancellationToken cancellationToken = default);

	Task<OneOf<Guid, NotFound>> GetChatByUserIdsAsync(ICollection<string> userIds,
		CancellationToken cancellationToken = default);
}

public class ChatService : IChatService
{
	private readonly JordnaerDbContext _context;
	private readonly CurrentUser _currentUser;
	private readonly IPublishEndpoint _publishEndpoint;
	private readonly ILogger<ChatService> _logger;

	public ChatService(JordnaerDbContext context,
		CurrentUser currentUser,
		IPublishEndpoint publishEndpoint,
		ILogger<ChatService> logger)
	{
		_context = context;
		_currentUser = currentUser;
		_publishEndpoint = publishEndpoint;
		_logger = logger;
	}

	public async Task<List<ChatDto>> GetChatsAsync(int skip, int take, CancellationToken cancellationToken = default)
	{
		var chats = await _context
						  .Chats
						  .AsNoTracking()
						  .Where(chat => chat.Recipients.Any(recipient => recipient.Id == _currentUser.Id))
						  .OrderByDescending(chat => chat.LastMessageSentUtc)
						  .Skip(skip)
						  .Take(take)
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
								  unreadMessage.ChatId == chat.Id && unreadMessage.RecipientId == _currentUser.Id)
						  })
						  .ToListAsync(cancellationToken);

		return chats;
	}

	public async Task<OneOf<Success, Error<string>>> MarkMessagesAsReadAsync(Guid chatId, CancellationToken cancellationToken = default)
	{
		var rowsModified = await _context
								 .UnreadMessages
								 .AsNoTracking()
								 .Where(unreadMessage => unreadMessage.ChatId == chatId && unreadMessage.RecipientId == _currentUser.Id)
								 .ExecuteDeleteAsync(cancellationToken);

		if (rowsModified > 0)
		{
			return new Success();
		}

		_logger.LogWarning("No messages were marked as read for the chat {ChatId} for the User {UserId}",
						   chatId, _currentUser.Id);

		return new Error<string>($"Failed to mark messages as read for the chat {chatId}");
	}

	public async Task<int> GetUnreadMessageCountAsync(CancellationToken cancellationToken = default)
	{
		var unreadMessageCount = await _context
									   .UnreadMessages
									   .AsNoTracking()
									   .Where(unreadMessage => unreadMessage.RecipientId == _currentUser.Id)
									   .CountAsync(cancellationToken);

		return unreadMessageCount;
	}

	public async Task<OneOf<List<ChatMessageDto>, Error<string>>> GetChatMessagesAsync(Guid chatId, int skip, int take, CancellationToken cancellationToken = default)
	{
		if (!await IsCurrentUserPartOfChat(chatId, cancellationToken))
		{
			_logger.LogWarning(
				"Tried to get chat messages for chat {ChatId} and UserId {UserId}, but that user is not part of that chat. Access denied.", chatId, _currentUser.Id);

			return new Error<string>(
				$"Tried to get chat messages for chat {chatId} and UserId {_currentUser.Id}, but that user is not part of that chat. Access denied.");
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
		if (!chat.Recipients.Select(recipient => recipient.Id).Contains(_currentUser.Id))
		{
			_logger.LogError("Failed to create chat with id {ChatId}, the current user is not among the recipients.", chat.Id);

			return new Error<string>("Failed to create chat, the current user is not among the recipients.");
		}

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
		if (chatMessage.SenderId != _currentUser.Id)
		{
			_logger.LogError("Sender ID does not match current user.");
			return new Error<string>("Sender ID does not match current user.");
		}

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

	public async Task<OneOf<Guid, NotFound>> GetChatByUserIdsAsync(ICollection<string> userIds, CancellationToken cancellationToken = default)
	{
		var existingChat = await _context
								 .Chats
								 .AsNoTracking()
								 .Where(chat => chat.Recipients.Any(recipient => recipient.Id == _currentUser.Id))
								 .FirstOrDefaultAsync(chat => chat.Recipients.Count == userIds.Count &&
															  chat.Recipients.All(recipient => userIds.Contains(recipient.Id)), cancellationToken);

		return existingChat is null
				   ? new NotFound()
				   : existingChat.Id;
	}

	private async Task<bool> IsCurrentUserPartOfChat(Guid chatId, CancellationToken cancellationToken = default)
	{
		var chat = await _context
						 .Chats
						 .AsNoTracking()
						 .FirstOrDefaultAsync(chat => chat.Id == chatId &&
													  chat.Recipients.Any(recipient => recipient.Id == _currentUser.Id), cancellationToken);

		return chat != null;
	}
}
using System.Collections.Concurrent;
using Jordnaer.Shared;

namespace Jordnaer.Features.Chat;

public interface IChatMessageCache
{
	ValueTask<List<ChatMessageDto>> GetChatMessagesAsync(string userId, Guid chatId,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Circuit-scoped in-memory cache for chat messages.
/// Since this is registered as Scoped, each Blazor Server circuit gets its own instance
/// that lives for the duration of the user's session — no need for a shared server-side cache.
/// </summary>
public class ChatMessageCache(IChatService chatService) : IChatMessageCache
{
	private readonly ConcurrentDictionary<Guid, List<ChatMessageDto>> _cache = new();

	public async ValueTask<List<ChatMessageDto>> GetChatMessagesAsync(string userId, Guid chatId, CancellationToken cancellationToken = default)
	{
		if (!_cache.TryGetValue(chatId, out var cachedMessages))
		{
			// First time: load all messages
			var getChatMessagesResponse = await chatService.GetChatMessagesAsync(userId, chatId, 0, int.MaxValue, cancellationToken);
			var messages = getChatMessagesResponse.Match(m => m, _ => []);

			_cache[chatId] = messages;

			return messages;
		}

		// Fetch new messages since last cached
		var newMessagesResponse = await chatService.GetChatMessagesAsync(userId, chatId, cachedMessages.Count, int.MaxValue, cancellationToken);
		var newMessages = newMessagesResponse.Match(m => m, _ => []);

		if (newMessages.Count > 0)
		{
			cachedMessages.AddRange(newMessages);
		}

		return cachedMessages;
	}
}

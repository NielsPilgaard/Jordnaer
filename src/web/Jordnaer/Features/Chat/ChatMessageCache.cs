using Jordnaer.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace Jordnaer.Features.Chat;

public interface IChatMessageCache
{
	ValueTask<List<ChatMessageDto>> GetChatMessagesAsync(string userId, Guid chatId,
		CancellationToken cancellationToken = default);
}

public class ChatMessageCache(IChatService chatService, IMemoryCache memoryCache) : IChatMessageCache
{
	public async ValueTask<List<ChatMessageDto>> GetChatMessagesAsync(string userId, Guid chatId, CancellationToken cancellationToken = default)
	{
		var key = $"{userId}:chatmessages:{chatId}";
		var cacheWasEmpty = false;
		var cachedMessages = await memoryCache.GetOrCreateAsync(key, async entry =>
		{
			cacheWasEmpty = true;

			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);

			var getChatMessagesResponse = await chatService.GetChatMessagesAsync(userId, chatId, 0, int.MaxValue, cancellationToken);

			return getChatMessagesResponse.Match(messages => messages,
												 _ => []);
		});

		// If this is the first time we're populating the cache, all messages are new
		if (cacheWasEmpty)
		{
			return cachedMessages ?? [];
		}

		var getChatMessagesResponse = await chatService.GetChatMessagesAsync(userId,
																			 chatId,
																			 cachedMessages?.Count ?? 0,
																			 int.MaxValue,
																			 cancellationToken);

		var newMessages = getChatMessagesResponse.Match(messages => messages,
														_ => []);
		if (cachedMessages is null)
		{
			return newMessages;
		}

		cachedMessages.AddRange(newMessages);

		memoryCache.Set(key, cachedMessages);

		return cachedMessages;
	}
}
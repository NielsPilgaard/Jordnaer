using Jordnaer.Shared;
using ZiggyCreatures.Caching.Fusion;

namespace Jordnaer.Features.Chat;

public interface IChatMessageCache
{
	ValueTask<List<ChatMessageDto>> GetChatMessagesAsync(string userId, Guid chatId,
		CancellationToken cancellationToken = default);
}

public class ChatMessageCache(IChatService chatService, IFusionCache fusionCache) : IChatMessageCache
{
	private static readonly FusionCacheEntryOptions CacheOptions = new() { Duration = TimeSpan.FromDays(7) };

	public async ValueTask<List<ChatMessageDto>> GetChatMessagesAsync(string userId, Guid chatId, CancellationToken cancellationToken = default)
	{
		var key = $"{userId}:chatmessages:{chatId}";

		// Try to get existing cached messages
		var maybeValue = await fusionCache.TryGetAsync<List<ChatMessageDto>>(key, token: cancellationToken);

		if (!maybeValue.HasValue)
		{
			// First time: load all messages
			var getChatMessagesResponse = await chatService.GetChatMessagesAsync(userId, chatId, 0, int.MaxValue, cancellationToken);
			var messages = getChatMessagesResponse.Match(m => m, _ => []);

			await fusionCache.SetAsync(key, messages, CacheOptions, token: cancellationToken);

			return messages;
		}

		var cachedMessages = maybeValue.Value ?? [];

		// Fetch new messages since last cached
		var newMessagesResponse = await chatService.GetChatMessagesAsync(userId, chatId, cachedMessages.Count, int.MaxValue, cancellationToken);
		var newMessages = newMessagesResponse.Match(m => m, _ => []);

		if (newMessages.Count > 0)
		{
			// Create a new list to avoid mutating the cached reference
			var allMessages = new List<ChatMessageDto>(cachedMessages.Count + newMessages.Count);
			allMessages.AddRange(cachedMessages);
			allMessages.AddRange(newMessages);

			await fusionCache.SetAsync(key, allMessages, CacheOptions, token: cancellationToken);

			return allMessages;
		}

		return cachedMessages;
	}
}

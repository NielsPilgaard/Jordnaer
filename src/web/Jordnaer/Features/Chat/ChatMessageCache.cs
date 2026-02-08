using System.Collections.Concurrent;
using System.Collections.Immutable;
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
	private readonly ConcurrentDictionary<Guid, ImmutableList<ChatMessageDto>> _cache = new();
	private readonly ConcurrentDictionary<Guid, Task<ImmutableList<ChatMessageDto>>> _inflightLoads = new();

	public async ValueTask<List<ChatMessageDto>> GetChatMessagesAsync(string userId, Guid chatId, CancellationToken cancellationToken = default)
	{
		if (_cache.TryGetValue(chatId, out _))
		{
			// Reuse the same inflight pattern to serialize incremental loads per chatId,
			// preventing concurrent callers from fetching and appending duplicate messages.
			var refreshTask = _inflightLoads.GetOrAdd(chatId, _ => RefreshMessagesAsync(userId, chatId, cancellationToken));

			try
			{
				var messages = await refreshTask;
				return [.. messages];
			}
			finally
			{
				_inflightLoads.TryRemove(chatId, out _);
			}
		}

		// Use GetOrAdd on _inflightLoads to ensure only one initial load per chatId
		var loadTask = _inflightLoads.GetOrAdd(chatId, _ => LoadMessagesAsync(userId, chatId, cancellationToken));

		try
		{
			var messages = await loadTask;
			return [.. messages];
		}
		finally
		{
			_inflightLoads.TryRemove(chatId, out _);
		}
	}

	private async Task<ImmutableList<ChatMessageDto>> RefreshMessagesAsync(string userId, Guid chatId, CancellationToken cancellationToken)
	{
		var currentMessages = _cache.GetValueOrDefault(chatId, ImmutableList<ChatMessageDto>.Empty);
		var currentCount = currentMessages.Count;

		var newMessagesResponse = await chatService.GetChatMessagesAsync(userId, chatId, currentCount, int.MaxValue, cancellationToken);
		var newMessages = newMessagesResponse.Match(m => m, _ => []);

		if (newMessages.Count > 0)
		{
			_cache.AddOrUpdate(
				chatId,
				_ => [.. newMessages],
				(_, existing) =>
				{
					// Only append messages beyond what we already have to avoid duplicates
					// in case another caller already appended between our read and this update.
					var delta = newMessages.Skip(existing.Count - currentCount).ToList();
					return delta.Count > 0 ? existing.AddRange(delta) : existing;
				});
		}

		return _cache.GetValueOrDefault(chatId, ImmutableList<ChatMessageDto>.Empty);
	}

	private async Task<ImmutableList<ChatMessageDto>> LoadMessagesAsync(string userId, Guid chatId, CancellationToken cancellationToken)
	{
		var getChatMessagesResponse = await chatService.GetChatMessagesAsync(userId: userId,
																	   chatId: chatId,
																	   skip: 0,
																	   take: int.MaxValue,
																	   cancellationToken: cancellationToken);
		var messages = getChatMessagesResponse.Match(
			messages => [.. messages],
			_ => ImmutableList<ChatMessageDto>.Empty);

		_cache[chatId] = messages;
		return messages;
	}
}

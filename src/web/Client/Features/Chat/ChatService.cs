using Jordnaer.Client.Models;
using System.Net;
using Jordnaer.Shared;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor;
using Refit;

namespace Jordnaer.Client.Features.Chat;

public interface IChatService
{
    ValueTask<List<ChatDto>> GetChats(string userId);
    ValueTask<Dictionary<Guid, int>> GetUnreadMessages(string userId);
    ValueTask<List<ChatMessageDto>> GetChatMessages(Guid chatId);
    ValueTask StartChat(StartChat chat);
    ValueTask SendMessage(ChatMessageDto message);
    ValueTask MarkMessagesAsRead(Guid chatId);
}

public class ChatService : IChatService
{
    private readonly IMemoryCache _cache;
    private readonly IChatClient _chatClient;
    private readonly ISnackbar _snackbar;

    public ChatService(IMemoryCache cache, IChatClient chatClient, ISnackbar snackbar)
    {
        _cache = cache;
        _chatClient = chatClient;
        _snackbar = snackbar;
    }

    public async ValueTask<List<ChatDto>> GetChats(string userId)
    {
        string key = $"{userId}-chats";
        bool cacheWasEmpty = false;
        var cachedChats = await _cache.GetOrCreateAsync(key, async entry =>
        {
            cacheWasEmpty = true;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);

            return HandleApiResponse(await _chatClient.GetChats(userId));
        });

        if (cacheWasEmpty)
        {
            return cachedChats ?? new List<ChatDto>();
        }

        var newChats = HandleApiResponse(await _chatClient.GetChats(userId, cachedChats?.Count ?? 0));
        if (cachedChats is null)
        {
            return newChats;
        }

        cachedChats.AddRange(newChats);

        return cachedChats;
    }

    public async ValueTask<Dictionary<Guid, int>> GetUnreadMessages(string userId)
        => HandleApiResponse(await _chatClient.GetUnreadMessages(userId));

    public async ValueTask<List<ChatMessageDto>> GetChatMessages(Guid chatId)
    {
        string key = $"{chatId}-chatmessages";
        bool cacheWasEmpty = false;
        var cachedMessages = await _cache.GetOrCreateAsync(key, async entry =>
        {
            cacheWasEmpty = true;

            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);

            return HandleApiResponse(await _chatClient.GetChatMessages(chatId));
        });

        if (cacheWasEmpty)
        {
            return cachedMessages ?? new List<ChatMessageDto>();
        }
        var newMessages = HandleApiResponse(await _chatClient.GetChatMessages(chatId, cachedMessages?.Count ?? 0));
        if (cachedMessages is null)
        {
            return newMessages;
        }

        cachedMessages.AddRange(newMessages);

        return cachedMessages;
    }

    public async ValueTask StartChat(StartChat chat) => HandleApiResponse(await _chatClient.StartChat(chat));

    public async ValueTask SendMessage(ChatMessageDto message) => HandleApiResponse(await _chatClient.SendMessage(message));

    public async ValueTask MarkMessagesAsRead(Guid chatId) => await _chatClient.MarkMessagesAsRead(chatId);

    /// <summary>
    /// Checks the <c>StatusCode</c> of the <see cref="IApiResponse"/> and shows a popup if <c>IsSuccessStatusCode</c> is false.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="response"></param>
    private T HandleApiResponse<T>(IApiResponse<T> response) where T : new()
    {
        switch (response.StatusCode)
        {
            case { } when response is { IsSuccessStatusCode: true, Content: not null }:
                return response.Content;

            case HttpStatusCode.TooManyRequests:
                _snackbar.Add(ErrorMessages.High_Load, Severity.Info);
                break;

            default:
                _snackbar.Add(ErrorMessages.Something_Went_Wrong_Refresh, Severity.Warning);
                break;
        }

        return new T();
    }


    /// <summary>
    /// Checks the <c>StatusCode</c> of the <see cref="IApiResponse"/> and shows a popup if <c>IsSuccessStatusCode</c> is false.
    /// </summary>
    /// <param name="response"></param>
    private void HandleApiResponse(IApiResponse response)
    {
        switch (response.StatusCode)
        {
            case { } when response.IsSuccessStatusCode:
                return;

            case HttpStatusCode.TooManyRequests:
                _snackbar.Add(ErrorMessages.High_Load, Severity.Info);
                break;

            default:
                _snackbar.Add(ErrorMessages.Something_Went_Wrong_Refresh, Severity.Warning);
                break;
        }
    }
}

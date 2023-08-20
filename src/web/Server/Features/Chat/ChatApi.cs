using System.Linq.Expressions;
using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Server.Extensions;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Features.Chat;

public static class ChatApi
{
    public static RouteGroupBuilder MapChat(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/chat");

        group.RequireAuthorization(builder => builder.RequireCurrentUser());

        group.RequirePerUserRateLimit();

        group.MapGet("{userId}",
            async Task<Results<Ok<List<ChatDto>>, UnauthorizedHttpResult>> (
                    [FromRoute] string userId,
                    [FromQuery] int skip,
                    [FromQuery] int take,
                    [FromServices] CurrentUser currentUser,
                    [FromServices] JordnaerDbContext context,
                        CancellationToken cancellationToken) =>
            {
                if (currentUser.Id != userId)
                {
                    return TypedResults.Unauthorized();
                }

                var chats = await context.Chats
                    .AsNoTracking()
                    .Where(ContainsUsers(userId))
                    .OrderByDescending(chat => chat.LastMessageSentUtc)
                    .Skip(skip)
                    .Take(take)
                    .Include(chat => chat.Recipients)
                    .Select(chat => new ChatDto
                    {
                        DisplayName = chat.DisplayName,
                        Id = chat.Id,
                        LastMessageSentUtc = chat.LastMessageSentUtc,
                        StartedUtc = chat.StartedUtc,
                        Recipients = chat.Recipients.Select(recipient => recipient.ToUserSlim()).ToList(),
                        IsUnread = context.UnreadMessages.Any(unreadMessage => unreadMessage.ChatId == chat.Id &&
                                                                               unreadMessage.RecipientId == userId)
                    })
                    .AsSingleQuery()
                    .ToListAsync(cancellationToken);

                return TypedResults.Ok(chats);
            });
        group.MapGet($"{MessagingConstants.GetChatMessages}/{{chatId:guid}}",
            async Task<Results<Ok<List<ChatMessageDto>>, UnauthorizedHttpResult>> (
                    [FromRoute] Guid chatId,
                    [FromQuery] int skip,
                    [FromQuery] int take,
                    [FromServices] CurrentUser currentUser,
                    [FromServices] JordnaerDbContext context,
                CancellationToken cancellationToken) =>
            {
                if (await CurrentUserIsNotPartOfChat())
                {
                    return TypedResults.Unauthorized();
                }

                async Task<bool> CurrentUserIsNotPartOfChat()
                {
                    var chat = await context.Chats
                        .FirstOrDefaultAsync(chat =>
                            chat.Id == chatId &&
                            chat.Recipients
                                .Select(recipient => recipient.Id)
                                .Contains(currentUser.Id), cancellationToken);

                    // If null, the Chat does not exist, or the current user is not a part of it
                    return chat is null;
                }

                var chatMessages = await context.ChatMessages
                    .AsNoTracking()
                    .Where(message => message.ChatId == chatId)
                    // Oldest messages first, so we get the right order in the chat
                    .OrderBy(message => message.SentUtc)
                    .Skip(skip)
                    .Take(take)
                    .Select(message => message.ToChatMessageDto(new UserSlim
                    {
                        DisplayName = $"{message.Sender.FirstName} {message.Sender.LastName}",
                        Id = message.SenderId,
                        // TODO: only 1 url should be sent per unique chat participant
                        ProfilePictureUrl = message.Sender.ProfilePictureUrl
                    }))
                    .ToListAsync(cancellationToken);

                return TypedResults.Ok(chatMessages);
            });
        group.MapPost(MessagingConstants.StartChat,
            async Task<Results<NoContent, BadRequest, UnauthorizedHttpResult>> (
            [FromBody] StartChat chat,
            [FromServices] CurrentUser currentUser,
            [FromServices] JordnaerDbContext context,
            [FromServices] IPublishEndpoint publishEndpoint,
            CancellationToken cancellationToken) =>
        {
            if (await context.Chats.AsNoTracking().AnyAsync(existingChat => existingChat.Id == chat.Id, cancellationToken))
            {
                return TypedResults.BadRequest();
            }

            if (!chat.Recipients.Select(recipient => recipient.Id).Contains(currentUser.Id))
            {
                return TypedResults.Unauthorized();
            }

            context.Chats.Add(new Shared.Chat
            {
                LastMessageSentUtc = chat.LastMessageSentUtc,
                Id = chat.Id,
                StartedUtc = chat.StartedUtc,
                Messages = chat.Messages.Select(message => message.ToChatMessage(chat.Id)).ToList()
            });

            // Every recipient 
            foreach (var message in chat.Messages)
            {
                foreach (var recipient in chat.Recipients.Where(recipient => recipient.Id != chat.InitiatorId))
                {
                    context.UnreadMessages.Add(new UnreadMessage
                    {
                        SenderId = message.Sender.Id,
                        RecipientId = recipient.Id,
                        ChatId = chat.Id,
                        MessageSentUtc = message.SentUtc
                    });
                }
            }

            context.UserChats.AddRange(chat.Recipients.Select(recipient =>
                new UserChat
                {
                    ChatId = chat.Id,
                    UserProfileId = recipient.Id
                }));

            await context.SaveChangesAsync(cancellationToken);

            // TODO: This should send a message through an exchange to an Azure Function, which does the heavy lifting
            await publishEndpoint.Publish(chat, cancellationToken);

            return TypedResults.NoContent();
        });

        group.MapPost(MessagingConstants.SendMessage, async Task<Results<NoContent, BadRequest, UnauthorizedHttpResult>> (
            [FromBody] SendMessage chatMessage,
            [FromServices] CurrentUser currentUser,
            [FromServices] JordnaerDbContext context,
            [FromServices] IPublishEndpoint publishEndpoint,
            CancellationToken cancellationToken) =>
        {
            if (await context.ChatMessages.AnyAsync(message => message.Id == chatMessage.Id, cancellationToken))
            {
                return TypedResults.BadRequest();
            }

            if (chatMessage.Sender.Id != currentUser.Id)
            {
                return TypedResults.Unauthorized();
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            context.ChatMessages.Add(
                new ChatMessage
                {
                    ChatId = chatMessage.ChatId,
                    Id = chatMessage.Id,
                    SenderId = chatMessage.Sender.Id,
                    Text = chatMessage.Text,
                    AttachmentUrl = chatMessage.AttachmentUrl,
                    SentUtc = chatMessage.SentUtc
                });

            var recipientIds = await context.UserChats
                .Where(userChat => userChat.ChatId == chatMessage.ChatId)
                .Select(userChat => userChat.UserProfileId)
                .ToListAsync(cancellationToken);

            context.UnreadMessages.AddRange(recipientIds.Select(recipientId => new UnreadMessage
            {
                RecipientId = recipientId,
                SenderId = chatMessage.Sender.Id,
                ChatId = chatMessage.ChatId,
                MessageSentUtc = chatMessage.SentUtc
            }));

            await context.Chats
                .ExecuteUpdateAsync(call =>
                    call.SetProperty(chat => chat.LastMessageSentUtc, DateTime.UtcNow),
                    cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // TODO: This should send a message through an exchange to an Azure Function, which does the heavy lifting
            await publishEndpoint.Publish(chatMessage, cancellationToken);

            return TypedResults.NoContent();
        });

        group.MapPut(MessagingConstants.SetChatName, async Task<Results<NoContent, NotFound>> (
            [FromBody] SetChatName setChatName,
            [FromServices] CurrentUser currentUser,
            [FromServices] JordnaerDbContext context,
            [FromServices] IPublishEndpoint publishEndpoint,
            CancellationToken cancellationToken) =>
        {
            if (!await context.Chats.AnyAsync(chat => chat.Id == setChatName.ChatId, cancellationToken))
            {
                return TypedResults.NotFound();
            }

            // TODO: Actually set chat name
            // TODO: This should send a message through an exchange to an Azure Function, which does the heavy lifting
            await publishEndpoint.Publish(setChatName, cancellationToken);

            return TypedResults.NoContent();
        });

        return group;
    }

    private static Expression<Func<Shared.Chat, bool>> ContainsUsers(string userId) =>
        chat => chat.Recipients
            .Select(recipient => recipient.Id)
            .Contains(userId);
}

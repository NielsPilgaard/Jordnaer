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
                    .Include(chat => chat.Messages
                        .OrderByDescending(chatMessage => chatMessage.SentUtc)
                        .Take(1))
                    .ThenInclude(chatMessage => chatMessage.Sender)
                    .Select(chat => new ChatDto
                    {
                        DisplayName = chat.DisplayName,
                        Id = chat.Id,
                        LastMessageSentUtc = chat.LastMessageSentUtc,
                        StartedUtc = chat.StartedUtc,
                        Recipients = chat.Recipients.Select(recipient => recipient.ToUserSlim()).ToList(),
                        Messages = chat.Messages.Select(chatMessage => chatMessage.ToChatMessageDto()).ToList()
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

                var chatMessages = await context.ChatMessages.AsNoTracking()
                    .Where(message => message.ChatId == chatId)
                    .OrderByDescending(message => message.SentUtc)
                    .Skip(skip)
                    .Take(take)
                    .Select(message => message.ToChatMessageDto(new UserSlim
                    {
                        DisplayName = $"{message.Sender.FirstName} {message.Sender.LastName}",
                        Id = message.SenderId,
                        ProfilePictureUrl = message.Sender.ProfilePictureUrl
                    }))
                    .ToListAsync(cancellationToken);


                return TypedResults.Ok(chatMessages);
            });
        group.MapPost(MessagingConstants.StartChat, async Task<Results<NoContent, BadRequest, UnauthorizedHttpResult>> (
            [FromBody] ChatDto chat,
            [FromServices] CurrentUser currentUser,
            [FromServices] JordnaerDbContext context,
            [FromServices] ISendEndpointProvider sendEndpointProvider,
            CancellationToken cancellationToken) =>
        {
            if (await context.Chats.AnyAsync(existingChat => existingChat.Id == chat.Id, cancellationToken))
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

            context.UserChats.AddRange(chat.Recipients.Select(recipient =>
                new UserChat
                {
                    ChatId = chat.Id,
                    UserProfileId = recipient.Id
                }));

            await context.SaveChangesAsync(cancellationToken);

            // TODO: This should send a message through an exchange to an Azure Function, which does the heavy lifting
            var publishEndpoint = await sendEndpointProvider.GetSendEndpoint(
                new Uri($"queue:{MessagingConstants.StartChat}"));
            await publishEndpoint.Send(chat, cancellationToken);

            return TypedResults.NoContent();
        });

        group.MapPost(MessagingConstants.SendMessage, async Task<Results<NoContent, BadRequest, UnauthorizedHttpResult>> (
            [FromBody] ChatMessageDto chatMessage,
            [FromServices] CurrentUser currentUser,
            [FromServices] JordnaerDbContext context,
            [FromServices] ISendEndpointProvider sendEndpointProvider,
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
                    IsDeleted = chatMessage.IsDeleted,
                    SentUtc = chatMessage.SentUtc
                });

            await context.Chats
                .ExecuteUpdateAsync(call =>
                    call.SetProperty(chat => chat.LastMessageSentUtc, DateTime.UtcNow),
                    cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);


            // TODO: This should send a message through an exchange to an Azure Function, which does the heavy lifting
            var publishEndpoint = await sendEndpointProvider.GetSendEndpoint(
                new Uri($"queue:{MessagingConstants.SendMessage}"));
            await publishEndpoint.Send(chatMessage, cancellationToken);

            return TypedResults.NoContent();
        });

        group.MapPut(MessagingConstants.SetChatName, async Task<Results<NoContent, BadRequest, UnauthorizedHttpResult>> (
            [FromBody] ChatMessageDto chatMessage,
            [FromServices] CurrentUser currentUser,
            [FromServices] JordnaerDbContext context,
            [FromServices] ISendEndpointProvider sendEndpointProvider,
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

            // TODO: Actually set chat name
            // TODO: This should send a message through an exchange to an Azure Function, which does the heavy lifting
            var publishEndpoint = await sendEndpointProvider.GetSendEndpoint(
                new Uri($"queue:{MessagingConstants.SetChatName}"));
            await publishEndpoint.Send(chatMessage, cancellationToken);

            return TypedResults.NoContent();
        });

        return group;
    }

    private static Expression<Func<Shared.Chat, bool>> ContainsUsers(string userId) =>
        chat => chat.Recipients
            .Select(recipient => recipient.Id)
            .Contains(userId);
}

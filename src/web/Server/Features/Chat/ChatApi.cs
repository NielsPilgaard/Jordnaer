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
            async Task<Results<Ok<ChatResult>, UnauthorizedHttpResult>>
                ([FromRoute] string userId, [FromQuery] int page, [FromQuery] int pageSize) =>
            {

                return TypedResults.Ok();
            });
        group.MapGet($"{MessagingConstants.GetChatMessages}/{{chatId}}",
            async Task<Results<Ok<ChatMessageResult>, UnauthorizedHttpResult>>
                ([FromRoute] Guid chatId, [FromQuery] int page, [FromQuery] int pageSize) =>
            {

                return TypedResults.Ok();
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
                StartedUtc = chat.StartedUtc
            });

            context.ChatMessages.AddRange(chat.Messages.Select(message =>
                new ChatMessage
                {
                    Id = message.Id,
                    ChatId = chat.Id,
                    SenderId = message.Sender.Id,
                    Text = message.Text,
                    AttachmentUrl = message.AttachmentUrl,
                    IsDeleted = message.IsDeleted,
                    SentUtc = message.SentUtc
                }));

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


            context.ChatMessages.Add(
                new ChatMessage
                {
                    ChatId = chatMessage.Id,
                    SenderId = chatMessage.Sender.Id,
                    Text = chatMessage.Text,
                    AttachmentUrl = chatMessage.AttachmentUrl,
                    IsDeleted = chatMessage.IsDeleted,
                    SentUtc = chatMessage.SentUtc
                });

            await context.SaveChangesAsync(cancellationToken);

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
}

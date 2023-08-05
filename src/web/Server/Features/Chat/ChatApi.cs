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

        // TODO: This should send a message through an exchange to an Azure Function, which does the heavy lifting
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

            var publishEndpoint = await sendEndpointProvider.GetSendEndpoint(
                new Uri($"queue:{MessagingConstants.SendMessage}"));
            await publishEndpoint.Send(chatMessage, cancellationToken);

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

            var publishEndpoint = await sendEndpointProvider.GetSendEndpoint(
                new Uri($"queue:{MessagingConstants.SetChatName}"));
            await publishEndpoint.Send(chatMessage, cancellationToken);

            return TypedResults.NoContent();
        });

        return group;
    }
}

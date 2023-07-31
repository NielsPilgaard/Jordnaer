using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Server.Extensions;
using Jordnaer.Shared.Contracts;
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
        group.MapPost("chat", async Task<Results<NoContent, BadRequest, UnauthorizedHttpResult>> (
            [FromBody] ChatDto chat,
            [FromServices] CurrentUser currentUser,
            [FromServices] JordnaerDbContext context,
            CancellationToken cancellationToken) =>
        {
            if (!chat.Recipients.Select(recipient => recipient.Id).Contains(currentUser.Id))
            {
                return TypedResults.Unauthorized();
            }

            if (await context.Chats.AnyAsync(chat => chat.Id == chat.Id, cancellationToken))
            {
                return TypedResults.BadRequest();
            }

            context.Chats.Add(new Shared.Contracts.Chat
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

            return TypedResults.NoContent();
        });

        group.MapPost("message", async Task<Results<NoContent, BadRequest, UnauthorizedHttpResult>> (
            [FromBody] ChatMessageDto chatMessage,
            [FromServices] CurrentUser currentUser,
            [FromServices] JordnaerDbContext context,
            CancellationToken cancellationToken) =>
        {
            if (chatMessage.Sender.Id != currentUser.Id)
            {
                return TypedResults.Unauthorized();
            }

            if (await context.ChatMessages.AnyAsync(message => message.Id == chatMessage.Id, cancellationToken))
            {
                return TypedResults.BadRequest();
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

            return TypedResults.NoContent();
        });

        return group;
    }
}

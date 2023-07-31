using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Server.Extensions;
using Jordnaer.Shared.Contracts;
using Jordnaer.Shared.Contracts.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Server.Features.Chat;

public static class ChatApi
{
    public static RouteGroupBuilder MapChat(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/chat");

        group.RequireAuthorization(builder => builder.RequireCurrentUser());

        group.RequirePerUserRateLimit();

        // TODO: This should send a message through an exchange to an Azure Function, which does the heavy lifting
        group.MapPost("new-chat", async Task<Results<NoContent, UnauthorizedHttpResult>> (
            [FromBody] ChatDto chatDto,
            [FromServices] CurrentUser currentUser,
            [FromServices] JordnaerDbContext context,
            CancellationToken cancellationToken) =>
        {
            if (!chatDto.Recipients.Select(recipient => recipient.Id).Contains(currentUser.Id))
            {
                return TypedResults.Unauthorized();
            }

            context.Chats.Add(new Shared.Contracts.Chat
            {
                DisplayName = chatDto.GetDisplayName(),
                LastMessageSentUtc = chatDto.LastMessageSentUtc,
                Id = chatDto.Id,
                StartedUtc = chatDto.StartedUtc
            });

            context.ChatMessages.AddRange(chatDto.Messages.Select(message => new ChatMessage
            {
                ChatId = chatDto.Id,
                SenderId = message.Sender.Id,
                Text = message.Text,
                AttachmentUrl = message.AttachmentUrl,
                IsDeleted = message.IsDeleted,
                SentUtc = message.SentUtc
            }));

            context.UserChats.AddRange(chatDto.Recipients.Select(recipient =>
                new UserChat
                {
                    ChatId = chatDto.Id,
                    UserProfileId = recipient.Id
                }));

            await context.SaveChangesAsync(cancellationToken);

            return TypedResults.NoContent();
        });

        return group;
    }
}

namespace Jordnaer.Shared;

public static class ChatDtoExtensions
{
    public static StartChat ToStartChatCommand(this ChatDto chatDto, string initiatorId) =>
        new()
        {
            InitiatorId = initiatorId,
            Id = chatDto.Id,
            Recipients = chatDto.Recipients,
            Messages = chatDto.Messages,
            LastMessageSentUtc = chatDto.LastMessageSentUtc,
            StartedUtc = chatDto.StartedUtc
        };
}

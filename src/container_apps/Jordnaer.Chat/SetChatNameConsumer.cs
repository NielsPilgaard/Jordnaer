using Jordnaer.Shared;
using MassTransit;

namespace Jordnaer.Chat;

public class SetChatNameConsumer : IConsumer<SetChatName>
{
    public Task Consume(ConsumeContext<SetChatName> context) => throw new NotImplementedException();
}

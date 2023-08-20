using Jordnaer.Shared;
using MassTransit;

namespace Jordnaer.Chat;

public class SendMessageConsumer : IConsumer<SendMessage>
{
    public Task Consume(ConsumeContext<SendMessage> context) => throw new NotImplementedException();
}

using Jordnaer.Shared;
using MassTransit;

namespace Jordnaer.Chat;

public class StartChatConsumer : IConsumer<StartChat>
{
    public Task Consume(ConsumeContext<StartChat> context) => throw new NotImplementedException();
}

using Jordnaer.Shared;
using MassTransit;

namespace Jordnaer.Consumers;

public class SetChatNameConsumer : IConsumer<SetChatName>
{
	public Task Consume(ConsumeContext<SetChatName> context)
	{
		throw new NotImplementedException();
	}
}

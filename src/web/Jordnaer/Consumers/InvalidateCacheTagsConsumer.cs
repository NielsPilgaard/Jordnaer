using Jordnaer.Shared;
using MassTransit;
using ZiggyCreatures.Caching.Fusion;

namespace Jordnaer.Consumers;

public class InvalidateCacheTagsConsumer(IFusionCache fusionCache) : IConsumer<InvalidateCacheTags>
{
	public async Task Consume(ConsumeContext<InvalidateCacheTags> context)
	{
		foreach (var tag in context.Message.Tags)
		{
			await fusionCache.RemoveByTagAsync(tag, token: context.CancellationToken);
		}
	}
}

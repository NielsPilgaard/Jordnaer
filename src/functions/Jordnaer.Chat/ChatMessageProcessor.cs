using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Jordnaer.Chat;

public class ChatMessageProcessor
{
    [FunctionName(nameof(ChatMessageProcessor))]
    public void Run(
        [ServiceBusTrigger("jordnaer.chat.message", Connection = "ServiceBus")]
        string myQueueItem, ILogger logger)
    {
        logger.LogInformation("ServiceBus queue trigger function received message: {myQueueItem}", myQueueItem);
    }
}

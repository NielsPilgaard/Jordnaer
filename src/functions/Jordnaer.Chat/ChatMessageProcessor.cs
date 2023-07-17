using Microsoft.Azure.WebJobs;
using ILogger = Serilog.ILogger;

namespace Jordnaer.Chat;

public class ChatMessageProcessor
{
    [FunctionName(nameof(ChatMessageProcessor))]
    public void Run(
        [ServiceBusTrigger("jordnaer.chat.message", Connection = "ConnectionStrings:AzureServiceBus")]
        string myQueueItem, ILogger log)
    {
        log.ForContext("function_name", nameof(ChatMessageProcessor));

        log.Information($"ServiceBus queue trigger function received message: {myQueueItem}");
    }
}

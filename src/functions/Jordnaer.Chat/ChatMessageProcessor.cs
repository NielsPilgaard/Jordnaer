using Microsoft.Azure.WebJobs;
using Serilog;

namespace Jordnaer.Chat;

public class ChatMessageProcessor
{
    [FunctionName(nameof(ChatMessageProcessor))]
    public void Run(
        [ServiceBusTrigger("jordnaer.chat.message", Connection = "ConnectionStrings:AzureServiceBus")]
        string myQueueItem)
    {
        Log.ForContext("function_name", nameof(ChatMessageProcessor));

        Log.Information("ServiceBus queue trigger function received message: {myQueueItem}", myQueueItem);
    }
}

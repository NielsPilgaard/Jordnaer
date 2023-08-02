using Jordnaer.Shared.Contracts;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Jordnaer.Chat;

public class ChatMessageProcessor
{
    [FunctionName(MessagingConstants.SendMessage)]
    public void Run(
        [ServiceBusTrigger(MessagingConstants.SendMessage, Connection = "ServiceBus")]
        ChatMessageDto chatMessage, ILogger logger)
    {
        logger.LogInformation("ServiceBus queue trigger function received message: {myQueueItem}", chatMessage);
    }


    [FunctionName(MessagingConstants.StartChat)]
    public void Run(
        [ServiceBusTrigger(MessagingConstants.StartChat, Connection = "ServiceBus")]
        ChatDto chatMessage, ILogger logger)
    {
        logger.LogInformation("ServiceBus queue trigger function received message: {myQueueItem}", chatMessage);
    }
}

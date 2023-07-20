namespace Jordnaer.Shared.Contracts.Extensions;

public static class ChatExtensions
{
    public static string SetDefaultDisplayName(this Chat chat)
    {
        if (chat.Recipients.TryGetNonEnumeratedCount(out var recipientCount))
        {

        }
    }
}

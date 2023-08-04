using Jordnaer.Shared;

namespace Jordnaer.Client;

public static class ChatDtoExtensions
{
    public static string GetDisplayName(this ChatDto chat, string ownUserId)
    {
        if (chat.DisplayName is not null)
            return chat.DisplayName;

        var recipients = chat.Recipients.Where(recipient => recipient.Id != ownUserId).ToArray();
        if (recipients.Length > 3)
        {
            const int recipientNamesToDisplay = 3;
            return $"{string.Join(", ", recipients
                .Take(recipientNamesToDisplay)
                .Select(e => e.DisplayName))} og {chat.Recipients.Count - recipientNamesToDisplay} andre";
        }

        if (recipients.Length > 1)
            return string.Join(", ", chat.Recipients.Select(e => e.DisplayName));

        if (recipients.Length is 1)
            return recipients[0].DisplayName;

        return string.Empty;
    }
}

namespace Jordnaer.Shared.Contracts.Extensions;

public static class ChatExtensions
{
    public static string GetDisplayName(this Chat chat, string ownUserId)
    {
        if (chat.DisplayName is not null)
        {
            return chat.DisplayName;
        }

        var recipients = chat.Recipients.ToArray();
        if (recipients.Length > 3)
        {
            const int recipientNamesToDisplay = 3;
            return $"{string.Join(", ", recipients
                .Take(recipientNamesToDisplay)
                .Select(e => e.FirstName))} og {chat.Recipients.Count - recipientNamesToDisplay} andre";
        }

        if (recipients.Length > 1)
        {
            return string.Join(", ", chat.Recipients.Select(e => e.FirstName));
        }

        if (recipients.Length is 1)
        {
            return $"{recipients[0].FirstName} {recipients[0].LastName}";
        }

        return string.Empty;
    }

    public static string GetDisplayName(this ChatDto chat)
    {
        if (chat.DisplayName is not null)
        {
            return chat.DisplayName;
        }

        var recipients = chat.Recipients.ToArray();
        if (recipients.Length > 3)
        {
            const int recipientNamesToDisplay = 3;
            return $"{string.Join(", ", recipients
                .Take(recipientNamesToDisplay)
                .Select(e => e.DisplayName))} og {chat.Recipients.Count - recipientNamesToDisplay} andre";
        }

        if (recipients.Length > 1)
        {
            return string.Join(", ", chat.Recipients.Select(e => e.DisplayName));
        }

        if (recipients.Length is 1)
        {
            return recipients[0].DisplayName;
        }

        return string.Empty;
    }
}

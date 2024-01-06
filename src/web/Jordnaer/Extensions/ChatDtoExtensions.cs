using Jordnaer.Shared;

namespace Jordnaer.Extensions;

public static class ChatDtoExtensions
{
	public static string GetChatImage(this ChatDto chat, string currentUserId)
	{
		if (chat.Recipients.Count > 1)
		{
			return chat.Recipients
					   .FirstOrDefault(recipient =>
						   recipient.Id != currentUserId)?.ProfilePictureUrl
				   ?? ProfileConstants.Default_Profile_Picture;
		}

		return chat.Recipients.FirstOrDefault()?.ProfilePictureUrl ?? ProfileConstants.Default_Profile_Picture;
	}
	public static string GetDisplayName(this ChatDto chat, string currentUserId)
	{
		if (chat.DisplayName is not null)
		{
			return chat.DisplayName;
		}

		// If there is only 1 recipient, it's a chat with the current user itself
		if (chat.Recipients.Count is 1)
		{
			return chat.Recipients[0].DisplayName;
		}

		var recipients = chat.Recipients.Where(recipient => recipient.Id != currentUserId).ToArray();
		if (recipients.Length > 3)
		{
			const int recipientNamesToDisplay = 3;
			return $"{string.Join(", ", recipients
				.Take(recipientNamesToDisplay)
				.Select(user => user.DisplayName.Split(' ')[0]))} og {chat.Recipients.Count - recipientNamesToDisplay} andre";
		}

		return string.Join(", ", recipients.Select(user => user.DisplayName));
	}
}

namespace Jordnaer.Features.Email;

/// <summary>
/// Represents an email recipient for Azure Communication Services.
/// </summary>
public class EmailRecipient
{
	public required string Email { get; init; }
	public string? DisplayName { get; init; }

	/// <summary>
	/// Concatenates all non-null recipient lists for logging purposes.
	/// Returns GDPR-compliant masked email addresses.
	/// </summary>
	public static string ConcatRecipients(params IEnumerable<EmailRecipient>?[] recipientLists)
	{
		var allRecipients = recipientLists
							.Where(list => list != null)
							.SelectMany(list => list!)
							.ToList();

		return allRecipients.Count == 0
				   ? "[no recipients]"
				   : string.Join(", ", allRecipients.Select(r => r.ToString()));
	}

	/// <summary>
	/// Returns a GDPR-compliant string representation that masks the email address.
	/// </summary>
	public override string ToString()
	{
		var maskedEmail = MaskedEmail.Mask(Email);

		return string.IsNullOrWhiteSpace(DisplayName)
				   ? maskedEmail
				   : $"{DisplayName} <{maskedEmail}>";
	}
}
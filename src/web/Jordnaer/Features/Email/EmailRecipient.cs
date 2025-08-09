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
		var maskedEmail = MaskEmailAddress(Email);

		return string.IsNullOrWhiteSpace(DisplayName)
				   ? maskedEmail
				   : $"{DisplayName} <{maskedEmail}>";
	}

	private static string MaskEmailAddress(string email)
	{
		if (string.IsNullOrWhiteSpace(email))
		{
			return "[invalid]";
		}

		var atIndex = email.LastIndexOf('@');
		if (atIndex <= 0 || atIndex == email.Length - 1)
		{
			return "[invalid]";
		}

		var localPart = email[..atIndex];
		var domain = email[(atIndex + 1)..];

		// Mask local part - show first character and last character (if length > 2)
		var maskedLocal = localPart.Length switch
		{
			1 => "*",
			2 => $"{localPart[0]}*",
			_ => $"{localPart[0]}***{localPart[^1]}"
		};

		// Mask domain - show first character and keep the TLD
		var dotIndex = domain.LastIndexOf('.');
		if (dotIndex <= 0)
		{
			// No TLD found, just mask most of domain
			var maskedDomain = domain.Length > 1 ? $"{domain[0]}***" : "*";
			return $"{maskedLocal}@{maskedDomain}";
		}

		var domainName = domain[..dotIndex];
		var tld = domain[dotIndex..];
		var maskedDomainName = domainName.Length > 1 ? $"{domainName[0]}***" : "*";

		return $"{maskedLocal}@{maskedDomainName}{tld}";
	}
}
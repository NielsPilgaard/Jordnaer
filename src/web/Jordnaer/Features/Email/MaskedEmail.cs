namespace Jordnaer.Features.Email;

/// <summary>
/// Wraps an email address for safe logging with automatic masking.
/// Use this in log statements to prevent exposing full email addresses.
/// </summary>
public readonly struct MaskedEmail
{
	private readonly string? _value;

	public MaskedEmail(string? value)
	{
		_value = value;
	}

	/// <summary>
	/// Returns a masked representation of the email address for logging.
	/// Example: john.doe@example.com becomes j***e@example.com
	/// </summary>
	public override string ToString() => Mask(_value);

	/// <summary>
	/// Implicitly converts a string to a MaskedEmail.
	/// </summary>
	public static implicit operator MaskedEmail(string? value) => new(value);

	/// <summary>
	/// Masks an email address for GDPR-compliant logging.
	/// Made public static for reuse across the application.
	/// </summary>
	public static string Mask(string? email)
	{
		if (string.IsNullOrWhiteSpace(email))
		{
			return "[no email]";
		}

		var atIndex = email.LastIndexOf('@');
		if (atIndex <= 0 || atIndex == email.Length - 1)
		{
			return "[invalid email]";
		}

		var localPart = email[..atIndex];
		var domain = email[(atIndex + 1)..];

		// Mask local part - show first and last character
		var maskedLocal = localPart.Length switch
		{
			1 => "*",
			2 => $"{localPart[0]}*",
			_ => $"{localPart[0]}***{localPart[^1]}"
		};

		return $"{maskedLocal}@{domain}";
	}
}

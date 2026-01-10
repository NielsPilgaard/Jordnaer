using Jordnaer.Features.Email;
using Xunit;

namespace Jordnaer.Tests.Features.Email;

public class EmailRecipientTests
{
	[Theory]
	[InlineData("john.doe@example.com", "j***e@example.com")]
	[InlineData("a@b.co", "*@b.co")]
	[InlineData("ab@example.com", "a*@example.com")]
	[InlineData("test@domain.org", "t***t@domain.org")]
	[InlineData("user@sub.domain.com", "u***r@sub.domain.com")]
	[InlineData("x@y.z", "*@y.z")]
	[InlineData("verylongemailaddress@example.com", "v***s@example.com")]
	public void MaskedEmail_Mask_ShouldMaskCorrectly(string input, string expected)
	{
		// Act
		var result = MaskedEmail.Mask(input);

		// Assert
		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(null)]
	public void MaskedEmail_Mask_ShouldReturnNoEmail_WhenEmailIsNullOrWhitespace(string? input)
	{
		// Act
		var result = MaskedEmail.Mask(input!);

		// Assert
		Assert.Equal("[no email]", result);
	}

	[Theory]
	[InlineData("invalid")]
	[InlineData("@example.com")]
	[InlineData("test@")]
	[InlineData("test")]
	public void MaskedEmail_Mask_ShouldReturnInvalidEmail_WhenEmailHasInvalidFormat(string input)
	{
		// Act
		var result = MaskedEmail.Mask(input);

		// Assert
		Assert.Equal("[invalid email]", result);
	}

	[Theory]
	[InlineData("test@nodomain", "t***t@nodomain")]
	[InlineData("a@b", "*@b")]
	public void MaskedEmail_Mask_ShouldKeepDomainUnmasked(string input, string expected)
	{
		// Act
		var result = MaskedEmail.Mask(input);

		// Assert
		Assert.Equal(expected, result);
	}

	[Fact]
	public void ToString_ShouldReturnMaskedEmail_WhenDisplayNameIsNull()
	{
		// Arrange
		var recipient = new EmailRecipient
		{
			Email = "test@example.com",
			DisplayName = null
		};

		// Act
		var result = recipient.ToString();

		// Assert
		Assert.Equal("t***t@example.com", result);
	}

	[Fact]
	public void ToString_ShouldReturnMaskedEmailWithDisplayName_WhenDisplayNameIsProvided()
	{
		// Arrange
		var recipient = new EmailRecipient
		{
			Email = "test@example.com",
			DisplayName = "Test User"
		};

		// Act
		var result = recipient.ToString();

		// Assert
		Assert.Equal("Test User <t***t@example.com>", result);
	}

	[Fact]
	public void ConcatRecipients_ShouldReturnMaskedEmails_WhenRecipientsExist()
	{
		// Arrange
		var recipients = new List<EmailRecipient>
		{
			new() { Email = "user1@example.com", DisplayName = "User One" },
			new() { Email = "user2@example.com", DisplayName = "User Two" }
		};

		// Act
		var result = EmailRecipient.ConcatRecipients(recipients);

		// Assert
		Assert.Contains("User One <u***1@example.com>", result);
		Assert.Contains("User Two <u***2@example.com>", result);
	}

	[Fact]
	public void ConcatRecipients_ShouldReturnNoRecipients_WhenListIsEmpty()
	{
		// Arrange
		var recipients = new List<EmailRecipient>();

		// Act
		var result = EmailRecipient.ConcatRecipients(recipients);

		// Assert
		Assert.Equal("[no recipients]", result);
	}

	[Fact]
	public void ConcatRecipients_ShouldHandleNullLists()
	{
		// Act
		var result = EmailRecipient.ConcatRecipients(null, null);

		// Assert
		Assert.Equal("[no recipients]", result);
	}

	[Fact]
	public void ConcatRecipients_ShouldCombineMultipleLists()
	{
		// Arrange
		var toRecipients = new List<EmailRecipient>
		{
			new() { Email = "to@example.com" }
		};
		var bccRecipients = new List<EmailRecipient>
		{
			new() { Email = "bcc@example.com" }
		};

		// Act
		var result = EmailRecipient.ConcatRecipients(toRecipients, bccRecipients);

		// Assert
		Assert.Contains("t*@example.com", result);
		Assert.Contains("b***c@example.com", result);
	}
}

using Jordnaer.Features.Email;
using Xunit;

namespace Jordnaer.Tests.Features.Email;

public class MaskedEmailTests
{
	[Theory]
	[InlineData("john.doe@example.com", "j***e@example.com")]
	[InlineData("a@b.co", "*@b.co")]
	[InlineData("ab@example.com", "a*@example.com")]
	[InlineData("test@domain.org", "t***t@domain.org")]
	[InlineData("user@sub.domain.com", "u***r@sub.domain.com")]
	[InlineData("x@y.z", "*@y.z")]
	public void ToString_ShouldMaskEmailLocalPart(string input, string expectedMask)
	{
		// Arrange
		var email = new MaskedEmail(input);

		// Act
		var result = email.ToString();

		// Assert
		Assert.Equal(expectedMask, result);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(null)]
	public void ToString_ShouldReturnNoEmail_WhenEmailIsNullOrWhitespace(string? input)
	{
		// Arrange
		var email = new MaskedEmail(input);

		// Act
		var result = email.ToString();

		// Assert
		Assert.Equal("[no email]", result);
	}

	[Theory]
	[InlineData("invalid")]
	[InlineData("@example.com")]
	[InlineData("test@")]
	public void ToString_ShouldReturnInvalidEmail_ForInvalidFormats(string input)
	{
		// Arrange
		var email = new MaskedEmail(input);

		// Act
		var result = email.ToString();

		// Assert
		Assert.Equal("[invalid email]", result);
	}

	[Fact]
	public void ImplicitConversion_FromString_ShouldWork()
	{
		// Arrange
		MaskedEmail email = "test@example.com";

		// Act
		var result = email.ToString();

		// Assert
		Assert.Equal("t***t@example.com", result);
	}
}

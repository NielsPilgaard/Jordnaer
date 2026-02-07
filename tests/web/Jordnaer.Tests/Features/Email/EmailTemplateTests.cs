using FluentAssertions;
using Jordnaer.Features.Email;
using Xunit;

namespace Jordnaer.Tests.Features.Email;

public class EmailTemplateTests
{
	[Fact]
	public void Button_ShouldHtmlEncodeHref_WithAmpersandInQueryString()
	{
		// Arrange - URL with multiple query parameters (& between them)
		var url = "https://example.com/confirm?userId=abc&code=xyz";

		// Act
		var result = EmailTemplate.Button(url, "Click");

		// Assert - href should contain &amp; (proper HTML encoding of &)
		result.Should().Contain("href=\"https://example.com/confirm?userId=abc&amp;code=xyz\"");
	}

	[Fact]
	public void Button_ShouldDoubleEncode_WhenHrefIsAlreadyHtmlEncoded()
	{
		// Arrange - URL that is already HTML-encoded (this was the bug scenario)
		// Callers should NOT pre-encode URLs - Button handles encoding internally
		var alreadyEncodedUrl = "https://example.com/confirm?userId=abc&amp;code=xyz";

		// Act
		var result = EmailTemplate.Button(alreadyEncodedUrl, "Click");

		// Assert - passing a pre-encoded URL WILL double-encode, producing broken links.
		// This is expected: callers must pass raw URLs, and Button will HTML-encode them.
		result.Should().Contain("&amp;amp;", "pre-encoded URLs get double-encoded - callers must pass raw URLs");
	}

	[Fact]
	public void Button_ShouldProduceValidHref_ForUrlWithMultipleQueryParameters()
	{
		// Arrange
		var url = "https://localhost:7116/Account/ConfirmEmail?userId=e36897dd-f9bb-4a4e-9512-7de47515814f&code=Q2ZESjhOL1VXRThk";

		// Act
		var result = EmailTemplate.Button(url, "Bekræft din konto");

		// Assert - the href should have properly encoded ampersand
		result.Should().Contain("href=\"https://localhost:7116/Account/ConfirmEmail?userId=e36897dd-f9bb-4a4e-9512-7de47515814f&amp;code=Q2ZESjhOL1VXRThk\"");
		// And should NOT contain unencoded & followed by code (without amp;)
		// The &amp; is correct HTML - browsers decode it to & when following the link
	}

	[Fact]
	public void Button_ShouldHtmlEncodeButtonText()
	{
		// Arrange
		var url = "https://example.com";

		// Act
		var result = EmailTemplate.Button(url, "Click <here> & confirm");

		// Assert
		result.Should().Contain("Click &lt;here&gt; &amp; confirm");
	}

	[Fact]
	public void Button_ShouldUseDefaultBackgroundColor_WhenNoneProvided()
	{
		// Act
		var result = EmailTemplate.Button("https://example.com", "Click");

		// Assert
		result.Should().Contain("background-color: #dbab45");
	}

	[Fact]
	public void Button_ShouldUseCustomBackgroundColor_WhenValidHexProvided()
	{
		// Act
		var result = EmailTemplate.Button("https://example.com", "Delete", backgroundColor: "#a94442");

		// Assert
		result.Should().Contain("background-color: #a94442");
	}

	[Fact]
	public void Button_ShouldFallBackToDefaultColor_WhenInvalidHexProvided()
	{
		// Act
		var result = EmailTemplate.Button("https://example.com", "Click", backgroundColor: "not-a-color");

		// Assert
		result.Should().Contain("background-color: #dbab45");
	}

	[Fact]
	public void Button_ShouldHandleUrlWithThreeQueryParameters()
	{
		// Arrange - mimics the real confirmation URL with userId, code, and returnUrl
		var url = "https://example.com/Account/ConfirmEmail?userId=123&code=abc&returnUrl=/home";

		// Act
		var result = EmailTemplate.Button(url, "Confirm");

		// Assert
		result.Should().Contain("userId=123&amp;code=abc&amp;returnUrl=/home");
		result.Should().NotContain("&amp;amp;");
	}

	[Fact]
	public void Wrap_ShouldContainBodyContent()
	{
		// Act
		var result = EmailTemplate.Wrap("<p>Test content</p>", "https://example.com");

		// Assert
		result.Should().Contain("<p>Test content</p>");
	}

	[Fact]
	public void Wrap_ShouldContainLogoUrl()
	{
		// Act
		var result = EmailTemplate.Wrap("<p>Test</p>", "https://mini-moeder.dk");

		// Assert
		result.Should().Contain("https://mini-moeder.dk/images/minimoeder_logo_payoff.png");
	}

	[Fact]
	public void Wrap_ShouldIncludePreheaderText_WhenProvided()
	{
		// Act
		var result = EmailTemplate.Wrap("<p>Test</p>", preheaderText: "Preview text");

		// Assert
		result.Should().Contain("Preview text");
	}

	[Fact]
	public void Wrap_ShouldHtmlEncodePreheaderText()
	{
		// Act
		var result = EmailTemplate.Wrap("<p>Test</p>", preheaderText: "Text with <html> & entities");

		// Assert
		result.Should().Contain("Text with &lt;html&gt; &amp; entities");
	}

	[Fact]
	public void GetLogoUrl_ShouldReturnDefaultUrl_WhenBaseUrlIsNull()
	{
		// Act
		var result = EmailTemplate.GetLogoUrl(null);

		// Assert
		result.Should().Be("https://mini-moeder.dk/images/minimoeder_logo_payoff.png");
	}

	[Fact]
	public void GetLogoUrl_ShouldTrimTrailingSlash()
	{
		// Act
		var result = EmailTemplate.GetLogoUrl("https://example.com/");

		// Assert
		result.Should().Be("https://example.com/images/minimoeder_logo_payoff.png");
	}
}

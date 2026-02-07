using FluentAssertions;
using Jordnaer.Features.Email;
using Xunit;

namespace Jordnaer.Tests.Features.Email;

public class EmailContentBuilderTests
{
	private const string BaseUrl = "https://mini-moeder.dk";

	[Fact]
	public void Confirmation_ShouldNotDoubleEncodeLink()
	{
		// Arrange - a realistic confirmation link with query parameters
		var confirmationLink = "https://mini-moeder.dk/Account/ConfirmEmail?userId=e36897dd-f9bb-4a4e-9512-7de47515814f&code=Q2ZESjhOL1VXRThk";

		// Act
		var result = EmailContentBuilder.Confirmation(BaseUrl, "TestUser", confirmationLink);

		// Assert - should contain &amp; (proper HTML encoding) but NOT &amp;amp; (double encoding)
		result.Should().Contain("&amp;code=");
		result.Should().NotContain("&amp;amp;");
	}

	[Fact]
	public void Confirmation_ShouldContainGreeting_WithUserName()
	{
		// Act
		var result = EmailContentBuilder.Confirmation(BaseUrl, "Niels", "https://example.com/confirm");

		// Assert
		result.Should().Contain("Hej Niels,");
	}

	[Fact]
	public void Confirmation_ShouldContainGreeting_WithoutUserName()
	{
		// Act
		var result = EmailContentBuilder.Confirmation(BaseUrl, null, "https://example.com/confirm");

		// Assert
		result.Should().Contain("Hej,");
	}

	[Fact]
	public void PasswordResetLink_ShouldNotDoubleEncodeLink()
	{
		// Arrange
		var resetLink = "https://mini-moeder.dk/Account/ResetPassword?code=abc123&userId=def456";

		// Act
		var result = EmailContentBuilder.PasswordResetLink(BaseUrl, "TestUser", resetLink);

		// Assert
		result.Should().Contain("&amp;userId=");
		result.Should().NotContain("&amp;amp;");
	}

	[Fact]
	public void PasswordResetCode_ShouldHtmlEncodeResetCode()
	{
		// Arrange - a code with special characters
		var resetCode = "abc<>&123";

		// Act
		var result = EmailContentBuilder.PasswordResetCode(BaseUrl, "TestUser", resetCode);

		// Assert
		result.Should().Contain("abc&lt;&gt;&amp;123");
		result.Should().NotContain("abc<>&123");
	}

	[Fact]
	public void GroupInvite_ShouldHtmlEncodeGroupName()
	{
		// Arrange
		var groupName = "Forældre & Børn <Test>";

		// Act
		var result = EmailContentBuilder.GroupInvite(BaseUrl, groupName);

		// Assert - raw HTML-unsafe characters should not appear unencoded in the body
		result.Should().NotContain("<b>Forældre & Børn <Test></b>");
		result.Should().Contain("&amp; B");
		result.Should().Contain("&lt;Test&gt;");
	}

	[Fact]
	public void GroupInvite_ShouldUrlEncodeGroupNameInLink()
	{
		// Arrange
		var groupName = "Test Group";

		// Act
		var result = EmailContentBuilder.GroupInvite(BaseUrl, groupName);

		// Assert - the URL should use Uri.EscapeDataString for the group name
		var containsEncoded = result.Contains("groups/Test+Group") || result.Contains("groups/Test%20Group");
		containsEncoded.Should().BeTrue("group name should be URL-encoded in the link");
	}

	[Fact]
	public void GroupInviteNewUser_ShouldNotDoubleEncodeInviteTokenUrl()
	{
		// Arrange
		var inviteToken = "token123&extra=value";

		// Act
		var result = EmailContentBuilder.GroupInviteNewUser(BaseUrl, "Test Group", inviteToken);

		// Assert - the token should be URL-encoded in the URL, then HTML-encoded in the href
		result.Should().NotContain("&amp;amp;");
	}

	[Fact]
	public void ChatNotification_ShouldHtmlEncodeSenderName()
	{
		// Arrange
		var senderName = "User <script>alert('xss')</script>";

		// Act
		var result = EmailContentBuilder.ChatNotification(BaseUrl, "Recipient", senderName, "https://example.com/chat/123");

		// Assert
		result.Should().NotContain("<script>");
	}

	[Fact]
	public void DeleteUser_ShouldNotDoubleEncodeLink()
	{
		// Arrange
		var deletionLink = "https://mini-moeder.dk/delete-user/token123?param1=a&param2=b";

		// Act
		var result = EmailContentBuilder.DeleteUser(BaseUrl, deletionLink);

		// Assert
		result.Should().NotContain("&amp;amp;");
	}

	[Fact]
	public void GroupPostNotification_ShouldHtmlEncodeAuthorNameAndPreview()
	{
		// Arrange
		var authorName = "User<br>Name";
		var postPreview = "Hello & welcome <everyone>";

		// Act
		var result = EmailContentBuilder.GroupPostNotification(BaseUrl, authorName, postPreview, "https://example.com/group");

		// Assert
		result.Should().NotContain("<br>Name</b>"); // author name should be encoded
		result.Should().NotContain("<everyone>"); // preview should be encoded
	}

	[Fact]
	public void MembershipRequest_ShouldProduceValidUrl()
	{
		// Act
		var result = EmailContentBuilder.MembershipRequest(BaseUrl, "Test Group");

		// Assert
		result.Should().Contain("groups/Test");
		result.Should().NotContain("&amp;amp;");
	}

	[Fact]
	public void PartnerContactForm_ShouldHtmlEncodeAllUserInput()
	{
		// Act
		var result = EmailContentBuilder.PartnerContactForm(
			BaseUrl,
			companyName: "<script>alert(1)</script>",
			contactPersonName: "Name & Co",
			email: "test@example.com",
			phoneNumber: "12345678",
			message: "Hello <world>");

		// Assert
		result.Should().NotContain("<script>");
		result.Should().Contain("&amp; Co");
		result.Should().Contain("&lt;world&gt;");
	}

	[Fact]
	public void PartnerImageApproval_ShouldHtmlEncodePartnerNameAndChanges()
	{
		// Arrange
		var changes = new List<string> { "New image: <img src=x>", "Name & description" };

		// Act
		var result = EmailContentBuilder.PartnerImageApproval(BaseUrl, "Partner<Co>", Guid.NewGuid(), changes);

		// Assert
		result.Should().NotContain("<img src=x>");
		result.Should().NotContain("Partner<Co>");
	}

	[Fact]
	public void PartnerWelcome_ShouldHtmlEncodeCredentials()
	{
		// Act
		var result = EmailContentBuilder.PartnerWelcome(BaseUrl, "Partner & Co", "test@example.com", "p@ss<word>");

		// Assert
		result.Should().Contain("Partner &amp; Co");
		result.Should().Contain("p@ss&lt;word&gt;");
	}

	[Fact]
	public void AllEmailTypes_ShouldWrapInHtmlDocument()
	{
		// Test that all email builders produce valid wrapped HTML
		var emails = new[]
		{
			EmailContentBuilder.Confirmation(BaseUrl, "User", "https://example.com/confirm?a=1&b=2"),
			EmailContentBuilder.PasswordResetLink(BaseUrl, "User", "https://example.com/reset?a=1&b=2"),
			EmailContentBuilder.PasswordResetCode(BaseUrl, "User", "code123"),
			EmailContentBuilder.GroupInvite(BaseUrl, "Group"),
			EmailContentBuilder.GroupInviteNewUser(BaseUrl, "Group", "token"),
			EmailContentBuilder.ChatNotification(BaseUrl, "Recipient", "Sender", "https://example.com/chat"),
			EmailContentBuilder.DeleteUser(BaseUrl, "https://example.com/delete"),
			EmailContentBuilder.GroupPostNotification(BaseUrl, "Author", "Preview", "https://example.com/group"),
			EmailContentBuilder.MembershipRequest(BaseUrl, "Group"),
			EmailContentBuilder.PartnerContactForm(BaseUrl, "Company", "Contact", "email@test.com", "123", "Message"),
			EmailContentBuilder.PartnerImageApproval(BaseUrl, "Partner", Guid.NewGuid(), ["Change 1"]),
			EmailContentBuilder.PartnerWelcome(BaseUrl, "Partner", "email@test.com", "password")
		};

		foreach (var email in emails)
		{
			email.Should().Contain("<!DOCTYPE html>");
			email.Should().Contain("</html>");
			email.Should().NotContain("&amp;amp;", "no email should contain double-encoded ampersands");
		}
	}
}

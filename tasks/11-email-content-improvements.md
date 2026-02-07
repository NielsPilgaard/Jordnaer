# Task: Email Content Improvements - Standard Layout, Branding & Styling

## Objective
Implement a consistent, branded email template system for all emails sent by the Mini Møder application. All emails should have a professional appearance with the company logo, consistent typography, and a unified visual style.

## Background

### Current State
Currently, emails are generated as plain HTML strings with minimal styling:
- No logo or header
- No consistent layout wrapper
- Inconsistent signature usage (some use `EmailConstants.Signature`, one uses inline signature)
- No CSS styling or inline styles for fonts/colors
- Links are plain `<a>` tags without button styling

### Branding Assets & Design System
From `src/web/Jordnaer/wwwroot/css/app.css` and `fonts.css`:

**Brand Colors:**
- `--color-glaede: #dbab45` (Joy - primary yellow-orange)
- `--color-ro: #878e64` (Calm - secondary green)
- `--color-moede: #41556b` (Meeting - body text blue)
- `--color-moede-red: #673417` (Small text/quotes red-brown)
- `--color-omsorg: #cfc1a6` (Care - light beige background)
- `--color-leg: #a9c0cf` (Play - light blue background)

**Typography:**
- Headings: `Cherry Bomb One` font (fun, playful)
- Body: `Open Sans Light/Medium/Bold` (clean, readable)
- Heading letter-spacing: 0.11em

**Logo:**
- Main logo: `/images/minimoeder_logo.png`
- Logo with payoff: `/images/minimoeder_logo_payoff.png`
- Small logo: `/images/mini_logo_small.png`

## Implementation Plan

### 1. Create Email Template Helper Class

Create a new file: `src/web/Jordnaer/Features/Email/EmailTemplate.cs`

This class should provide:

```csharp
public static class EmailTemplate
{
    /// <summary>
    /// Wraps email content in the standard Mini Møder email layout with header, logo, and footer.
    /// </summary>
    /// <param name="bodyContent">The main HTML content of the email (without wrapper)</param>
    /// <param name="preheaderText">Optional preview text shown in email clients</param>
    /// <returns>Complete HTML email with layout</returns>
    public static string Wrap(string bodyContent, string? preheaderText = null)
    {
        // Returns full HTML document with:
        // - DOCTYPE and html/head/body structure
        // - Inline CSS for email client compatibility
        // - Header with logo (hosted URL, not local path)
        // - Content area with proper padding
        // - Footer with signature and unsubscribe info
    }
}
```

**Template Requirements:**
1. **Email-safe HTML**: Use tables for layout (email clients don't support flexbox/grid)
2. **Inline CSS**: All styles must be inline (many email clients strip `<style>` blocks)
3. **Hosted Logo URL**: Use `{baseUrl}/images/minimoeder_logo_payoff.png` - the logo must be accessible via HTTPS URL, not embedded
4. **Max Width**: 600px container (standard email width)
5. **Background**: Light background (`#f5f5f5`) with white content area
6. **Fonts**: Use web-safe fallbacks: `'Open Sans', Arial, sans-serif` for body
7. **Colors**: Use the brand colors defined above
8. **Footer**: Include the signature and optionally a small "Denne email blev sendt fra Mini Møder" note

**Example structure:**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Mini Møder</title>
</head>
<body style="margin: 0; padding: 0; background-color: #f5f5f5; font-family: 'Open Sans', Arial, sans-serif;">
    <!-- Preheader text (hidden) -->
    <div style="display: none; max-height: 0; overflow: hidden;">
        {preheaderText}
    </div>

    <!-- Main container -->
    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color: #f5f5f5;">
        <tr>
            <td align="center" style="padding: 20px 10px;">
                <!-- Content wrapper -->
                <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="background-color: #ffffff; border-radius: 8px;">
                    <!-- Header with logo -->
                    <tr>
                        <td align="center" style="padding: 30px 40px 20px;">
                            <img src="{logoUrl}" alt="Mini Møder" width="200" style="display: block;">
                        </td>
                    </tr>

                    <!-- Body content -->
                    <tr>
                        <td style="padding: 20px 40px 30px; color: #41556b; font-size: 16px; line-height: 1.6;">
                            {bodyContent}
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style="padding: 20px 40px; background-color: #f9f9f9; border-radius: 0 0 8px 8px; color: #666; font-size: 14px;">
                            <p style="margin: 0;">Venlig hilsen,<br>Mini Møder Teamet</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
```

### 2. Update EmailConstants.cs

Location: `src/web/Jordnaer/Features/Email/EmailConstants.cs`

**Changes needed:**
- Remove the `Signature` constant (it will be part of the template wrapper)
- Add `LogoUrl` property that uses the base URL
- Consider adding a method to get the full logo URL: `GetLogoUrl(string baseUrl)`

### 3. Add Button Styling Helper

Add to `EmailTemplate.cs`:

```csharp
/// <summary>
/// Creates a styled CTA button for emails
/// </summary>
public static string Button(string href, string text, string? backgroundColor = null)
{
    var bgColor = backgroundColor ?? "#dbab45"; // Default to brand yellow
    return $"""
        <table role="presentation" cellspacing="0" cellpadding="0">
            <tr>
                <td style="border-radius: 6px; background-color: {bgColor};">
                    <a href="{href}" style="display: inline-block; padding: 12px 24px; color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px;">
                        {text}
                    </a>
                </td>
            </tr>
        </table>
        """;
}
```

### 4. Update All Email Senders

The following files need to be updated to use `EmailTemplate.Wrap()`:

#### 4.1 EmailSender.cs
Location: `src/web/Jordnaer/Features/Email/EmailSender.cs`

**Methods to update:**
- `SendConfirmationLinkAsync` - wrap content, use button for link
- `SendPasswordResetLinkAsync` - wrap content, use button for link
- `SendPasswordResetCodeAsync` - wrap content

**Note:** This class needs access to `IOptions<AppOptions>` to get the base URL for logo. Inject it in the constructor.

#### 4.2 EmailService.cs
Location: `src/web/Jordnaer/Features/Email/EmailService.cs`

**Methods to update:**
- `SendEmailFromContactForm` - wrap content (but this goes to admin, may not need branding)
- `SendEmailFromPartnerContactForm` - wrap content (admin email)
- `SendMembershipRequestEmails` - wrap content, use button for link
- `SendGroupInviteEmail` - wrap content, use button for link
- `SendGroupInviteEmailToNewUserAsync` - wrap content, use button for link
- `SendPartnerImageApprovalEmailAsync` - wrap content (admin email)
- `SendPartnerWelcomeEmailAsync` - wrap content, use button for login link

#### 4.3 GroupPostCreatedConsumer.cs
Location: `src/web/Jordnaer/Consumers/GroupPostCreatedConsumer.cs`

**Method to update:**
- `CreateNewPostEmailContent` - wrap content, use button for "Se opslaget" link, keep blockquote styling

#### 4.4 ChatNotificationService.cs
Location: `src/web/Jordnaer/Features/Chat/ChatNotificationService.cs`

**Method to update:**
- `CreateNewChatEmailMessage` - wrap content, use button for "Læs besked" link

#### 4.5 DeleteUserService.cs
Location: `src/web/Jordnaer/Features/DeleteUser/DeleteUserService.cs`

**Method to update:**
- `CreateDeleteUserEmailMessage` - wrap content, use button (possibly in warning color like red/orange)
- Note: This method currently has its own inline signature - remove it and rely on the template footer

### 5. Inject Base URL Where Needed

Several classes already have `IOptions<AppOptions>` injected. For those that don't:

- `EmailSender` - needs `IOptions<AppOptions>` added
- `EmailTemplate` - should receive `baseUrl` as parameter to `Wrap()` method

### 6. Update Tests

Location: `tests/web/Jordnaer.Tests/Email/EmailServiceTests.cs`

Update tests to verify:
- Emails contain the logo URL
- Emails are wrapped in the template structure
- Button links are properly styled

## Files to Create/Modify

| File | Action | Description |
|------|--------|-------------|
| `src/web/Jordnaer/Features/Email/EmailTemplate.cs` | CREATE | New email template wrapper class |
| `src/web/Jordnaer/Features/Email/EmailConstants.cs` | MODIFY | Remove Signature, add logo URL helper |
| `src/web/Jordnaer/Features/Email/EmailSender.cs` | MODIFY | Use template wrapper, add button styling |
| `src/web/Jordnaer/Features/Email/EmailService.cs` | MODIFY | Use template wrapper, add button styling |
| `src/web/Jordnaer/Consumers/GroupPostCreatedConsumer.cs` | MODIFY | Use template wrapper |
| `src/web/Jordnaer/Features/Chat/ChatNotificationService.cs` | MODIFY | Use template wrapper |
| `src/web/Jordnaer/Features/DeleteUser/DeleteUserService.cs` | MODIFY | Use template wrapper, remove inline signature |
| `tests/web/Jordnaer.Tests/Email/EmailServiceTests.cs` | MODIFY | Update assertions for new template structure |
| `tests/web/Jordnaer.Tests/Email/EmailPreviewHelper.cs` | CREATE | Helper to save email HTML as preview files |
| `tests/web/Jordnaer.Tests/Email/EmailPreviewTests.cs` | CREATE | Tests that generate visual previews for all email types |
| `.gitignore` | MODIFY | Exclude email preview output files |

## Implementation Notes

1. **Email Client Compatibility**: Use tables, inline styles, and web-safe fonts. Test in multiple email clients if possible.

2. **Logo Hosting**: The logo URL must be absolute (https://mini-moeder.dk/images/...) for it to display in emails. Use `IOptions<AppOptions>.BaseUrl` to construct this.

3. **Preserve Existing Functionality**: The email content should remain the same - we're just wrapping it in a nicer template.

4. **Danish Language**: Keep all text in Danish as it currently is.

5. **Admin Emails**: For emails that go to `kontakt@mini-moeder.dk` (contact forms, approval requests), the branding is still appropriate but optional. Use your judgment.

6. **Blockquote Styling**: The group post notification already has blockquote styling - preserve this within the template.

### 7. Email Content Preview in Tests

Add the ability to visually inspect rendered email HTML during development and test runs.

#### 7.1 Create Email Preview Test Helper

Create: `tests/web/Jordnaer.Tests/Email/EmailPreviewHelper.cs`

```csharp
public static class EmailPreviewHelper
{
    private static readonly string PreviewOutputDir = Path.Combine(
        TestContext.CurrentContext?.TestDirectory ?? AppContext.BaseDirectory,
        "email-previews");

    /// <summary>
    /// Saves the rendered email HTML to a file that can be opened in a browser.
    /// Also writes the file path to test output for easy access.
    /// </summary>
    public static string SavePreview(string htmlContent, string emailName)
    {
        Directory.CreateDirectory(PreviewOutputDir);

        var fileName = $"{emailName}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
        var filePath = Path.Combine(PreviewOutputDir, fileName);

        File.WriteAllText(filePath, htmlContent);

        // Write to test output so the path is visible in test results
        TestContext.Out?.WriteLine($"Email preview saved: {filePath}");

        return filePath;
    }
}
```

#### 7.2 Create Email Preview Tests

Create: `tests/web/Jordnaer.Tests/Email/EmailPreviewTests.cs`

These tests generate preview HTML files for every email type. They serve two purposes:
1. **Visual verification** - open the saved `.html` files in a browser to see how emails look
2. **Regression detection** - test output shows the full HTML, making it easy to spot unintended changes

```csharp
[TestFixture]
public class EmailPreviewTests
{
    [Test]
    public void Preview_ConfirmationEmail()
    {
        var body = EmailTemplate.Wrap(
            "<p>Bekræft venligst din email ved at klikke på knappen nedenfor.</p>" +
            EmailTemplate.Button("https://mini-moeder.dk/confirm?code=abc123", "Bekræft Email"),
            preheaderText: "Bekræft din Mini Møder konto");

        var path = EmailPreviewHelper.SavePreview(body, "confirmation");
        TestContext.Out.WriteLine(body); // Also write HTML to test output
        Assert.That(body, Does.Contain("Bekræft Email"));
    }

    [Test]
    public void Preview_PasswordResetEmail() { /* ... */ }

    [Test]
    public void Preview_GroupInviteEmail() { /* ... */ }

    [Test]
    public void Preview_ChatNotificationEmail() { /* ... */ }

    [Test]
    public void Preview_DeleteUserEmail() { /* ... */ }

    [Test]
    public void Preview_GroupPostNotificationEmail() { /* ... */ }

    [Test]
    public void Preview_MembershipRequestEmail() { /* ... */ }

    [Test]
    public void Preview_PartnerWelcomeEmail() { /* ... */ }
}
```

#### 7.3 Usage

**View previews after running tests:**
```powershell
# Run the preview tests
dotnet test tests/web/Jordnaer.Tests --filter FullyQualifiedName~EmailPreviewTests

# Preview files are saved to: tests/web/Jordnaer.Tests/bin/Debug/net10.0/email-previews/
# Open any .html file in a browser to see the rendered email
```

**View HTML in test output:**
```powershell
# Run with verbose output to see HTML in console
dotnet test tests/web/Jordnaer.Tests --filter FullyQualifiedName~EmailPreviewTests --logger "console;verbosity=detailed"
```

#### 7.4 .gitignore

Add to `.gitignore`:
```
# Email preview outputs
**/email-previews/
```

## Testing Checklist

- [ ] All email types render correctly with the new template
- [ ] Logo displays properly (accessible via URL)
- [ ] Buttons are clickable and styled correctly
- [ ] Email renders well on mobile (responsive)
- [ ] Unit tests pass with updated assertions
- [ ] Test sending emails in development environment
- [ ] Email preview tests generate `.html` files that can be opened in a browser
- [ ] Preview files show correct branding, layout, and content for each email type

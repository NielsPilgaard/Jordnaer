namespace Jordnaer.Features.Email;

public static class EmailTemplate
{
	/// <summary>
	/// Wraps email content in the standard Mini Møder email layout with header, logo, and footer.
	/// </summary>
	/// <param name="bodyContent">The main HTML content of the email (without wrapper)</param>
	/// <param name="baseUrl">The base URL for hosted assets (e.g. https://mini-moeder.dk)</param>
	/// <param name="preheaderText">Optional preview text shown in email clients</param>
	/// <returns>Complete HTML email with layout</returns>
	public static string Wrap(string bodyContent, string? baseUrl = null, string? preheaderText = null)
	{
		var logoUrl = GetLogoUrl(baseUrl);

		var preheader = preheaderText is not null
			? $"""<div style="display: none; max-height: 0; overflow: hidden;">{preheaderText}</div>"""
			: "";

		return $"""
			<!DOCTYPE html>
			<html>
			<head>
			    <meta charset="utf-8">
			    <meta name="viewport" content="width=device-width, initial-scale=1.0">
			    <title>Mini Møder</title>
			</head>
			<body style="margin: 0; padding: 0; background-color: #f5f5f5; font-family: 'Open Sans', Arial, sans-serif;">
			    {preheader}

			    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color: #f5f5f5;">
			        <tr>
			            <td align="center" style="padding: 20px 10px;">
			                <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width: 600px; width: 100%; background-color: #ffffff; border-radius: 8px;">
			                    <!-- Header with logo -->
			                    <tr>
			                        <td align="center" style="padding: 30px 40px 20px;">
			                            <img src="{logoUrl}" alt="Mini Møder" width="200" style="display: block; max-width: 200px; height: auto;">
			                        </td>
			                    </tr>

			                    <!-- Body content -->
			                    <tr>
			                        <td style="padding: 20px 40px 30px; color: #41556b; font-size: 16px; line-height: 1.6; font-family: 'Open Sans', Arial, sans-serif;">
			                            {bodyContent}
			                        </td>
			                    </tr>

			                    <!-- Footer -->
			                    <tr>
			                        <td style="padding: 20px 40px; background-color: #f9f9f9; border-radius: 0 0 8px 8px; color: #666666; font-size: 14px; line-height: 1.5; font-family: 'Open Sans', Arial, sans-serif;">
			                            <p style="margin: 0;">Venlig hilsen,<br>Mini Møder Teamet</p>
			                            <p style="margin: 10px 0 0; font-size: 12px; color: #999999;">Denne email blev sendt fra Mini Møder</p>
			                        </td>
			                    </tr>
			                </table>
			            </td>
			        </tr>
			    </table>
			</body>
			</html>
			""";
	}

	/// <summary>
	/// Creates a styled CTA button for emails.
	/// </summary>
	public static string Button(string href, string text, string? backgroundColor = null)
	{
		var bgColor = backgroundColor ?? "#dbab45";
		return $"""
			<table role="presentation" cellspacing="0" cellpadding="0" style="margin: 16px 0;">
			    <tr>
			        <td style="border-radius: 6px; background-color: {bgColor};">
			            <a href="{href}" style="display: inline-block; padding: 12px 24px; color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px; font-family: 'Open Sans', Arial, sans-serif;">{text}</a>
			        </td>
			    </tr>
			</table>
			""";
	}

	/// <summary>
	/// Gets the full logo URL from the base URL.
	/// </summary>
	public static string GetLogoUrl(string? baseUrl)
	{
		var url = (baseUrl ?? "https://mini-moeder.dk").TrimEnd('/');
		return $"{url}/images/minimoeder_logo_payoff.png";
	}
}

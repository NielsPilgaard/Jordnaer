using Ganss.Xss;
using Markdig;
using Microsoft.AspNetCore.Components;

namespace Jordnaer.Features.Profile;

public static class MarkdownRenderer
{
	private static readonly Lazy<HtmlSanitizer> _sanitizer = new(() => new HtmlSanitizer());
	internal static readonly HtmlSanitizer Sanitizer = _sanitizer.Value;

	internal static readonly MarkupString EmptyMarkupString = new(string.Empty);

	public static string Sanitize(string markdown) => Sanitizer.Sanitize(markdown);

	public static MarkupString SanitizeAndRenderMarkupString(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return EmptyMarkupString;
		}

		var sanitizedHtml = Sanitize(markdown);
		var html = Markdown.ToHtml(sanitizedHtml);
		return new MarkupString(html);
	}
}
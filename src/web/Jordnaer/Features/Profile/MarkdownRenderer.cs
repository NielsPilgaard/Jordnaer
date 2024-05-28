using Ganss.Xss;
using Markdig;
using Microsoft.AspNetCore.Components;

namespace Jordnaer.Features.Profile;

public static class MarkdownRenderer
{
	private static readonly Lazy<HtmlSanitizer> _sanitizer = new(() => new HtmlSanitizer());
	internal static readonly HtmlSanitizer Sanitizer = _sanitizer.Value;

	public static string Sanitize(string markdown) => Sanitizer.Sanitize(markdown);

	public static MarkupString SanitizeAndRenderMarkupString(string markdown)
	{
		var sanitizedHtml = Sanitize(markdown);
		var html = Markdown.ToHtml(sanitizedHtml);
		return new MarkupString(html);
	}
}
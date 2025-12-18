using Jordnaer.Features.Profile;
using Markdig;
using Microsoft.AspNetCore.Components;

namespace Jordnaer.Extensions;

public static class StringExtensions
{
	public static MarkupString SanitizeAndRenderMarkupString(this string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return MarkdownRenderer.EmptyMarkupString;
		}

		var sanitizedHtml = MarkdownRenderer.Sanitize(markdown);
		var html = Markdown.ToHtml(sanitizedHtml);
		return new MarkupString(html);
	}

	public static string Sanitize(this string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		var sanitizedValue = MarkdownRenderer.Sanitize(value);
		return sanitizedValue;
	}

	public static MarkupString SanitizeHtml(this string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return MarkdownRenderer.EmptyMarkupString;
		}

		var sanitizedHtml = MarkdownRenderer.Sanitize(value);
		return new MarkupString(sanitizedHtml);
	}
}

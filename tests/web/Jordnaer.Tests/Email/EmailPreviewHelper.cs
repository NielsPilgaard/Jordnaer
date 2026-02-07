namespace Jordnaer.Tests.Email;

public static class EmailPreviewHelper
{
	private static readonly string PreviewOutputDir = Path.Combine(
		AppContext.BaseDirectory,
		"email-previews");

	/// <summary>
	/// Saves the rendered email HTML to a file that can be opened in a browser.
	/// </summary>
	public static string SavePreview(string htmlContent, string emailName)
	{
		Directory.CreateDirectory(PreviewOutputDir);

		var fileName = $"{emailName}.html";
		var filePath = Path.Combine(PreviewOutputDir, fileName);

		File.WriteAllText(filePath, htmlContent);

		return filePath;
	}
}

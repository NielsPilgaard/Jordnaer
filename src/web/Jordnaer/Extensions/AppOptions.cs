using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Extensions;

public sealed class AppOptions
{
	public const string SectionName = "App";

	[Url]
	public string BaseUrl { get; set; } = "https://mini-moeder.dk";
}

using System.Text.Json.Serialization;

namespace Jordnaer.Features.Images;

public class FacebookProfilePictureData
{
	[JsonPropertyName("height")]
	public int Height { get; set; }

	[JsonPropertyName("is_silhouette")]
	public bool IsSilhouette { get; set; }

	[JsonPropertyName("url")]
	public string? Url { get; set; }

	[JsonPropertyName("width")]
	public int Width { get; set; }
}

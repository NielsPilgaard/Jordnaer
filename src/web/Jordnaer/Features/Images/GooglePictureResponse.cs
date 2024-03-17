using System.Text.Json.Serialization;

namespace Jordnaer.Features.Images;

public class GooglePictureResponse
{
	[JsonPropertyName("picture")]
	public string? Picture { get; set; }
}

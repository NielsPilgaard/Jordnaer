using System.Text.Json.Serialization;

namespace Jordnaer.Features.Images;

public class FacebookProfilePictureResponse
{
	[JsonPropertyName("data")]
	public FacebookProfilePictureData? Data { get; set; }
}

using System.Text.Json.Serialization;

namespace Jordnaer.Features.Profile;

public class FacebookProfilePictureResponse
{
	[JsonPropertyName("data")]
	public FacebookProfilePictureData? Data { get; set; }
}

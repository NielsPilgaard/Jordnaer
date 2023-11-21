using System.Text.Json.Serialization;

namespace Jordnaer.Server.Features.Profile;

public class FacebookProfilePictureResponse
{
    [JsonPropertyName("data")]
    public FacebookProfilePictureData? Data { get; set; }
}

using System.Text.Json.Serialization;

namespace Jordnaer.Server.Features.Profile;

public class GooglePictureResponse
{
    [JsonPropertyName("picture")]
    public string? Picture { get; set; }
}

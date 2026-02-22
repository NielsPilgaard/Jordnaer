using Azure.Storage.Blobs;
using Jordnaer.Features.Map;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Jordnaer.Features.HjemGroups;

public interface IHjemGroupProvider
{
    Task<IReadOnlyList<GroupMarkerData>> GetMarkersAsync(CancellationToken cancellationToken = default);
}

public class HjemGroupProvider(
    BlobServiceClient blobServiceClient,
    ILogger<HjemGroupProvider> logger) : IHjemGroupProvider
{
    private const string ContainerName = "hjemlo-groups";
    private const string BlobName = "groups.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private IReadOnlyList<GroupMarkerData>? _cached;

    public async Task<IReadOnlyList<GroupMarkerData>> GetMarkersAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null)
            return _cached;

        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

            if (!await containerClient.ExistsAsync(cancellationToken))
                return [];

            var blobClient = containerClient.GetBlobClient(BlobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
                return [];

            var response = await blobClient.DownloadContentAsync(cancellationToken);
            var entries = JsonSerializer.Deserialize<List<HjemGroupEntry>>(
                response.Value.Content.ToString(), JsonOptions);

            if (entries is null or { Count: 0 })
                return [];

            _cached = entries.Select(MapToMarker).ToList();
            return _cached;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load HJEM group markers from blob storage.");
            return _cached ?? [];
        }
    }

    private static GroupMarkerData MapToMarker(HjemGroupEntry entry)
    {
        var idBytes = MD5.HashData(Encoding.UTF8.GetBytes(entry.WebsiteUrl.ToString() + entry.Name));
        var id = new Guid(idBytes);

        return new GroupMarkerData
        {
            Id = id,
            Name = entry.Name,
            ProfilePictureUrl = null,
            WebsiteUrl = entry.WebsiteUrl.ToString(),
            ShortDescription = entry.Type == HjemGroupType.Lokalafdeling
                ? "HJEM lokalafdeling"
                : "HJEM lokalrepræsentant",
            ZipCode = entry.ZipCode,
            City = entry.City,
            Latitude = entry.Latitude,
            Longitude = entry.Longitude,
        };
    }
}

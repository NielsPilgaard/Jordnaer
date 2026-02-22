using Azure.Storage.Blobs;
using Jordnaer.Features.Map;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZiggyCreatures.Caching.Fusion;

namespace Jordnaer.Features.HjemGroups;

public interface IHjemGroupProvider
{
    Task<IReadOnlyList<GroupMarkerData>> GetMarkersAsync(CancellationToken cancellationToken = default);
}

public class HjemGroupProvider(
    BlobServiceClient blobServiceClient,
    IFusionCache fusionCache,
    ILogger<HjemGroupProvider> logger) : IHjemGroupProvider
{
    internal const string CacheTag = "hjem-groups";
    private const string CacheKey = "HjemGroups:markers";
    private const string ContainerName = "hjemlo-groups";
    private const string BlobName = "groups.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<IReadOnlyList<GroupMarkerData>> GetMarkersAsync(CancellationToken cancellationToken = default)
    {
        return await fusionCache.GetOrSetAsync<IReadOnlyList<GroupMarkerData>>(
            CacheKey,
            (_, innerToken) => LoadFromBlobAsync(innerToken),
            tags: [CacheTag],
            token: cancellationToken) ?? [];
    }

    private async Task<IReadOnlyList<GroupMarkerData>> LoadFromBlobAsync(CancellationToken cancellationToken)
    {
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

            return entries.Select(MapToMarker).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load HJEM group markers from blob storage.");
            return [];
        }
    }

    private static GroupMarkerData MapToMarker(HjemGroupEntry entry)
    {
        var idBytes = MD5.HashData(Encoding.UTF8.GetBytes(entry.WebsiteUrl.ToString() + entry.Name));
        var id = new Guid(idBytes);

        var websiteUrl = entry.Type == HjemGroupType.Lokalrepresentant
            ? "https://www.hjemlo.dk/lokalrepraesentanter"
            : entry.WebsiteUrl.ToString();

        return new GroupMarkerData
        {
            Id = id,
            Name = entry.Name,
            ProfilePictureUrl = entry.IconUrl ?? "/images/partners/logo-hjem.avif",
            WebsiteUrl = websiteUrl,
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

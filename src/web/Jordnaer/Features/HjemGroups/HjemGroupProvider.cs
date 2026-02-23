using Azure.Storage.Blobs;
using Jordnaer.Features.Map;
using OneOf;
using OneOf.Types;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZiggyCreatures.Caching.Fusion;

namespace Jordnaer.Features.HjemGroups;

public interface IHjemGroupProvider
{
    Task<OneOf<IReadOnlyList<GroupMarkerData>, Error>> GetMarkersAsync(CancellationToken cancellationToken = default);
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

    public async Task<OneOf<IReadOnlyList<GroupMarkerData>, Error>> GetMarkersAsync(CancellationToken cancellationToken = default)
    {
        return await fusionCache.GetOrSetAsync<OneOf<IReadOnlyList<GroupMarkerData>, Error>>(
            CacheKey,
            (_, innerToken) => LoadFromBlobAsync(innerToken),
            tags: [CacheTag],
            token: cancellationToken);
    }

    private async Task<OneOf<IReadOnlyList<GroupMarkerData>, Error>> LoadFromBlobAsync(CancellationToken cancellationToken)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                logger.LogError("Blob container '{ContainerName}' does not exist.", ContainerName);
                return new Error();
            }

            var blobClient = containerClient.GetBlobClient(BlobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                logger.LogError("Blob '{BlobName}' does not exist in container '{ContainerName}'.", BlobName, ContainerName);
                return new Error();
            }

            var response = await blobClient.DownloadContentAsync(cancellationToken);
            var entries = JsonSerializer.Deserialize<List<HjemGroupEntry>>(
                response.Value.Content.ToString(), JsonOptions);

            if (entries is null or { Count: 0 })
            {
                return Array.Empty<GroupMarkerData>();
            }

            return entries.Select(MapToMarker).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load HJEM group markers from blob storage.");
            return new Error();
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

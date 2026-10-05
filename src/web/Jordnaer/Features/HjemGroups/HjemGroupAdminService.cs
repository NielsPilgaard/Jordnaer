using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Jordnaer.Features.Search;
using Jordnaer.Shared;
using MassTransit;
using OneOf;
using OneOf.Types;
using System.Text;
using System.Text.Json;

namespace Jordnaer.Features.HjemGroups;

public class HjemGroupAdminService(
    BlobServiceClient blobServiceClient,
    IZipCodeService zipCodeService,
    IPublishEndpoint publishEndpoint,
    ILogger<HjemGroupAdminService> logger)
{
    private const string ContainerName = "hjemlo-groups";
    private const string BlobName = "groups.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<OneOf<List<HjemGroupEntry>, HjemGroupLoadError>> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                return new HjemGroupLoadError($"Blob container '{ContainerName}' does not exist.");
            }

            var blobClient = containerClient.GetBlobClient(BlobName);
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return new HjemGroupLoadError($"Blob '{BlobName}' does not exist in container '{ContainerName}'.");
            }

            var response = await blobClient.DownloadContentAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<HjemGroupEntry>>(
                response.Value.Content.ToString(), JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load HJEM group entries from blob storage.");
            return new HjemGroupLoadError(ex.Message);
        }
    }

    public async Task<OneOf<Success, HjemGroupSaveError>> SaveAsync(List<HjemGroupEntry> entries, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(BlobName);
            var json = JsonSerializer.Serialize(entries, JsonOptions);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);

            await publishEndpoint.Publish(
                new InvalidateCacheTags { Tags = [HjemGroupProvider.CacheTag] },
                cancellationToken);

            return new Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save HJEM group entries.");
            return new HjemGroupSaveError(ex.Message);
        }
    }

    /// <summary>
    /// Looks up coordinates and city/zip for a Danish city or area name, using the zip code center.
    /// Returns null if not found.
    /// </summary>
    public GeocodeResult? Geocode(string locationText)
    {
        var zipCode = zipCodeService.Find(locationText);

        return zipCode is null
            ? null
            : new GeocodeResult(zipCode.Name, zipCode.Number, zipCode.Latitude, zipCode.Longitude);
    }

    public sealed record GeocodeResult(string City, int? ZipCode, double Latitude, double Longitude);
}

public sealed record HjemGroupLoadError(string Message);
public sealed record HjemGroupSaveError(string Message);

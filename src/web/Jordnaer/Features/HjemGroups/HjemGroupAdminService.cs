using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Jordnaer.Shared;
using MassTransit;
using OneOf;
using System.Text;
using System.Text.Json;

namespace Jordnaer.Features.HjemGroups;

public class HjemGroupAdminService(
    BlobServiceClient blobServiceClient,
    IDataForsyningenClient dataForsyningenClient,
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

    public async Task SaveAsync(List<HjemGroupEntry> entries, CancellationToken cancellationToken = default)
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
    }

    /// <summary>
    /// Looks up coordinates and city/zip for a Danish city or area name via Dataforsyningen.
    /// Returns null if not found.
    /// </summary>
    public async Task<GeocodeResult?> GeocodeAsync(string locationText, CancellationToken cancellationToken = default)
    {
        var response = await dataForsyningenClient.SearchZipCodesAsync(locationText, cancellationToken);

        if (!response.IsSuccessStatusCode || response.Content is null)
        {
            return null;
        }

        var first = response.Content.FirstOrDefault();
        if (first.Navn is null || first.Visueltcenter is not { Length: >= 2 })
        {
            return null;
        }

        // GeoJSON order: [longitude, latitude]
        var longitude = (double)first.Visueltcenter[0];
        var latitude = (double)first.Visueltcenter[1];

        int? zipCode = null;
        if (int.TryParse(first.Nr, out var parsedZip))
        {
            zipCode = parsedZip;
        }

        return new GeocodeResult(first.Navn, zipCode, latitude, longitude);
    }

    public sealed record GeocodeResult(string City, int? ZipCode, double Latitude, double Longitude);
}

public sealed record HjemGroupLoadError(string Message);

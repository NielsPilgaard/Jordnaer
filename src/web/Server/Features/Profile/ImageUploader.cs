using Azure.Storage.Blobs;

namespace Jordnaer.Server.Features.Profile;

public class ImageUploader : IImageUploader
{
    private readonly BlobServiceClient _blobServiceClient;

    public ImageUploader(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<string> UploadImageAsync(string blobName, string containerName, byte[] fileBytes)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobName);

        // Convert file bytes to MemoryStream and upload
        using var memoryStream = new MemoryStream(fileBytes);

        await blobClient.UploadAsync(memoryStream, overwrite: true);

        return blobClient.Uri.AbsoluteUri;
    }
}
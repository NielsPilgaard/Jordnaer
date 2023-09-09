using Azure.Storage.Blobs;

namespace Jordnaer.Server.Features.Profile;

public interface IImageUploader
{
    /// <summary>
    /// Uploads an image to Azure Blob Storage an returns the url.
    /// <para>
    /// If container <paramref name="containerName"/> already has a blob
    /// with the name <paramref name="blobName"/>, it is overriden.
    /// </para>
    /// </summary>
    /// <param name="blobServiceClient"></param>
    /// <param name="blobName"></param>
    /// <param name="containerName"></param>
    /// <param name="fileBytes"></param>
    /// <returns></returns>
    Task<string> UploadImageAsync(
        string blobName,
        string containerName,
        byte[] fileBytes);
}

public class ImageUploader : IImageUploader
{
    public const string ChildProfilePicturesContainerName = "childprofile-pictures";
    public const string UserProfilePicturesContainerName = "userprofile-pictures";

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

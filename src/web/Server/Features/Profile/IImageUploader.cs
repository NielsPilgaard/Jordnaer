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
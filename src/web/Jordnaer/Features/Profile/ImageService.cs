using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Jordnaer.Features.Profile;

public interface IImageService
{
	/// <summary>
	/// Uploads an image to Azure Blob Storage an returns the url.
	/// <para>
	/// If container <paramref name="containerName"/> already has a blob
	/// with the name <paramref name="blobName"/>, it is overriden.
	/// </para>
	/// </summary>
	/// <param name="blobName"></param>
	/// <param name="containerName"></param>
	/// <param name="fileBytes"></param>
	/// <returns></returns>
	Task<string> UploadImageAsync(
		string blobName,
		string containerName,
		byte[] fileBytes,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Uploads an image to Azure Blob Storage an returns the url.
	/// <para>
	/// If container <paramref name="containerName"/> already has a blob
	/// with the name <paramref name="blobName"/>, it is overriden.
	/// </para>
	/// </summary>
	/// <param name="blobName"></param>
	/// <param name="containerName"></param>
	/// <param name="fileStream"></param>
	/// <returns></returns>
	Task<string> UploadImageAsync(
		string blobName,
		string containerName,
		Stream fileStream,
		CancellationToken cancellationToken = default);

	Task DeleteImageAsync(string blobName, string containerName,
		CancellationToken cancellationToken = default);
}

public class ImageService : IImageService
{
	private readonly BlobServiceClient _blobServiceClient;
	private readonly ILogger<ImageService> _logger;

	public ImageService(BlobServiceClient blobServiceClient, ILogger<ImageService> logger)
	{
		_blobServiceClient = blobServiceClient;
		_logger = logger;
	}

	public async Task<string> UploadImageAsync(string blobName, string containerName, Stream fileStream, CancellationToken cancellationToken = default)
	{
		var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
		await containerClient.CreateIfNotExistsAsync(
			publicAccessType: PublicAccessType.Blob,
			cancellationToken: cancellationToken);

		var blobClient = containerClient.GetBlobClient(blobName);

		await blobClient.UploadAsync(fileStream, overwrite: true, cancellationToken);

		return blobClient.Uri.AbsoluteUri;
	}

	public async Task<string> UploadImageAsync(string blobName, string containerName, byte[] fileBytes, CancellationToken cancellationToken = default)
	{
		using var stream = new MemoryStream(fileBytes);

		return await UploadImageAsync(blobName, containerName, stream, cancellationToken);
	}

	public async Task DeleteImageAsync(string blobName, string containerName,
		CancellationToken cancellationToken = default)
	{
		var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
		await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

		var blobClient = containerClient.GetBlobClient(blobName);

		var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

		if (response.HasValue && response.Value is false)
		{
			_logger.LogWarning("Failed to delete blob {blobName} from container {containerName}", blobName, containerName);
		}
	}
}

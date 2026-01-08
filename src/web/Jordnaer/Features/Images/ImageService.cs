using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Jordnaer.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Jordnaer.Features.Images;

public interface IImageService
{
	/// <summary>
	/// Uploads an image to Azure Blob Storage and returns the url.
	/// <para>
	/// If container <paramref name="containerName"/> already has a blob
	/// with the name <paramref name="blobName"/>, it is overriden.
	/// </para>
	/// </summary>
	/// <param name="blobName"></param>
	/// <param name="containerName"></param>
	/// <param name="fileBytes"></param>
	/// <param name="cancellationToken"></param>
	Task<string> UploadImageAsync(
		string blobName,
		string containerName,
		byte[] fileBytes,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Uploads an image to Azure Blob Storage and returns the url.
	/// <para>
	/// If container <paramref name="containerName"/> already has a blob
	/// with the name <paramref name="blobName"/>, it is overriden.
	/// </para>
	/// </summary>
	/// <param name="blobName"></param>
	/// <param name="containerName"></param>
	/// <param name="fileStream"></param>
	/// <param name="cancellationToken"></param>
	Task<string> UploadImageAsync(
		string blobName,
		string containerName,
		Stream fileStream,
		CancellationToken cancellationToken = default);

	Task DeleteImageAsync(
		string blobName,
		string containerName,
		CancellationToken cancellationToken = default);

	Task<Stream?> GetImageStreamFromUrlAsync(string url, CancellationToken cancellationToken = default);

	Task<Stream> ResizeImageAsync(Stream imageAsStream, CancellationToken cancellationToken = default);
}

public class ImageService(
	BlobServiceClient blobServiceClient,
	ILogger<ImageService> logger,
	IHttpClientFactory httpClientFactory)
	: IImageService
{
	public async Task<string> UploadImageAsync(string blobName, string containerName, Stream fileStream, CancellationToken cancellationToken = default)
	{
		var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
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
		var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
		await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

		var blobClient = containerClient.GetBlobClient(blobName);

		var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

		if (response.HasValue && response.Value is false)
		{
			logger.LogWarning("Failed to delete blob {blobName} from container {containerName}", blobName, containerName);
		}
	}

	public async Task<Stream?> GetImageStreamFromUrlAsync(string url, CancellationToken cancellationToken = default)
	{
		var client = httpClientFactory.CreateClient(HttpClients.Default);
		var response = await client.GetAsync(url, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			logger.LogError("Failed to get image as byte array from url {url}", url);
			return null;
		}

		return await response.Content.ReadAsStreamAsync(cancellationToken);
	}

	[Obsolete("Should probably not be used in this form, it worsens the image quality." +
			  "We should enforce a 1x1 pixel ratio instead, so we can scale up and down.")]
	public async Task<Stream> ResizeImageAsync(Stream imageAsStream, CancellationToken cancellationToken = default)
	{
		using var image = await Image.LoadAsync(imageAsStream, cancellationToken);

		if (image.Height > 800)
		{
			// 0 maintains aspect ratio
			const int width = 0;
			const int height = 200;
			image.Mutate(img => img.Resize(width, height));
		}

		var outputStream = new MemoryStream();

		await image.SaveAsync(outputStream, new WebpEncoder(), cancellationToken);

		// Allow the stream to be read again
		outputStream.Position = 0;

		return outputStream;
	}
}

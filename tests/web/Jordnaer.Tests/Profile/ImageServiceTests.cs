using Azure.Storage.Blobs;
using FluentAssertions;
using Jordnaer.Features.Images;
using Jordnaer.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jordnaer.Tests.Profile;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class ImageServiceTests
{
	private readonly BlobServiceClient _blobServiceClient;
	private readonly IImageService _sut;

	private const string ContainerName = "test-container";

	public ImageServiceTests(JordnaerWebApplicationFactory factory)
	{
		using var scope = factory.Services.CreateScope();
		_blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
		_sut = scope.ServiceProvider.GetRequiredService<IImageService>();
	}

	[Fact]
	public async Task UploadImage_UsingFileStream_Successfully()
	{
		// Arrange
		const string blobName = nameof(UploadImage_UsingFileStream_Successfully);
		var fileStream = new MemoryStream([1, 2, 3, 4, 5]);

		// Act
		var result = await _sut.UploadImageAsync(blobName, ContainerName, fileStream);

		// Assert
		result.Should().NotBeNullOrEmpty();

		// Clean up (delete the blob created during the test)
		await _sut.DeleteImageAsync(blobName, ContainerName);
	}

	[Fact]
	public async Task UploadImage_UsingByteArray_Successfully()
	{
		// Arrange
		const string blobName = nameof(UploadImage_UsingByteArray_Successfully);
		byte[] fileBytes = [1, 2, 3, 4, 5];

		// Act
		var result = await _sut.UploadImageAsync(blobName, ContainerName, fileBytes);

		// Assert
		result.Should().NotBeNullOrEmpty();

		// Clean up (delete the blob created during the test)
		await _sut.DeleteImageAsync(blobName, ContainerName);
	}

	[Fact]
	public async Task DeleteImage_Successfully()
	{
		// Arrange
		const string blobName = nameof(DeleteImage_Successfully);
		byte[] fileBytes = [1, 2, 3, 4, 5];
		await _sut.UploadImageAsync(blobName, ContainerName, fileBytes);

		// Act
		await _sut.DeleteImageAsync(blobName, ContainerName);

		// Assert
		var blobExists = await _blobServiceClient.GetBlobContainerClient(ContainerName)
			.GetBlobClient(blobName)
			.ExistsAsync();
		blobExists.Value.Should().BeFalse();
	}

	[Fact]
	public async Task UploadImage_OverridesExistingBlob_Successfully()
	{
		// Arrange
		const string blobName = nameof(UploadImage_OverridesExistingBlob_Successfully);
		byte[] initialBytes = [1, 2, 3, 4, 5];
		byte[] newBytes = [6, 7, 8, 9, 10];
		await _sut.UploadImageAsync(blobName, ContainerName, initialBytes);

		// Assert that the initial bytes are stored
		var blob = await _blobServiceClient.GetBlobContainerClient(ContainerName)
			.GetBlobClient(blobName)
			.DownloadContentAsync();
		blob.Value.Content.ToArray().Should().BeEquivalentTo(initialBytes);

		// Act
		var result = await _sut.UploadImageAsync(blobName, ContainerName, newBytes);

		// Assert
		result.Should().NotBeNullOrEmpty();

		// Assert that the new bytes are stored
		blob = await _blobServiceClient.GetBlobContainerClient(ContainerName)
			.GetBlobClient(blobName)
			.DownloadContentAsync();
		blob.Value.Content.ToArray().Should().BeEquivalentTo(newBytes);

		// Clean up (delete the blob created during the test)
		await _sut.DeleteImageAsync(blobName, ContainerName);
	}
}

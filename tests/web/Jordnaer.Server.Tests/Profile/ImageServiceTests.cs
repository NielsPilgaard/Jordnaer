using Azure.Storage.Blobs;
using FluentAssertions;
using Jordnaer.Server.Features.Profile;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jordnaer.Server.Tests.Profile;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class ImageServiceTests
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ImageService _sut;

    private const string ContainerName = "test-container";
    private const string BlobName = "test-blob";

    public ImageServiceTests(JordnaerWebApplicationFactory factory)
    {
        _blobServiceClient = factory.Services.GetRequiredService<BlobServiceClient>();
        var logger = factory.Services.GetRequiredService<ILogger<ImageService>>();

        _sut = new ImageService(_blobServiceClient, logger);
    }

    [Fact]
    public async Task UploadImage_UsingFileStream_Successfully()
    {
        // Arrange
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        string result = await _sut.UploadImageAsync(BlobName, ContainerName, fileStream);

        // Assert
        result.Should().NotBeNullOrEmpty();

        // Clean up (delete the blob created during the test)
        await _sut.DeleteImageAsync(BlobName, ContainerName);
    }

    [Fact]
    public async Task UploadImage_UsingByteArray_Successfully()
    {
        // Arrange
        byte[] fileBytes = { 1, 2, 3, 4, 5 };

        // Act
        string result = await _sut.UploadImageAsync(BlobName, ContainerName, fileBytes);

        // Assert
        result.Should().NotBeNullOrEmpty();

        // Clean up (delete the blob created during the test)
        await _sut.DeleteImageAsync(BlobName, ContainerName);
    }

    [Fact]
    public async Task DeleteImage_Successfully()
    {
        // Arrange
        byte[] fileBytes = { 1, 2, 3, 4, 5 };
        await _sut.UploadImageAsync(BlobName, ContainerName, fileBytes);

        // Act
        await _sut.DeleteImageAsync(BlobName, ContainerName);

        // Assert
        var blobExists = await _blobServiceClient.GetBlobContainerClient(ContainerName)
            .GetBlobClient(BlobName)
            .ExistsAsync();
        blobExists.Value.Should().BeFalse();
    }

    [Fact]
    public async Task UploadImage_OverridesExistingBlob_Successfully()
    {
        // Arrange
        byte[] initialBytes = { 1, 2, 3, 4, 5 };
        byte[] newBytes = { 6, 7, 8, 9, 10 };
        await _sut.UploadImageAsync(BlobName, ContainerName, initialBytes);

        // Assert that the initial bytes are stored
        var blob = await _blobServiceClient.GetBlobContainerClient(ContainerName)
            .GetBlobClient(BlobName)
            .DownloadContentAsync();
        blob.Value.Content.ToArray().Should().BeEquivalentTo(initialBytes);

        // Act
        string result = await _sut.UploadImageAsync(BlobName, ContainerName, newBytes);

        // Assert
        result.Should().NotBeNullOrEmpty();

        // Assert that the new bytes are stored
        blob = await _blobServiceClient.GetBlobContainerClient(ContainerName)
            .GetBlobClient(BlobName)
            .DownloadContentAsync();
        blob.Value.Content.ToArray().Should().BeEquivalentTo(newBytes);

        // Clean up (delete the blob created during the test)
        await _sut.DeleteImageAsync(BlobName, ContainerName);
    }
}

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Jordnaer.Features.HjemGroups;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Text;
using System.Text.Json;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Jordnaer.Tests.HjemGroups;

public class HjemGroupProviderTests
{
    private readonly BlobServiceClient _blobServiceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _containerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();
    private readonly ILogger<HjemGroupProvider> _logger = Substitute.For<ILogger<HjemGroupProvider>>();

    private static IFusionCache CreateFusionCache() =>
        new FusionCache(new FusionCacheOptions(), new MemoryCache(new MemoryCacheOptions()));

    private HjemGroupProvider CreateSut() =>
        new(_blobServiceClient, CreateFusionCache(), _logger);

    public HjemGroupProviderTests()
    {
        _blobServiceClient
            .GetBlobContainerClient("hjemlo-groups")
            .Returns(_containerClient);

        _containerClient
            .GetBlobClient("groups.json")
            .Returns(_blobClient);
    }

    [Fact]
    public async Task GetMarkersAsync_ReturnsEmpty_WhenContainerDoesNotExist()
    {
        // Arrange
        _containerClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(false, Substitute.For<Response>()));

        var sut = CreateSut();

        // Act
        var result = await sut.GetMarkersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMarkersAsync_ReturnsEmpty_WhenBlobDoesNotExist()
    {
        // Arrange
        _containerClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));
        _blobClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(false, Substitute.For<Response>()));

        var sut = CreateSut();

        // Act
        var result = await sut.GetMarkersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMarkersAsync_ReturnsEmpty_WhenBlobIsEmpty()
    {
        // Arrange
        SetupBlobWithContent("[]");
        var sut = CreateSut();

        // Act
        var result = await sut.GetMarkersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMarkersAsync_ReturnsMappedMarkers_ForLokalafdeling()
    {
        // Arrange
        var entries = new[]
        {
            new HjemGroupEntry
            {
                Name = "Randers",
                WebsiteUrl = new Uri("https://www.hjemlo.dk/randers"),
                City = "Randers",
                ZipCode = 8900,
                Latitude = 56.46,
                Longitude = 10.03,
                Type = HjemGroupType.Lokalafdeling,
            }
        };
        SetupBlobWithContent(JsonSerializer.Serialize(entries, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        var sut = CreateSut();

        // Act
        var result = await sut.GetMarkersAsync();

        // Assert
        result.Should().HaveCount(1);
        var marker = result[0];
        marker.Name.Should().Be("Randers");
        marker.WebsiteUrl.Should().Be("https://www.hjemlo.dk/randers");
        marker.City.Should().Be("Randers");
        marker.ZipCode.Should().Be(8900);
        marker.Latitude.Should().Be(56.46);
        marker.Longitude.Should().Be(10.03);
        marker.ShortDescription.Should().Be("HJEM lokalafdeling");
        marker.ProfilePictureUrl.Should().Be("/images/partners/logo-hjem.avif");
        marker.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetMarkersAsync_ReturnsMappedMarkers_ForLokalrepresentant()
    {
        // Arrange
        var entries = new[]
        {
            new HjemGroupEntry
            {
                Name = "Aalborg",
                WebsiteUrl = new Uri("https://www.hjemlo.dk/lokalrepraesentanter"),
                City = "Aalborg",
                ZipCode = 9000,
                Latitude = 57.04,
                Longitude = 9.92,
                Type = HjemGroupType.Lokalrepresentant,
            }
        };
        SetupBlobWithContent(JsonSerializer.Serialize(entries, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        var sut = CreateSut();

        // Act
        var result = await sut.GetMarkersAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].ShortDescription.Should().Be("HJEM lokalrepræsentant");
    }

    [Fact]
    public async Task GetMarkersAsync_ProducesStableId_ForSameEntry()
    {
        // Arrange
        var entry = new HjemGroupEntry
        {
            Name = "Aarhus",
            WebsiteUrl = new Uri("https://www.hjemlo.dk/aarhus"),
            City = "Aarhus",
            ZipCode = 8000,
            Latitude = 56.15,
            Longitude = 10.20,
            Type = HjemGroupType.Lokalafdeling,
        };
        var json = JsonSerializer.Serialize(new[] { entry }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Call GetMarkersAsync twice with two separate provider instances
        SetupBlobWithContent(json);
        var sut1 = CreateSut();
        var result1 = await sut1.GetMarkersAsync();

        SetupBlobWithContent(json);
        var sut2 = CreateSut();
        var result2 = await sut2.GetMarkersAsync();

        // Assert: same input always produces same Guid
        result1[0].Id.Should().Be(result2[0].Id);
        result1[0].Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetMarkersAsync_IsCached_WithinSameInstance()
    {
        // Arrange
        SetupBlobWithContent(JsonSerializer.Serialize(new[]
        {
            new HjemGroupEntry
            {
                Name = "Odense",
                WebsiteUrl = new Uri("https://www.hjemlo.dk/odense"),
                City = "Odense",
                ZipCode = 5000,
                Latitude = 55.40,
                Longitude = 10.38,
                Type = HjemGroupType.Lokalafdeling,
            }
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        var sut = CreateSut();

        // Act
        await sut.GetMarkersAsync();
        await sut.GetMarkersAsync();

        // Assert: blob was only downloaded once despite two calls
        await _blobClient.Received(1).DownloadContentAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMarkersAsync_ReturnsEmpty_WhenBlobThrows()
    {
        // Arrange
        _containerClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));
        _blobClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));
        _blobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException("Simulated blob error"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetMarkersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMarkersAsync_ReturnsAllEntries_WhenMultipleEntriesPresent()
    {
        // Arrange
        var entries = Enumerable.Range(1, 5).Select(i => new HjemGroupEntry
        {
            Name = $"City{i}",
            WebsiteUrl = new Uri($"https://www.hjemlo.dk/city{i}"),
            City = $"City{i}",
            ZipCode = 1000 + i,
            Latitude = 55.0 + i * 0.1,
            Longitude = 10.0 + i * 0.1,
            Type = i % 2 == 0 ? HjemGroupType.Lokalrepresentant : HjemGroupType.Lokalafdeling,
        }).ToArray();

        SetupBlobWithContent(JsonSerializer.Serialize(entries, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        var sut = CreateSut();

        // Act
        var result = await sut.GetMarkersAsync();

        // Assert
        result.Should().HaveCount(5);
        result.Select(m => m.Name).Should().BeEquivalentTo(entries.Select(e => e.Name));
    }

    [Fact]
    public async Task GetMarkersAsync_ProducesDifferentIds_ForDifferentEntries()
    {
        // Arrange
        var entries = new[]
        {
            new HjemGroupEntry { Name = "A", WebsiteUrl = new Uri("https://www.hjemlo.dk/a"), Latitude = 55, Longitude = 10, Type = HjemGroupType.Lokalafdeling },
            new HjemGroupEntry { Name = "B", WebsiteUrl = new Uri("https://www.hjemlo.dk/b"), Latitude = 56, Longitude = 11, Type = HjemGroupType.Lokalafdeling },
        };
        SetupBlobWithContent(JsonSerializer.Serialize(entries, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        var sut = CreateSut();

        // Act
        var result = await sut.GetMarkersAsync();

        // Assert
        result[0].Id.Should().NotBe(result[1].Id);
    }

    // Helper: wires up blob container + blob to return the given JSON string
    private void SetupBlobWithContent(string json)
    {
        _containerClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));
        _blobClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));

        var bytes = Encoding.UTF8.GetBytes(json);
        var binaryData = BinaryData.FromBytes(bytes);
        var downloadResult = BlobsModelFactory.BlobDownloadResult(content: binaryData);
        _blobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(downloadResult, Substitute.For<Response>()));
    }
}

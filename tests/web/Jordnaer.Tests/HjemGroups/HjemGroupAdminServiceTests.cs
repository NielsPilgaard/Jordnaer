using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Response = Azure.Response;
using ResponseT = Azure.Response<Azure.Storage.Blobs.Models.BlobContainerInfo>;
using ResponseTContent = Azure.Response<Azure.Storage.Blobs.Models.BlobContentInfo>;
using FluentAssertions;
using Jordnaer.Features.HjemGroups;
using Jordnaer.Features.Search;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OneOf;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Jordnaer.Tests.HjemGroups;

public class HjemGroupAdminServiceTests
{
    private readonly BlobServiceClient _blobServiceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _containerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();
    private readonly IZipCodeService _zipCodeService = new ZipCodeService();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly ILogger<HjemGroupAdminService> _logger = Substitute.For<ILogger<HjemGroupAdminService>>();

    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public HjemGroupAdminServiceTests()
    {
        _blobServiceClient
            .GetBlobContainerClient("hjemlo-groups")
            .Returns(_containerClient);
        _containerClient
            .GetBlobClient("groups.json")
            .Returns(_blobClient);
        _containerClient
            .CreateIfNotExistsAsync(Arg.Any<PublicAccessType>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<BlobContainerEncryptionScopeOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ResponseT>()));
        _blobClient
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ResponseTContent>()));
    }

    private HjemGroupAdminService CreateSut() =>
        new(_blobServiceClient, _zipCodeService, _publishEndpoint, _logger);

    // -------------------------------------------------------------------------
    // LoadAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_ReturnsError_WhenContainerDoesNotExist()
    {
        _containerClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(false, Substitute.For<Response>()));

        var result = await CreateSut().LoadAsync();

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ReturnsError_WhenBlobDoesNotExist()
    {
        _containerClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));
        _blobClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(false, Substitute.For<Response>()));

        var result = await CreateSut().LoadAsync();

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmpty_WhenBlobContainsEmptyArray()
    {
        SetupBlobWithContent("[]");

        var result = await CreateSut().LoadAsync();

        result.IsT0.Should().BeTrue();
        result.AsT0.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ReturnsDeserializedEntries()
    {
        var entries = new[]
        {
            MakeEntry("Randers", "https://www.hjemlo.dk/randers", HjemGroupType.Lokalafdeling),
            MakeEntry("Odense", "https://www.hjemlo.dk/lokalrepraesentanter", HjemGroupType.Lokalrepresentant),
        };
        SetupBlobWithContent(JsonSerializer.Serialize(entries, CamelCase));

        var result = await CreateSut().LoadAsync();

        result.IsT0.Should().BeTrue();
        result.AsT0.Should().HaveCount(2);
        result.AsT0[0].Name.Should().Be("Randers");
        result.AsT0[1].Name.Should().Be("Odense");
    }

    [Fact]
    public async Task LoadAsync_ReturnsError_WhenBlobThrows()
    {
        _containerClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));
        _blobClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));
        _blobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException("Simulated blob error"));

        var result = await CreateSut().LoadAsync();

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ReturnsError_WhenBlobContainsInvalidJson()
    {
        SetupBlobWithContent("this is not valid json {{{");

        var result = await CreateSut().LoadAsync();

        result.IsT1.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // SaveAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SaveAsync_UploadsJson_WithOverwriteTrue()
    {
        var entries = new List<HjemGroupEntry> { MakeEntry("Aarhus", "https://www.hjemlo.dk/aarhus", HjemGroupType.Lokalafdeling) };

        await CreateSut().SaveAsync(entries);

        await _blobClient.Received(1).UploadAsync(Arg.Any<Stream>(), overwrite: true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_CreatesContainerIfNotExists()
    {
        var entries = new List<HjemGroupEntry> { MakeEntry("Aarhus", "https://www.hjemlo.dk/aarhus", HjemGroupType.Lokalafdeling) };

        await CreateSut().SaveAsync(entries);

        await _containerClient.Received(1).CreateIfNotExistsAsync(
            PublicAccessType.None,
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<BlobContainerEncryptionScopeOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_PublishesCacheInvalidation()
    {
        var entries = new List<HjemGroupEntry> { MakeEntry("Aarhus", "https://www.hjemlo.dk/aarhus", HjemGroupType.Lokalafdeling) };

        await CreateSut().SaveAsync(entries);

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<InvalidateCacheTags>(m => m.Tags.Contains(HjemGroupProvider.CacheTag)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_UploadsValidCamelCaseJson()
    {
        var entries = new List<HjemGroupEntry>
        {
            MakeEntry("Randers", "https://www.hjemlo.dk/randers", HjemGroupType.Lokalafdeling),
        };

        var uploadedJson = string.Empty;
        _blobClient
            .UploadAsync(Arg.Do<Stream>(s =>
            {
                using var sr = new StreamReader(s, Encoding.UTF8, leaveOpen: true);
                uploadedJson = sr.ReadToEnd();
            }), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ResponseTContent>()));

        await CreateSut().SaveAsync(entries);

        var deserialized = JsonSerializer.Deserialize<List<HjemGroupEntry>>(uploadedJson, CamelCase);
        deserialized.Should().NotBeNull().And.HaveCount(1);
        deserialized![0].Name.Should().Be("Randers");
        // camelCase: field names start lowercase
        uploadedJson.Should().Contain("\"name\"");
        uploadedJson.Should().NotContain("\"Name\"");
    }

    [Fact]
    public async Task SaveAsync_CanRoundtrip_EmptyList()
    {
        var uploadedJson = string.Empty;
        _blobClient
            .UploadAsync(Arg.Do<Stream>(s =>
            {
                using var sr = new StreamReader(s, Encoding.UTF8, leaveOpen: true);
                uploadedJson = sr.ReadToEnd();
            }), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ResponseTContent>()));

        await CreateSut().SaveAsync([]);

        var deserialized = JsonSerializer.Deserialize<List<HjemGroupEntry>>(uploadedJson, CamelCase);
        deserialized.Should().NotBeNull().And.BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Geocode
    // -------------------------------------------------------------------------

    [Fact]
    public void Geocode_ReturnsNull_WhenNoZipCodeMatches()
    {
        var result = CreateSut().Geocode("xyzxyzxyz_nonexistent_city_999");

        result.Should().BeNull();
    }

    [Fact]
    public void Geocode_ReturnsCityAndCoordinates_ForKnownCity()
    {
        var result = CreateSut().Geocode("Randers C");

        result.Should().NotBeNull();
        result!.City.Should().Be("Randers C");
        result.ZipCode.Should().Be(8900);
        result.Latitude.Should().BeApproximately(56.46, 0.05);
        result.Longitude.Should().BeApproximately(10.03, 0.05);
    }

    [Fact]
    public void Geocode_MatchesCityNamePrefix()
    {
        var result = CreateSut().Geocode("Randers");

        result.Should().NotBeNull();
        result!.ZipCode.Should().Be(8900);
    }

    [Fact]
    public void Geocode_MatchesZipCode()
    {
        var result = CreateSut().Geocode("8550");

        result.Should().NotBeNull();
        result!.City.Should().Be("Ryomgård");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static HjemGroupEntry MakeEntry(string name, string url, HjemGroupType type) => new()
    {
        Name = name,
        WebsiteUrl = new Uri(url),
        City = name,
        ZipCode = 8900,
        Latitude = 56.0,
        Longitude = 10.0,
        Type = type,
    };

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

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Jordnaer.Features.HjemGroups;
using Jordnaer.Shared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Refit;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Jordnaer.Tests.HjemGroups;

public class HjemGroupAdminServiceTests
{
    private readonly BlobServiceClient _blobServiceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _containerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();
    private readonly IDataForsyningenClient _geocoder = Substitute.For<IDataForsyningenClient>();
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
            .Returns(Task.FromResult(Substitute.For<Response<BlobContainerInfo>>()));
        _blobClient
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<Response<BlobContentInfo>>()));
    }

    private HjemGroupAdminService CreateSut() =>
        new(_blobServiceClient, _geocoder, _logger);

    // -------------------------------------------------------------------------
    // LoadAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_ReturnsEmpty_WhenContainerDoesNotExist()
    {
        _containerClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(false, Substitute.For<Response>()));

        var result = await CreateSut().LoadAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmpty_WhenBlobDoesNotExist()
    {
        _containerClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));
        _blobClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(false, Substitute.For<Response>()));

        var result = await CreateSut().LoadAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmpty_WhenBlobContainsEmptyArray()
    {
        SetupBlobWithContent("[]");

        var result = await CreateSut().LoadAsync();

        result.Should().BeEmpty();
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

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Randers");
        result[1].Name.Should().Be("Odense");
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmpty_WhenBlobThrows()
    {
        _containerClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));
        _blobClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));
        _blobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException("Simulated blob error"));

        var result = await CreateSut().LoadAsync();

        result.Should().BeEmpty();
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
            PublicAccessType.Blob,
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<BlobContainerEncryptionScopeOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_UploadsValidCamelCaseJson()
    {
        var entries = new List<HjemGroupEntry>
        {
            MakeEntry("Randers", "https://www.hjemlo.dk/randers", HjemGroupType.Lokalafdeling),
        };

        string uploadedJson = string.Empty;
        _blobClient
            .UploadAsync(Arg.Do<Stream>(s =>
            {
                using var sr = new StreamReader(s, Encoding.UTF8);
                uploadedJson = sr.ReadToEnd();
            }), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<Response<BlobContentInfo>>()));

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
        string uploadedJson = string.Empty;
        _blobClient
            .UploadAsync(Arg.Do<Stream>(s =>
            {
                using var sr = new StreamReader(s, Encoding.UTF8);
                uploadedJson = sr.ReadToEnd();
            }), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<Response<BlobContentInfo>>()));

        await CreateSut().SaveAsync([]);

        var deserialized = JsonSerializer.Deserialize<List<HjemGroupEntry>>(uploadedJson, CamelCase);
        deserialized.Should().NotBeNull().And.BeEmpty();
    }

    // -------------------------------------------------------------------------
    // GeocodeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GeocodeAsync_ReturnsNull_WhenApiReturnsNoResults()
    {
        _geocoder
            .SearchZipCodesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakeGeoResponse());

        var result = await CreateSut().GeocodeAsync("UnknownPlace");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GeocodeAsync_ReturnsNull_WhenApiCallFails()
    {
        _geocoder
            .SearchZipCodesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<IEnumerable<ZipCodeSearchResponse>>(
                new HttpResponseMessage(HttpStatusCode.InternalServerError), null, new RefitSettings()));

        var result = await CreateSut().GeocodeAsync("Randers");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GeocodeAsync_ReturnsCityAndCoordinates_WhenApiSucceeds()
    {
        _geocoder
            .SearchZipCodesAsync("Randers", Arg.Any<CancellationToken>())
            .Returns(MakeGeoResponse(MakeZip("Randers", 8900, lat: 56.46, lng: 10.03)));

        var result = await CreateSut().GeocodeAsync("Randers");

        result.Should().NotBeNull();
        result!.City.Should().Be("Randers");
        result.ZipCode.Should().Be(8900);
        result.Latitude.Should().BeApproximately(56.46, 0.001);
        result.Longitude.Should().BeApproximately(10.03, 0.001);
    }

    [Fact]
    public async Task GeocodeAsync_ParsesZipCode_FromStringNr()
    {
        _geocoder
            .SearchZipCodesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakeGeoResponse(MakeZip("København", 1000, lat: 55.67, lng: 12.57)));

        var result = await CreateSut().GeocodeAsync("København");

        result!.ZipCode.Should().Be(1000);
    }

    [Fact]
    public async Task GeocodeAsync_ReturnsNullZipCode_WhenNrIsNotParseable()
    {
        var zip = new ZipCodeSearchResponse(
            Href: null, Nr: "not-a-number", Navn: "SomeCity",
            Stormodtageradresser: null, Bbox: null,
            Visueltcenter: [10f, 55f],
            Kommuner: null, Ændret: DateTime.UtcNow, Geo_Ændret: DateTime.UtcNow,
            Geo_Version: 1, Dagi_Id: null);

        _geocoder
            .SearchZipCodesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakeGeoResponse(zip));

        var result = await CreateSut().GeocodeAsync("SomeCity");

        result.Should().NotBeNull();
        result!.ZipCode.Should().BeNull();
    }

    [Fact]
    public async Task GeocodeAsync_PassesQueryDirectlyToClient()
    {
        _geocoder
            .SearchZipCodesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakeGeoResponse());

        await CreateSut().GeocodeAsync("Silkeborg");

        await _geocoder.Received(1).SearchZipCodesAsync("Silkeborg", Arg.Any<CancellationToken>());
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

    private static ZipCodeSearchResponse MakeZip(string navn, int nr, double lat, double lng) =>
        new(Href: null, Nr: nr.ToString("D4"), Navn: navn,
            Stormodtageradresser: null, Bbox: null,
            Visueltcenter: [(float)lng, (float)lat],   // GeoJSON order: [lng, lat]
            Kommuner: null, Ændret: DateTime.UtcNow, Geo_Ændret: DateTime.UtcNow,
            Geo_Version: 1, Dagi_Id: null);

    private static IApiResponse<IEnumerable<ZipCodeSearchResponse>> MakeGeoResponse(params ZipCodeSearchResponse[] results) =>
        new ApiResponse<IEnumerable<ZipCodeSearchResponse>>(
            new HttpResponseMessage(HttpStatusCode.OK),
            results,
            new RefitSettings());

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

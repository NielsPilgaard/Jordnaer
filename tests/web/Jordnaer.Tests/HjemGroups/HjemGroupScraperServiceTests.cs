using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Jordnaer.Features.HjemGroups;
using Jordnaer.Shared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Refit;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Jordnaer.Tests.HjemGroups;

public class HjemGroupScraperServiceTests
{
    private readonly IDataForsyningenClient _dataForsyningenClient = Substitute.For<IDataForsyningenClient>();
    private readonly BlobServiceClient _blobServiceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _containerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();
    private readonly ILogger<HjemGroupScraperService> _logger = Substitute.For<ILogger<HjemGroupScraperService>>();

    private const string LokalafdelingerHtml = """
        <html><body>
          <nav>
            <a href="/lokalafdelinger">Lokalafdelinger</a>
            <a href="/om">Om HJEM</a>
          </nav>
          <main>
            <a href="/randers">Randers</a>
            <a href="/aarhus">Aarhus</a>
            <a href="/koebenhavn">København</a>
          </main>
        </body></html>
        """;

    private const string LokalreprasentanterHtml = """
        <html><body>
          <ul>
            <li>Odense</li>
            <li>Esbjerg</li>
          </ul>
        </body></html>
        """;

    private const string EmptyHtml = "<html><body></body></html>";

    public HjemGroupScraperServiceTests()
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

    private HjemGroupScraperService CreateSut(string lokAfdelHtml, string lokRepHtml)
    {
        var handler = new FakeHttpMessageHandler(lokAfdelHtml, lokRepHtml);
        var httpClient = new HttpClient(handler);
        return new HjemGroupScraperService(httpClient, _dataForsyningenClient, _blobServiceClient, _logger);
    }

    private static IApiResponse<IEnumerable<ZipCodeSearchResponse>> MakeGeoResponse(params ZipCodeSearchResponse[] results) =>
        new ApiResponse<IEnumerable<ZipCodeSearchResponse>>(
            new HttpResponseMessage(HttpStatusCode.OK),
            results,
            new RefitSettings());

    private static IApiResponse<IEnumerable<ZipCodeSearchResponse>> MakeEmptyGeoResponse() =>
        new ApiResponse<IEnumerable<ZipCodeSearchResponse>>(
            new HttpResponseMessage(HttpStatusCode.OK),
            [],
            new RefitSettings());

    private static ZipCodeSearchResponse MakeZip(string navn, int nr, double lat, double lng) =>
        new(Href: null, Nr: nr.ToString("D4"), Navn: navn,
            Stormodtageradresser: null, Bbox: null,
            Visueltcenter: [(float)lng, (float)lat],
            Kommuner: null, Ændret: DateTime.UtcNow, Geo_Ændret: DateTime.UtcNow,
            Geo_Version: 1, Dagi_Id: null);

    private void SetupGeocodeSuccessForAll(string cityName, int zip, double lat, double lng)
    {
        _dataForsyningenClient
            .SearchZipCodesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakeGeoResponse(MakeZip(cityName, zip, lat, lng)));
    }

    private void SetupGeocodeEmpty()
    {
        _dataForsyningenClient
            .SearchZipCodesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakeEmptyGeoResponse());
    }

    // Captures the uploaded JSON from the blob upload call
    private string CaptureUploadedJson()
    {
        var captured = string.Empty;
        _blobClient
            .UploadAsync(Arg.Do<Stream>(s =>
            {
                using var sr = new StreamReader(s, Encoding.UTF8);
                captured = sr.ReadToEnd();
            }), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<Response<BlobContentInfo>>()));
        return captured; // note: populated after the act
    }

    [Fact]
    public async Task ScrapeAndSaveAsync_SavesBlob_WhenAtLeastOneEntryGeocoded()
    {
        // Arrange
        SetupGeocodeSuccessForAll("Randers", 8900, 56.46, 10.03);
        var sut = CreateSut(LokalafdelingerHtml, LokalreprasentanterHtml);

        // Act
        await sut.ScrapeAndSaveAsync();

        // Assert
        await _blobClient.Received().UploadAsync(Arg.Any<Stream>(), overwrite: true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScrapeAndSaveAsync_DoesNotSaveBlob_WhenGeocodeYieldsZeroResults()
    {
        // Arrange
        SetupGeocodeEmpty();
        var sut = CreateSut(LokalafdelingerHtml, LokalreprasentanterHtml);

        // Act
        await sut.ScrapeAndSaveAsync();

        // Assert: must not overwrite blob — preserves previous version
        await _blobClient.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScrapeAndSaveAsync_DoesNotSaveBlob_WhenHtmlIsEmpty()
    {
        // Arrange
        SetupGeocodeSuccessForAll("SomeCity", 1234, 55.0, 10.0);
        var sut = CreateSut(EmptyHtml, EmptyHtml);

        // Act
        await sut.ScrapeAndSaveAsync();

        // Assert
        await _blobClient.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScrapeAndSaveAsync_SkipsNavLinks_FromLokalafdelingerPage()
    {
        // Arrange - page has only skip-listed nav links
        const string navOnlyHtml = """
            <html><body>
              <a href="/lokalafdelinger">Lokalafdelinger</a>
              <a href="/om">Om HJEM</a>
              <a href="/kontakt">Kontakt</a>
            </body></html>
            """;
        SetupGeocodeSuccessForAll("City", 1000, 55.0, 10.0);
        var sut = CreateSut(navOnlyHtml, EmptyHtml);

        // Act
        await sut.ScrapeAndSaveAsync();

        // Assert: no content entries → no upload
        await _blobClient.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScrapeAndSaveAsync_DeduplicatesHrefs()
    {
        // Arrange - same href appears twice
        const string duplicateHtml = """
            <html><body>
              <a href="/randers">Randers</a>
              <a href="/randers">Randers (duplicate)</a>
            </body></html>
            """;
        SetupGeocodeSuccessForAll("Randers", 8900, 56.46, 10.03);
        var sut = CreateSut(duplicateHtml, EmptyHtml);

        string uploadedJson = string.Empty;
        _blobClient
            .UploadAsync(Arg.Do<Stream>(s =>
            {
                using var sr = new StreamReader(s, Encoding.UTF8);
                uploadedJson = sr.ReadToEnd();
            }), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<Response<BlobContentInfo>>()));

        // Act
        await sut.ScrapeAndSaveAsync();

        // Assert: only one entry for /randers
        var entries = JsonSerializer.Deserialize<List<HjemGroupEntry>>(uploadedJson,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        entries.Should().NotBeNull();
        entries!.Count(e => e.WebsiteUrl == new Uri("https://www.hjemlo.dk/randers")).Should().Be(1);
    }

    [Fact]
    public async Task ScrapeAndSaveAsync_SavesLokalafdeling_WithCorrectType()
    {
        // Arrange
        SetupGeocodeSuccessForAll("Randers", 8900, 56.46, 10.03);
        var sut = CreateSut(LokalafdelingerHtml, EmptyHtml);

        string uploadedJson = string.Empty;
        _blobClient
            .UploadAsync(Arg.Do<Stream>(s =>
            {
                using var sr = new StreamReader(s, Encoding.UTF8);
                uploadedJson = sr.ReadToEnd();
            }), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<Response<BlobContentInfo>>()));

        // Act
        await sut.ScrapeAndSaveAsync();

        // Assert
        var entries = JsonSerializer.Deserialize<List<HjemGroupEntry>>(uploadedJson,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        entries.Should().NotBeNull().And.NotBeEmpty();
        entries!.Should().AllSatisfy(e => e.Type.Should().Be(HjemGroupType.Lokalafdeling));
    }

    [Fact]
    public async Task ScrapeAndSaveAsync_DoesNotThrow_WhenHttpClientFails()
    {
        // Arrange
        var httpClient = new HttpClient(new FailingHttpMessageHandler());
        var sut = new HjemGroupScraperService(httpClient, _dataForsyningenClient, _blobServiceClient, _logger);

        // Act & Assert: must swallow exceptions internally
        await FluentActions.Awaiting(() => sut.ScrapeAndSaveAsync()).Should().NotThrowAsync();
        await _blobClient.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScrapeAndSaveAsync_SavesValidJson_ToBlob()
    {
        // Arrange
        SetupGeocodeSuccessForAll("Randers", 8900, 56.46, 10.03);
        var sut = CreateSut(LokalafdelingerHtml, EmptyHtml);

        string uploadedJson = string.Empty;
        _blobClient
            .UploadAsync(Arg.Do<Stream>(s =>
            {
                using var sr = new StreamReader(s, Encoding.UTF8);
                uploadedJson = sr.ReadToEnd();
            }), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<Response<BlobContentInfo>>()));

        // Act
        await sut.ScrapeAndSaveAsync();

        // Assert: valid deserializable JSON
        var act = () => JsonSerializer.Deserialize<List<HjemGroupEntry>>(
            uploadedJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        act.Should().NotThrow();
        act()!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ScrapeAndSaveAsync_CallsGeocodeForEachUniqueHref()
    {
        // Arrange - three unique hrefs
        SetupGeocodeSuccessForAll("City", 1000, 55.0, 10.0);
        var sut = CreateSut(LokalafdelingerHtml, EmptyHtml); // has /randers, /aarhus, /koebenhavn

        // Act
        await sut.ScrapeAndSaveAsync();

        // Assert: geocoded for each unique city name
        await _dataForsyningenClient.Received(3).SearchZipCodesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // --- Fakes ---

    private sealed class FakeHttpMessageHandler(string lokAfdelHtml, string lokRepHtml) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var html = request.RequestUri?.AbsolutePath.Contains("lokalrepraesentanter") == true
                ? lokRepHtml
                : lokAfdelHtml;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            });
        }
    }

    private sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }
}

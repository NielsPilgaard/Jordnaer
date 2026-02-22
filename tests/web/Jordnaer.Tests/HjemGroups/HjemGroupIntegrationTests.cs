using Azure.Storage.Blobs;
using FluentAssertions;
using Jordnaer.Features.HjemGroups;
using Jordnaer.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Refit;
using System.Net;
using System.Text;
using System.Text.Json;
using Testcontainers.Azurite;
using Xunit;

namespace Jordnaer.Tests.HjemGroups;

/// <summary>
/// Integration tests for the HJEM scraper pipeline using a real Azurite blob container.
/// Geocoding uses a fake IDataForsyningenClient to avoid external API calls in CI.
/// </summary>
public class HjemGroupIntegrationTests : IAsyncLifetime
{
    private readonly AzuriteContainer _azurite = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
        .WithInMemoryPersistence()
        .WithCommand("--skipApiVersionCheck")
        .Build();

    private BlobServiceClient _blobServiceClient = null!;

    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InitializeAsync()
    {
        await _azurite.StartAsync();
        _blobServiceClient = new BlobServiceClient(_azurite.GetConnectionString());
    }

    public async Task DisposeAsync() => await _azurite.DisposeAsync();

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private HjemGroupScraperService CreateScraper(string lokAfdelHtml, string lokRepHtml, IDataForsyningenClient? geocoder = null)
    {
        var handler = new HtmlFakeHandler(lokAfdelHtml, lokRepHtml);
        var httpClient = new HttpClient(handler);
        geocoder ??= new AlwaysSuccessGeocoder();
        return new HjemGroupScraperService(httpClient, geocoder, _blobServiceClient, NullLogger<HjemGroupScraperService>.Instance);
    }

    private HjemGroupProvider CreateProvider() =>
        new(_blobServiceClient, NullLogger<HjemGroupProvider>.Instance);

    private const string SingleCityHtml = """
        <html><body><a href="/randers">Randers</a></body></html>
        """;

    private const string TwoCitiesHtml = """
        <html><body>
          <a href="/randers">Randers</a>
          <a href="/aarhus">Aarhus</a>
        </body></html>
        """;

    private const string Empty = "<html><body></body></html>";

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FullPipeline_ScrapeSave_ThenProviderReadsMarkers()
    {
        // Arrange
        var scraper = CreateScraper(SingleCityHtml, Empty);
        var provider = CreateProvider();

        // Act
        await scraper.ScrapeAndSaveAsync();
        var markers = await provider.GetMarkersAsync();

        // Assert
        markers.Should().HaveCount(1);
        markers[0].Name.Should().Be("Randers");
        markers[0].ShortDescription.Should().Be("HJEM lokalafdeling");
        markers[0].Latitude.Should().Be(56.0);
        markers[0].Longitude.Should().Be(10.0);
    }

    [Fact]
    public async Task FullPipeline_MultipleCities_AllAppearAsMarkers()
    {
        // Arrange
        var scraper = CreateScraper(TwoCitiesHtml, Empty);
        var provider = CreateProvider();

        // Act
        await scraper.ScrapeAndSaveAsync();
        var markers = await provider.GetMarkersAsync();

        // Assert
        markers.Should().HaveCount(2);
        markers.Select(m => m.Name).Should().BeEquivalentTo(["Randers", "Aarhus"]);
    }

    [Fact]
    public async Task FullPipeline_BlobPreserved_WhenSecondScrapeYieldsNothing()
    {
        // Arrange: first successful scrape
        var scraper = CreateScraper(SingleCityHtml, Empty);
        await scraper.ScrapeAndSaveAsync();

        // Second scraper — geocoding always returns empty (simulates failure)
        var failingScraper = CreateScraper(SingleCityHtml, Empty, geocoder: new AlwaysEmptyGeocoder());
        await failingScraper.ScrapeAndSaveAsync();

        // Provider should still return data from the first run
        var provider = CreateProvider();
        var markers = await provider.GetMarkersAsync();

        markers.Should().HaveCount(1, "previous blob must be preserved when scrape yields zero results");
    }

    [Fact]
    public async Task FullPipeline_BlobOverwritten_WhenNewScrapeSucceeds()
    {
        // Arrange: first scrape — one city
        await CreateScraper(SingleCityHtml, Empty).ScrapeAndSaveAsync();

        // Second scrape — two cities
        await CreateScraper(TwoCitiesHtml, Empty).ScrapeAndSaveAsync();

        var markers = await CreateProvider().GetMarkersAsync();

        markers.Should().HaveCount(2, "blob should be overwritten with the newer result");
    }

    [Fact]
    public async Task Provider_ReturnsEmpty_BeforeScrapeHasEverRun()
    {
        // Arrange: fresh Azurite — no blob uploaded
        var provider = CreateProvider();

        // Act
        var markers = await provider.GetMarkersAsync();

        // Assert
        markers.Should().BeEmpty();
    }

    [Fact]
    public async Task FullPipeline_MarkersHaveStableIds_AcrossProviderInstances()
    {
        // Arrange
        await CreateScraper(SingleCityHtml, Empty).ScrapeAndSaveAsync();

        // Act: two separate provider instances read the same blob
        var markers1 = await CreateProvider().GetMarkersAsync();
        var markers2 = await CreateProvider().GetMarkersAsync();

        // Assert
        markers1[0].Id.Should().Be(markers2[0].Id);
        markers1[0].Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task FullPipeline_LokalreprasentanterGetCorrectShortDescription()
    {
        // Arrange
        const string repHtml = """
            <html><body><ul><li>Odense</li></ul></body></html>
            """;
        await CreateScraper(Empty, repHtml).ScrapeAndSaveAsync();

        var markers = await CreateProvider().GetMarkersAsync();

        // Assert
        markers.Should().ContainSingle();
        markers[0].ShortDescription.Should().Be("HJEM lokalrepræsentant");
    }

    [Fact]
    public async Task FullPipeline_BlobContainsValidJson()
    {
        // Arrange
        await CreateScraper(SingleCityHtml, Empty).ScrapeAndSaveAsync();

        // Act: read raw blob content
        var containerClient = _blobServiceClient.GetBlobContainerClient("hjemlo-groups");
        var blobClient = containerClient.GetBlobClient("groups.json");
        var download = await blobClient.DownloadContentAsync();
        var json = download.Value.Content.ToString();

        // Assert
        var act = () => JsonSerializer.Deserialize<List<HjemGroupEntry>>(json, CamelCase);
        act.Should().NotThrow();
        act()!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FullPipeline_WebsiteUrl_IsFullHjemloUrl()
    {
        // Arrange
        await CreateScraper(SingleCityHtml, Empty).ScrapeAndSaveAsync();

        var markers = await CreateProvider().GetMarkersAsync();

        // Assert
        markers[0].WebsiteUrl.Should().StartWith("https://www.hjemlo.dk/");
    }

    [Fact]
    public async Task FullPipeline_Coordinates_AreFromGeocoderResult()
    {
        // Arrange - AlwaysSuccessGeocoder returns lat=56, lng=10
        await CreateScraper(SingleCityHtml, Empty).ScrapeAndSaveAsync();
        var markers = await CreateProvider().GetMarkersAsync();

        // Assert
        markers[0].Latitude.Should().Be(56.0);
        markers[0].Longitude.Should().Be(10.0);
    }

    // -------------------------------------------------------------------------
    // Fake helpers
    // -------------------------------------------------------------------------

    private sealed class HtmlFakeHandler(string lokAfdelHtml, string lokRepHtml) : HttpMessageHandler
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

    /// <summary>Always returns a fixed geocode result for any query.</summary>
    private sealed class AlwaysSuccessGeocoder : IDataForsyningenClient
    {
        private static readonly ZipCodeSearchResponse FakeResult = new(
            Href: null, Nr: "8900", Navn: "Randers",
            Stormodtageradresser: null, Bbox: null,
            Visueltcenter: [10f, 56f],  // GeoJSON: [lng, lat]
            Kommuner: null,
            Ændret: DateTime.UtcNow, Geo_Ændret: DateTime.UtcNow,
            Geo_Version: 1, Dagi_Id: null);

        public Task<IApiResponse<IEnumerable<AddressAutoCompleteResponse>>> GetAddressesWithAutoComplete(string? query, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IApiResponse<IEnumerable<ZipCodeAutoCompleteResponse>>> GetZipCodesWithAutoComplete(string? query, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IApiResponse<ZipCodeSearchResponse>> GetZipCodeFromCoordinates(string longitude, string latitude, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IApiResponse<IEnumerable<ZipCodeSearchResponse>>> GetZipCodesWithinCircle(string? circle, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IApiResponse<IEnumerable<ZipCodeSearchResponse>>> SearchZipCodesAsync(string query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IApiResponse<IEnumerable<ZipCodeSearchResponse>>>(
                new ApiResponse<IEnumerable<ZipCodeSearchResponse>>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    [FakeResult],
                    new RefitSettings()));
    }

    /// <summary>Always returns no geocode results (simulates failed geocoding).</summary>
    private sealed class AlwaysEmptyGeocoder : IDataForsyningenClient
    {
        public Task<IApiResponse<IEnumerable<AddressAutoCompleteResponse>>> GetAddressesWithAutoComplete(string? query, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IApiResponse<IEnumerable<ZipCodeAutoCompleteResponse>>> GetZipCodesWithAutoComplete(string? query, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IApiResponse<ZipCodeSearchResponse>> GetZipCodeFromCoordinates(string longitude, string latitude, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IApiResponse<IEnumerable<ZipCodeSearchResponse>>> GetZipCodesWithinCircle(string? circle, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IApiResponse<IEnumerable<ZipCodeSearchResponse>>> SearchZipCodesAsync(string query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IApiResponse<IEnumerable<ZipCodeSearchResponse>>>(
                new ApiResponse<IEnumerable<ZipCodeSearchResponse>>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    [],
                    new RefitSettings()));
    }
}

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HtmlAgilityPack;
using Jordnaer.Shared;
using System.Text.Json;

namespace Jordnaer.Features.HjemGroups;

public class HjemGroupScraperService(
    HttpClient httpClient,
    IDataForsyningenClient dataForsyningenClient,
    BlobServiceClient blobServiceClient,
    ILogger<HjemGroupScraperService> logger)
{
    private const string ContainerName = "hjemlo-groups";
    private const string BlobName = "groups.json";
    private const string LokalafdjelingerUrl = "https://www.hjemlo.dk/lokalafdelinger";
    private const string LokalreprasentanterUrl = "https://www.hjemlo.dk/lokalrepraesentanter";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task ScrapeAndSaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = new List<HjemGroupEntry>();

            var lokalafdelinger = await ScrapeLokalafdelingerAsync(cancellationToken);
            entries.AddRange(lokalafdelinger);

            var lokalreprasentanter = await ScrapeLokalreprasentanterAsync(cancellationToken);
            entries.AddRange(lokalreprasentanter);

            if (entries.Count == 0)
            {
                logger.LogWarning("Scraping hjemlo.dk yielded zero results. Skipping blob upload to preserve previous version.");
                return;
            }

            await SaveToBlobAsync(entries, cancellationToken);
            logger.LogInformation("Saved {Count} HJEM entries to blob storage.", entries.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to scrape and save HJEM groups.");
        }
    }

    private async Task<List<HjemGroupEntry>> ScrapeLokalafdelingerAsync(CancellationToken cancellationToken)
    {
        var results = new List<HjemGroupEntry>();

        string html;
        try
        {
            html = await httpClient.GetStringAsync(LokalafdjelingerUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch lokalafdelinger page.");
            return results;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Look for anchor elements with hrefs like /randers, /koebenhavn, etc.
        var anchors = doc.DocumentNode.SelectNodes("//a[starts-with(@href, '/') and string-length(@href) > 1]");
        if (anchors is null || anchors.Count == 0)
        {
            logger.LogWarning("No lokalafdeling anchors found on page — page may require JavaScript rendering.");
            return results;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var anchor in anchors)
        {
            var href = anchor.GetAttributeValue("href", "").Trim();
            var name = HtmlEntity.DeEntitize(anchor.InnerText).Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href))
                continue;

            // Skip obvious navigation links (about, contact, etc.)
            var skipPaths = new[] { "/lokalafdelinger", "/lokalrepraesentanter", "/om", "/kontakt", "/blog", "/login", "/signup", "/search" };
            if (skipPaths.Any(s => href.Equals(s, StringComparison.OrdinalIgnoreCase)))
                continue;

            // Only single-segment relative paths like /randers
            if (href.Count(c => c == '/') != 1)
                continue;

            if (!seen.Add(href))
                continue;

            var geocoded = await GeocodeAsync(name, cancellationToken);
            if (geocoded is null)
            {
                logger.LogWarning("Could not geocode lokalafdeling: {Name}", name);
                continue;
            }

            results.Add(new HjemGroupEntry
            {
                Name = name,
                WebsiteUrl = $"https://www.hjemlo.dk{href}",
                City = geocoded.City,
                ZipCode = geocoded.ZipCode,
                Latitude = geocoded.Latitude,
                Longitude = geocoded.Longitude,
                Type = HjemGroupType.Lokalafdeling,
            });
        }

        logger.LogInformation("Scraped {Count} lokalafdelinger.", results.Count);
        return results;
    }

    private async Task<List<HjemGroupEntry>> ScrapeLokalreprasentanterAsync(CancellationToken cancellationToken)
    {
        var results = new List<HjemGroupEntry>();

        string html;
        try
        {
            html = await httpClient.GetStringAsync(LokalreprasentanterUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch lokalrepræsentanter page.");
            return results;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Look for text nodes / elements that reference city/area names.
        var candidates = doc.DocumentNode.SelectNodes("//li | //p | //div[@class]");
        if (candidates is null || candidates.Count == 0)
        {
            logger.LogWarning("No lokalrepræsentant candidates found on page — page may require JavaScript rendering.");
            return results;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in candidates)
        {
            var text = HtmlEntity.DeEntitize(node.InnerText).Trim();

            if (string.IsNullOrWhiteSpace(text) || text.Length > 100 || text.Length < 3)
                continue;

            // Skip if contains newlines or many words (likely a paragraph)
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length > 2)
                continue;

            var singleLine = lines[0];
            if (string.IsNullOrWhiteSpace(singleLine) || singleLine.Length > 60)
                continue;

            if (!seen.Add(singleLine))
                continue;

            var geocoded = await GeocodeAsync(singleLine, cancellationToken);
            if (geocoded is null)
                continue;

            results.Add(new HjemGroupEntry
            {
                Name = singleLine,
                WebsiteUrl = LokalreprasentanterUrl,
                City = geocoded.City,
                ZipCode = geocoded.ZipCode,
                Latitude = geocoded.Latitude,
                Longitude = geocoded.Longitude,
                Type = HjemGroupType.Lokalrepresentant,
            });
        }

        logger.LogInformation("Scraped {Count} lokalrepræsentanter.", results.Count);
        return results;
    }

    private async Task<GeocodedResult?> GeocodeAsync(string locationText, CancellationToken cancellationToken)
    {
        // Try the name as-is first, then with common suffixes stripped
        var candidates = new List<string> { locationText };

        var suffixes = new[] { " lokalafdeling", " lokalrepræsentant", " kommune", " by" };
        foreach (var suffix in suffixes)
        {
            if (locationText.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(locationText[..^suffix.Length].Trim());
            }
        }

        foreach (var query in candidates)
        {
            var result = await TryGeocodeAsync(query, cancellationToken);
            if (result is not null)
                return result;
        }

        return null;
    }

    private async Task<GeocodedResult?> TryGeocodeAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var response = await dataForsyningenClient.SearchZipCodesAsync(query, cancellationToken);

            if (!response.IsSuccessStatusCode || response.Content is null)
                return null;

            var first = response.Content.FirstOrDefault();
            if (first.Navn is null)
                return null;

            if (first.Visueltcenter is not { Length: >= 2 })
                return null;

            // GeoJSON order: [longitude, latitude]
            var longitude = (double)first.Visueltcenter[0];
            var latitude = (double)first.Visueltcenter[1];

            int? zipCode = null;
            if (int.TryParse(first.Nr, out var parsedZip))
                zipCode = parsedZip;

            return new GeocodedResult(first.Navn, zipCode, latitude, longitude);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Geocoding failed for query: {Query}", query);
            return null;
        }
    }

    private async Task SaveToBlobAsync(List<HjemGroupEntry> entries, CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(BlobName);
        var json = JsonSerializer.Serialize(entries, JsonOptions);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);
    }

    private sealed record GeocodedResult(string? City, int? ZipCode, double Latitude, double Longitude);
}

# HJEM Lokalafdelinger & Lokalrepræsentanter on Group Map — Task Description

> **Prerequisite**: This task must not be started until **task `00-group-search.md` is complete**. It depends on the map-first group search page produced by that task.

---

## Overview

HJEM's local chapters (*lokalafdelinger*) and local representatives (*lokalrepræsentanter*) should appear as markers on the `/groups/discover` map alongside real Mini Møder groups.

They are not real database-backed groups. They are **hardcoded/scraped external entries** that get merged into the map's `GroupMarkerData` list at render time. They must:

- Appear as markers on the Leaflet map.
- Show a popup with name, location, and a link to the relevant hjemlo.dk page.
- Be excluded from the "total count" of Mini Møder groups.
- Be kept up-to-date via a **daily background scrape** of hjemlo.dk, with results stored in **Azure Blob Storage** so the previous version survives a failed scrape.
- Be filtered by the reactive name filter (client-side, since they're injected after the service call).

---

## Data Sources

### 1. Lokalafdelinger — `https://www.hjemlo.dk/lokalafdelinger`

Scrape the dropdown list of all lokalafdelinger. For each entry collect:

| Field | Source |
|-------|--------|
| `Name` | Text of the lokalafdeling (e.g. `"Randers"`) |
| `WebsiteUrl` | The direct page URL on hjemlo.dk (e.g. `https://www.hjemlo.dk/randers`) |
| `City` / `ZipCode` | Derive from the name via Dataforsyningen geocoding (see below) |
| `Latitude` / `Longitude` | From Dataforsyningen geocoding |

### 2. Lokalrepræsentanter — `https://www.hjemlo.dk/lokalrepraesentanter`

Scrape the list of local representatives. For each entry collect:

| Field | Source |
|-------|--------|
| `Name` | Representative name / area name visible on the page |
| `WebsiteUrl` | Always `https://www.hjemlo.dk/lokalrepraesentanter` (fixed for all) |
| `City` / `ZipCode` | Extracted from the page text next to each entry |
| `Latitude` / `Longitude` | From Dataforsyningen geocoding |

---

## Architecture

### New files to create

```
src/web/Jordnaer/Features/HjemGroups/
    HjemGroupEntry.cs                   # Record: scraped data model
    HjemGroupScraperService.cs          # Scrapes hjemlo.dk + geocodes + saves to blob
    HjemGroupProvider.cs                # Reads from blob; returns List<GroupMarkerData>
    HjemGroupScraperBackgroundService.cs # Daily BackgroundService host
    WebApplicationBuilderExtensions.cs  # DI registration
```

### Existing files to modify

| File | Change |
|------|--------|
| [GroupSearch.razor](../src/web/Jordnaer/Pages/GroupSearch/GroupSearch.razor) | Inject `IHjemGroupProvider`; merge hjemlo markers after `GetGroupsAsync`; apply name filter client-side to them |
| [Program.cs](../src/web/Jordnaer/Program.cs) | Call `builder.AddHjemGroupServices()` |

---

## Step-by-Step Implementation

### Step 1 — `HjemGroupEntry` record

```csharp
// Features/HjemGroups/HjemGroupEntry.cs
namespace Jordnaer.Features.HjemGroups;

public record HjemGroupEntry
{
    public required string Name { get; init; }
    public required string WebsiteUrl { get; init; }
    public string? City { get; init; }
    public int? ZipCode { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required HjemGroupType Type { get; init; }
}

public enum HjemGroupType { Lokalafdeling, Lokalrepresentant }
```

The blob stores `List<HjemGroupEntry>` serialised as JSON.

---

### Step 2 — `HjemGroupScraperService`

Inject `HttpClient` (named client, see DI step), `BlobServiceClient`, and `ILogger`.

#### 2a. Scraping

Use `HttpClient` with a browser-like `User-Agent` header to `GET` each page. Parse HTML with **HtmlAgilityPack** (add the NuGet package `HtmlAgilityPack`).

- **Lokalafdelinger page**: The dropdown or link list contains anchor elements with lokalafdeling names and relative or absolute paths like `/randers`. Extract `(name, href)` pairs. Normalise href to a full `https://www.hjemlo.dk/...` URL.
- **Lokalrepræsentanter page**: Each representative entry includes a location string (city or municipality). Extract `(name, locationText)` pairs.

If the Wix page requires JavaScript to render its content (the initial HTML is minimal), fall back to the following strategy: load the page, check if the expected elements are present; if not, log a warning and **return without overwriting the blob** so the previous version stays intact.

#### 2b. Geocoding with Dataforsyningen

For each scraped `locationText` (city or area name), call:

```
GET https://api.dataforsyningen.dk/postnumre?q={Uri.EscapeDataString(locationText)}
```

The response is a JSON array. Take the first result. Key fields:
- `navn` — city name (string)
- `nr` — zip code as string (parse to `int`)
- `visueltcenter` — `[longitude, latitude]` array (note: GeoJSON order — lng first, lat second)

If no result is found for the lokalafdeling name directly, try stripping suffixes like " lokalafdeling" and retry.

#### 2c. Saving to Blob Storage

Container: `"hjemlo-groups"` (create if not exists, `PublicAccessType.Blob`).
Blob name: `"groups.json"`.

Serialise with `System.Text.Json` (camelCase, write indented). Upload with `overwrite: true`.

Only write to the blob if at least one entry was successfully scraped and geocoded. If scraping yielded zero results (likely a transient failure), log a warning and skip the upload so the previous version remains.

---

### Step 3 — `HjemGroupProvider`

Reads from the blob on demand. Cache the result in-process for the duration of one request (scoped service, so it loads once per Blazor circuit initialisation).

```csharp
public interface IHjemGroupProvider
{
    Task<IReadOnlyList<GroupMarkerData>> GetMarkersAsync(CancellationToken cancellationToken = default);
}
```

Implementation:
1. Get `BlobContainerClient` for `"hjemlo-groups"`.
2. If the container or blob does not exist, return an empty list (the scraper hasn't run yet).
3. Download and deserialise `groups.json` to `List<HjemGroupEntry>`.
4. Map each entry to `GroupMarkerData`:
   - `Id` = deterministic `Guid` derived from the entry's `WebsiteUrl + Name` via `Guid.CreateVersion5` / or `new Guid(MD5.HashData(Encoding.UTF8.GetBytes(entry.WebsiteUrl + entry.Name)).Take(16).ToArray())` — a stable fake ID so the marker identity is consistent across renders.
   - `Name` = `entry.Name`
   - `ProfilePictureUrl` = `null` (will show initial letter on map)
   - `WebsiteUrl` = `entry.WebsiteUrl`
   - `ShortDescription` = `entry.Type == HjemGroupType.Lokalafdeling ? "HJEM lokalafdeling" : "HJEM lokalrepræsentant"`
   - `ZipCode` = `entry.ZipCode`
   - `City` = `entry.City`
   - `Latitude` = `entry.Latitude`
   - `Longitude` = `entry.Longitude`

---

### Step 4 — `HjemGroupScraperBackgroundService`

Follow the same pattern as [NotificationCleanupBackgroundService.cs](../src/web/Jordnaer/Features/Notifications/NotificationCleanupBackgroundService.cs):

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // Run immediately on startup, then every 24 hours
    while (!stoppingToken.IsCancellationRequested)
    {
        await _scraperService.ScrapeAndSaveAsync(stoppingToken);
        await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
    }
}
```

---

### Step 5 — DI Registration

```csharp
// Features/HjemGroups/WebApplicationBuilderExtensions.cs
public static WebApplicationBuilder AddHjemGroupServices(this WebApplicationBuilder builder)
{
    builder.Services.AddHttpClient<HjemGroupScraperService>(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (compatible; MiniMoeder/1.0)");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    builder.Services.AddScoped<IHjemGroupProvider, HjemGroupProvider>();
    builder.Services.AddHostedService<HjemGroupScraperBackgroundService>();

    return builder;
}
```

Register in [Program.cs](../src/web/Jordnaer/Program.cs):

```csharp
builder.AddHjemGroupServices();
```

---

### Step 6 — Merge into `GroupSearch.razor`

After the revamp from task 00, `GroupSearch.razor` fetches all groups via `GetGroupsAsync` and passes the result to `GroupSearchForm`. Add the following:

1. Inject `IHjemGroupProvider`.
2. In `OnInitializedAsync`, load hjemlo markers once:
   ```csharp
   _hjemMarkers = await HjemGroupProvider.GetMarkersAsync();
   ```
3. When building the list of `GroupMarkerData` to pass to the map (in `GroupSearchForm`'s `OnParametersSet`, after task 00's changes), the hjemlo markers should be passed **in addition** to the regular group markers.

   The cleanest approach: add a second `[Parameter]` on `GroupSearchForm` (or directly on `MapSearchFilter`) called `AdditionalMarkers` of type `IReadOnlyList<GroupMarkerData>`. These are always shown on the map regardless of the active filter (since they're not in the database) **but** are filtered client-side by the active name filter: only include hjemlo markers whose `Name` contains the current name filter text (case-insensitive), or include all if the name filter is empty.

   In the `OnParametersSet` of `GroupSearchForm`, merge them:
   ```csharp
   _groupMarkers = regularMarkers.Concat(
       filteredHjemMarkers  // already filtered by name above
   ).ToList();
   ```

4. The `TotalCount` shown anywhere in the UI must still reflect only Mini Møder groups (i.e., do not add hjemlo count to it).

---

## Blob Storage Notes

- The `BlobServiceClient` is already registered in DI (used by `ImageService`). Inject it directly in `HjemGroupScraperService` and `HjemGroupProvider`.
- Container `"hjemlo-groups"` is separate from the image containers. Create it with `PublicAccessType.Blob` so the blob can be read without a SAS token (same pattern as images).
- On local development with Azurite, this works identically to production.

---

## File Reference Summary

| File | Action |
|------|--------|
| [GroupSearch.razor](../src/web/Jordnaer/Pages/GroupSearch/GroupSearch.razor) | Inject `IHjemGroupProvider`, load markers on init, pass as `AdditionalMarkers` to form |
| [GroupSearchForm.razor](../src/web/Jordnaer/Features/GroupSearch/GroupSearchForm.razor) | Accept `AdditionalMarkers` parameter; merge into `_groupMarkers` after name filtering |
| [LeafletMapInterop.cs](../src/web/Jordnaer/Features/Map/LeafletMapInterop.cs) | `GroupMarkerData` already has all needed fields — no changes |
| `Features/HjemGroups/HjemGroupEntry.cs` | **New** — scraped data model |
| `Features/HjemGroups/HjemGroupScraperService.cs` | **New** — scrapes hjemlo.dk, geocodes, writes blob |
| `Features/HjemGroups/HjemGroupProvider.cs` | **New** — reads blob, returns `GroupMarkerData` list |
| `Features/HjemGroups/HjemGroupScraperBackgroundService.cs` | **New** — daily `BackgroundService` host |
| `Features/HjemGroups/WebApplicationBuilderExtensions.cs` | **New** — DI registration |
| [Program.cs](../src/web/Jordnaer/Program.cs) | Add `builder.AddHjemGroupServices()` |

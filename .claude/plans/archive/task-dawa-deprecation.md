---
title: 'DAWA → Adressevælgeren Migration'
status: 'Implemented — pending full verify + manual smoke test'
purpose: 'Task plan for replacing DAWA after its October 1, 2026 shutdown.'
description: >-
  Replace DAWA with Adressevælgeren for address autocomplete and an embedded zip code dataset for all zip code
  lookups. Implemented 2026-10-05; final design documented in docs/dawa-migration.md.
---

# DAWA → Adressevælgeren Migration Plan

## Status: IMPLEMENTED (2026-10-05) — see `docs/dawa-migration.md`

Phase 0 resolved: docs moved to `confluence.kds.dk`. Base URL `https://adressevaelger.dk`, mandatory `token` query param (shared `adressevaelger123` until user management), search results have no coordinates (lookup `/husnumre/{id}` → EPSG:25832), **no zip code search, no reverse geocoding**. Zip codes embedded from a Wayback snapshot of DAWA `/postnumre` (2026-09-16).

Remaining: full `/verify`, manual smoke test (list at bottom).

The original plan below is kept for history.

---

## Context

DAWA (`api.dataforsyningen.dk`) shuts down **October 1, 2026 at 10:00**. The existing `docs/dawa-migration.md` is outdated (said July 1 — corrected to October 1, and listed options that are now resolved).

Key finding from code analysis: the app already does all spatial queries in SQL Server via `IsWithinDistance()`. `ZipCodeService` (which called DAWA's circle endpoint) is **dead code — never called**. Radius search doesn't go through zip codes at all.

**What we actually need from an external API**: just address + zip code text autocomplete → coordinates. Two cases:
1. Full address input (street level) → `GetAddressesWithAutoComplete` → `LocationService.GetLocationFromAddressAsync`
2. Zip code input (e.g. "8550") → `GetZipCodesWithAutoComplete` → `LocationService.GetLocationFromZipCodeAsync`

Both are replaceable. Address autocomplete → **Adressevælgeren**. Zip code autocomplete → either Adressevælgeren searches by zip too, OR a local embedded JSON/CSV of ~600 Danish zip codes (stable dataset, rarely changes).

---

## What We Know About Adressevælgeren (so far)

- `api.datafordeler.dk` returns **401 Unauthorized** on all requests — auth required
- Auth method unknown (API key? OAuth? username/password?) — docs needed
- Endpoint paths unknown — docs needed
- Response format unknown — docs needed (likely similar to DAWA given same source data)
- The Confluence docs page **is the only known doc source** and is currently down

---

## What We Still Need From External API

| Current DAWA endpoint | Status | Replacement |
|---|---|---|
| `GET /adresser/autocomplete?q=` | **KEEP** | Adressevælgeren |
| `GET /postnumre/autocomplete?q=` | **KEEP or replace** | Adressevælgeren (if it supports zip search) OR local zip table |
| `GET /postnumre/reverse?x=&y=` | Drop or keep | Used only in `ZipCodeAutoComplete` for "use my location" feature |
| `GET /postnumre?cirkel=x,y,r` | **DROP** | Dead code (`ZipCodeService` never called) |
| `GET /postnumre?q=` | **DROP** | `HjemGroupAdminService.GeocodeAsync` → replace with Adressevælgeren address search |
| `GET /postnumre?side=1&per_side=1` | **DROP** | Health ping for a service we're removing |

The "use my location" reverse geocode (coords → zip) in `ZipCodeAutoComplete.razor` can be replaced with a local lookup against an embedded zip code table (bounding box or nearest-center distance), avoiding any external call.

---

## Decision

**Use Adressevælgeren only. No Datafordeleren calls needed.** Embed a static zip code dataset for the zip code autocomplete + reverse geocode, or use Adressevælgeren if it supports zip code search.

This means:
- One new client: `IAdressevælgerClient`
- One optional local data source: embedded Danish zip code list (for zip autocomplete + "use my location")
- Delete everything else

---

## Files to Change

### Delete
- `src/shared/Jordnaer.Shared/UserSearch/IDataForsyningenPingClient.cs`
- `src/web/Jordnaer/Features/Search/ZipCodeService.cs` (dead code)
- `src/web/Jordnaer/Features/UserSearch/IZipCodeService.cs` (if exists — it's defined inside ZipCodeService.cs)
- `src/web/Jordnaer/Features/UserSearch/DataForsyningenHealthCheck.cs` (health check for service being removed)

### Replace (new file, old deleted)
- `IDataForsyningenClient.cs` → `IAdressevælgerClient.cs` (address autocomplete only)
- `DataForsyningenResponses.cs` → `AdressevælgerResponses.cs`
- `DataForsyningenOptions.cs` → `AdressevælgerOptions.cs` (add auth fields once known)

### Modify
| File | Change |
|---|---|
| `src/shared/Jordnaer.Shared/Extensions/ServiceCollectionExtensions.cs` | Register `IAdressevælgerClient`, remove zip/ping clients |
| `src/web/Jordnaer/appsettings.json` | Replace `DataForsyningen` section with `Adressevælger` |
| `src/web/Jordnaer/Features/UserSearch/ServiceCollectionExtensions.cs` | Wire new options, remove health check registration |
| `src/web/Jordnaer/Features/Profile/LocationService.cs` | `GetLocationFromAddressAsync` → new client; `GetLocationFromZipCodeAsync` → local zip table or new client |
| `src/web/Jordnaer/Features/HjemGroups/HjemGroupAdminService.cs` | `GeocodeAsync` → use Adressevælgeren address search instead of zip search |
| `src/web/Jordnaer/Features/Profile/AddressAutoComplete.razor` | Use `IAdressevælgerClient` |
| `src/web/Jordnaer/Features/Search/ZipCodeAutoComplete.razor` | Use local zip table for autocomplete + reverse geocode |
| `src/web/Jordnaer/Features/Map/MapSearchFilter.razor` | Use `IAdressevælgerClient` |
| `tests/web/Jordnaer.Tests/UserSearch/DataForsyningenClientTests.cs` | Update to test new client |
| `docs/dawa-migration.md` | Update deadline, record final decision |

---

## Phase 0 — Research (BLOCKING — do this first)

When docs come back up, answer these before writing any code:

1. **Adressevælgeren base URL** — what is it? (`api.datafordeler.dk`? something else?)
2. **Auth method** — API key header? OAuth token? Username/password query param?
3. **Address autocomplete endpoint** — path + params + response JSON shape
4. **Zip code support** — does it have a zip/postal code search endpoint?
   - If yes: use it, no need for embedded zip data
   - If no: fetch Danish zip code list from DAWA *before Oct 1* and embed as `postnumre.json` resource
5. **Reverse geocode** — does it have coords → zip endpoint?
   - If no: implement local nearest-center lookup against embedded zip table

Doc URL: `https://confluence.sdfi.dk/pages/viewpage.action?pageId=234782998`

---

## Implementation Order (after Phase 0)

**Phase 1 — New client**
1. Update `docs/dawa-migration.md`
2. New `AdressevælgerOptions` + `IAdressevælgerClient` + response models
3. DI registration in `ServiceCollectionExtensions.cs` (shared)
4. Optionally: embed `postnumre.json` + `DanishZipCodeService` if needed

**Phase 2 — Replace callers**
5. `LocationService.GetLocationFromAddressAsync` → new client
6. `HjemGroupAdminService.GeocodeAsync` → new client (address search, not zip search)
7. `AddressAutoComplete.razor`, `MapSearchFilter.razor` → new client
8. `ZipCodeAutoComplete.razor` + `LocationService.GetLocationFromZipCodeAsync` → local zip table or new client

**Phase 3 — Cleanup**
9. Delete `IDataForsyningenClient`, `IDataForsyningenPingClient`, `DataForsyningenOptions`, `ZipCodeService`, `DataForsyningenHealthCheck`
10. Remove health check registration from `ServiceCollectionExtensions.cs`
11. Update integration tests

---

## Verification

```bash
dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi
```

Manual smoke test:
- Address autocomplete on profile page → results appear, location saved with coordinates
- Zip code input on user search → suggestion list works, "use my location" resolves zip
- Map search filter → address lookup centers map
- User/group radius search → results appear within chosen distance

---
title: 'DAWA Replacement: Adressevælger + Embedded Zip Codes'
status: 'Done'
purpose: 'Reference for how Jordnaer looks up Danish addresses and zip codes after DAWA shut down.'
description: >-
  DAWA shut down on October 1, 2026. Address autocomplete now uses Klimadatastyrelsen's Adressevælger API;
  all zip code lookups (autocomplete, zip → coordinates, coordinates → zip, city geocoding) use an embedded
  snapshot of DAWA's zip code list, with no external calls.
---

# DAWA Replacement: Adressevælger + Embedded Zip Codes

## TL;DR

- DAWA (`api.dataforsyningen.dk`) returns `410 Gone` since **October 1, 2026**.
- Addresses → **Adressevælger** (`https://adressevaelger.dk`) via `IAdressevaelgerClient`.
- Zip codes → **`IZipCodeService`**, backed by the embedded `Features/Search/Data/postnumre.json` (1,089 zip codes).
- Adressevælger returns ETRS89/UTM32 (EPSG:25832) coordinates; `Utm32Converter` converts them to WGS84.

## Addresses: Adressevælger

Docs: <https://confluence.kds.dk/pages/viewpage.action?pageId=234782998>

| What we do | Endpoint |
|---|---|
| Autocomplete (`AddressAutoComplete.razor`, `MapSearchFilter.razor`) | `GET /adresser/soeg?tekst=...&maksimum=20` |
| Coordinates for a selected address (`LocationService.GetLocationFromAddressAsync`) | `GET /adresser/soeg?tekst=...&maksimum=1` → `GET /husnumre/{husnummerId}` |

- **Token**: every request needs `token=`. There is no user management yet (expected late 2026 / early 2027), so KDS recommends the shared token `adressevaelger123`. It is configured as `Adressevaelger:Token` and appended by `AdressevaelgerTokenHandler`. Replace it when KDS introduces user management; sign up for their notification service to get notified.
- **No coordinates in search results**: search hits only return a title and ids. We look up the house number to get the access point coordinates.
- **Result types**: `adresse`, `husnummer`, `navngivenvejpostnummer` (street within a zip code) or `vejnavn` (street name only). If the best match is only a street, `LocationService` falls back to the zip code center.
- **No rate limit**, according to KDS. **No reverse geocoding** and **no zip code search**, which is why zip codes are local.

Known issues (from KDS's "Kendte fejl" page):

- A comma without a following space breaks the search (`"Vestergade 12,8000"`). The titles we pass back always contain `", "`.
- `o`/`oe` do not match `ø`, and `a`/`ae` do not match `æ`.
- Occasional `504 Upstream request timeout`; the standard resilience handler retries.

## Zip codes: embedded dataset

`ZipCodeService` (singleton) loads `src/web/Jordnaer/Features/Search/Data/postnumre.json`, an embedded resource with number, name, visual center and bounding box per zip code.

| Use | Method |
|---|---|
| Zip code autocomplete (`ZipCodeAutoComplete.razor`) | `Search(query)` |
| Zip code → coordinates for user/group/post search, profiles, groups (`LocationService.GetLocationFromZipCodeAsync`) | `Find(text)` |
| "Use my location" (`ZipCodeAutoComplete.razor`) | `FindNearest(lat, lon)`: nearest center among bounding boxes containing the point; `null` if outside Denmark |
| HJEM group city geocoding (`HjemGroupAdminService.Geocode`) | `Find(text)` |

The data is a snapshot of DAWA's `/postnumre` endpoint from September 16, 2026, recovered from the Internet Archive:
`https://web.archive.org/web/20260916062034id_/https://api.dataforsyningen.dk/postnumre`.

Danish zip codes rarely change. If new ones appear, regenerate the file from a current source (e.g. DAGI's postnummerinddeling on Datafordeleren) with the same shape: `number`, `name`, `latitude`, `longitude`, `boundingBox` (`[minLon, minLat, maxLon, maxLat]`).

## What was removed

- `IDataForsyningenClient`, `IDataForsyningenPingClient`, `DataForsyningenOptions`, response models
- `DataForsyningenHealthCheck` (no health check for Adressevælger; failures only degrade address autocomplete)
- The old `ZipCodeService` and `Circle`, which were dead code (radius search happens in SQL Server with `IsWithinDistance()`)

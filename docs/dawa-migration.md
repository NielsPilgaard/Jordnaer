# DAWA API Migration Guide

## Overview

DAWA (Danmarks Adressers Web API) is being shut down on **July 1, 2026**. This document outlines the impact on Jordnaer and the migration path to the replacement service.

## Timeline

| Date | Event |
|------|-------|
| December 16, 2023 | BBR data stopped updating in DAWA |
| April 1, 2024 | BBR data fully removed from DAWA |
| September 2024 | Zone data removed from DAWA |
| June 30, 2026 | Parallel operation ends |
| **July 1, 2026** | **DAWA shuts down** |

## Why is DAWA Closing?

- No funding secured for ongoing maintenance
- Multiple hard-to-fix bugs in recent years
- Lack of governance for data quality
- Modernization of Datafordeleren provides opportunity to consolidate

## Current Usage in Jordnaer

Jordnaer uses DAWA through the `IDataForsyningenClient` interface for the following functionality:

### 1. Address Autocomplete
- **Endpoint**: `GET /adresser/autocomplete?q={query}`
- **Used in**:
  - `Features/Profile/AddressAutoComplete.razor` - User profile address input
  - `Features/Map/MapSearchFilter.razor` - Map search functionality
  - `Features/Profile/LocationService.cs` - Location resolution

### 2. Zip Code Autocomplete
- **Endpoint**: `GET /postnumre/autocomplete?q={query}`
- **Used in**:
  - `Features/Search/ZipCodeAutoComplete.razor` - Search filter zip code input
  - `Features/Search/ZipCodeService.cs` - Zip code search service

### 3. Reverse Geocoding (Coordinates to Zip Code)
- **Endpoint**: `GET /postnumre/reverse?x={longitude}&y={latitude}`
- **Used in**:
  - `Features/Search/ZipCodeAutoComplete.razor` - Get zip code from user's location

### 4. Zip Codes Within Radius
- **Endpoint**: `GET /postnumre?cirkel={x,y,radius}`
- **Used in**:
  - `Features/Search/ZipCodeService.cs` - Find zip codes within search radius

## Affected Files

| File | Purpose |
|------|---------|
| `src/shared/Jordnaer.Shared/UserSearch/IDataForsyningenClient.cs` | Refit client interface |
| `src/shared/Jordnaer.Shared/UserSearch/IDataForsyningenPingClient.cs` | Health check client |
| `src/shared/Jordnaer.Shared/Extensions/ServiceCollectionExtensions.cs` | Client registration |
| `src/web/Jordnaer/Features/UserSearch/ServiceCollectionExtensions.cs` | Feature registration |
| `src/web/Jordnaer/Features/Profile/AddressAutoComplete.razor` | Address input component |
| `src/web/Jordnaer/Features/Profile/LocationService.cs` | Location service |
| `src/web/Jordnaer/Features/Map/MapSearchFilter.razor` | Map search filter |
| `src/web/Jordnaer/Features/Search/ZipCodeAutoComplete.razor` | Zip code input component |
| `src/web/Jordnaer/Features/Search/ZipCodeService.cs` | Zip code service |
| `src/web/Jordnaer/appsettings.json` | Configuration (BaseUrl) |
| `tests/web/Jordnaer.Tests/UserSearch/DataForsyningenClientTests.cs` | Integration tests |

## Migration Path: Datafordeleren

The replacement service is **Datafordeleren** with **DAR** (Danmarks Adresseregister).

### New API Details

- **Portal**: https://datafordeler.dk
- **DAR Documentation**: https://datafordeler.dk/dataoversigt/danmarks-adresseregister-dar/
- **Base URL**: `https://services.datafordeler.dk/DAR/DAR/3.0.0/rest/`

### Key Differences

| Aspect | DAWA | Datafordeleren |
|--------|------|----------------|
| Authentication | None (open) | May require username/password or certificate |
| Autocomplete | Built-in endpoints | Status uncertain - may need custom implementation |
| Response format | JSON | JSON/XML |
| Rate limiting | Unknown | Unknown |

### Autocomplete Uncertainty

From the official announcement:
> "It can be said with certainty that the autocomplete functionality will continue after DAWA closes, but it remains unclear exactly what it will look like in the future."

This means we should monitor announcements closely and be prepared for potential changes to autocomplete implementation.

## Migration Options

### Option 1: Wait for Datafordeleren Autocomplete (Recommended)
- Monitor official announcements for autocomplete replacement
- Migrate when new endpoints are available
- Lowest effort if functionality is preserved

### Option 2: Build Custom Autocomplete
- Use Datafordeleren raw data APIs
- Implement server-side autocomplete logic
- Cache address data for performance
- Higher effort but more control

### Option 3: Third-Party Service
- Evaluate commercial address validation services
- Consider services like Google Places, HERE, or local alternatives
- May have cost implications

## Action Items

- [ ] Register at https://datafordeler.dk to test DAR API
- [ ] Subscribe to Klimadatastyrelsens newsletter for updates
- [ ] Monitor https://dataforsyningen.dk for migration announcements
- [ ] Test Datafordeleren API endpoints in development
- [ ] Plan migration timeline (before June 2026)
- [ ] Update integration tests for new API
- [ ] Update health checks for new service

## Resources

- [DAWA Documentation](https://dawadocs.dataforsyningen.dk/)
- [Datafordeler Portal](https://datafordeler.dk/)
- [DAR Address API](https://datafordeler.dk/dataoversigt/danmarks-adresseregister-dar/dar-adresse/)
- [DAWA Autocomplete Demo](https://autocomplete.aws.dk/)
- [Dataforsyningen](https://dataforsyningen.dk/)

## Contact

For questions about the migration, contact Klimadatastyrelsen through their official channels.

---

*Document created: December 2024*
*Last updated: December 2024*

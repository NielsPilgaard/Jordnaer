# Task 02: Migrate Hardcoded Ads to Partner System

## Status

- **State**: Partially Complete
- **Priority**: Medium
- **Prerequisites**: Complete Task 01 (Partner Dashboard) ✅

## What's Done ✅

The unified ad system is now in place:

- **AdProvider service** (`src/web/Jordnaer/Features/Ad/AdProvider.cs`) - Combines hardcoded and partner ads
- **AdData updated** - Now includes `PartnerId` for analytics tracking
- **All ad display components updated** to use `IAdProvider` and pass `PartnerId` to `AdCard`
- **Analytics tracking works** - Partner ads get impressions/clicks tracked automatically

## How It Works Now

1. `AdProvider.GetAdsAsync()` fetches partner ads from database + hardcoded ads
2. Ads are shuffled and served together
3. Partner ads include their `PartnerId`, hardcoded ads have `null`
4. `AdCard` tracks impressions/clicks for partner ads via `PartnerService`

## What Remains

### 1. Contact Moon Creative (when ready)
- [ ] Reach out to Moon Creative
- [ ] Get their email for account creation
- [ ] Confirm they want to continue as a partner

### 2. Create Partner Account
- [ ] Create account via `/backoffice/partners/create`
- [ ] Have them upload their ad image via `/partner/dashboard`
- [ ] Approve the image

### 3. Remove Hardcoded Ads
Once Moon Creative is onboarded and verified:
- [ ] Remove Moon Creative entry from `HardcodedAds._ads`
- [ ] Delete `src/web/Jordnaer/wwwroot/images/ads/mooncreative_mobile.png`
- [ ] Once no hardcoded ads remain, delete `HardcodedAds.cs`

### 4. Verify
- [ ] Moon Creative can log in and view analytics
- [ ] Their ad displays correctly
- [ ] Impressions and clicks are tracked

## Files Changed

| File | Change |
|------|--------|
| `Features/Ad/AdProvider.cs` | **New** - Unified ad provider |
| `Features/Ad/WebApplicationBuilderExtensions.cs` | **New** - DI registration |
| `Features/Ad/HardcodedAds.cs` | Added `PartnerId` to `AdData` |
| `Program.cs` | Added `builder.AddAdServices()` |
| `Features/UserSearch/UserSearchResultComponent.razor` | Uses `IAdProvider` |
| `Features/Posts/PostSearchResultComponent.razor` | Uses `IAdProvider` |
| `Features/GroupSearch/GroupSearchResultComponent.razor` | Uses `IAdProvider` |
| `Pages/Posts/PostPage.razor` | Uses `IAdProvider` |

## Trigger

When active user count reaches 100+ and we're ready to onboard partners.

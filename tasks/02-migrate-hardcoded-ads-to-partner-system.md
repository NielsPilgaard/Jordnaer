# Task 02: Migrate Hardcoded Ads to Partner System

## Status
- **State**: Ready (waiting for 100+ active users)
- **Priority**: Medium
- **Estimated Effort**: 1-2 hours
- **Prerequisites**: Complete Task 01 (Sponsor Dashboard) ✅

## Context

Currently, sponsor advertisements are hardcoded in `src/web/Jordnaer/Features/Ad/HardcodedAds.cs`. With the new sponsor/partner system (Task 01) now complete, we can migrate these hardcoded ads to the database-backed partner system once we have meaningful traffic to offer sponsors.

**Current Hardcoded Ad:**
- **Partner**: Moon Creative
- **Description**: Professionel webudvikling og design
- **Image**: `images/ads/mooncreative_mobile.png`
- **Link**: https://www.mooncreative.dk/

## Why Wait?

We're intentionally delaying this migration until we have **100+ active users** because:
- Offering sponsor placement with low traffic feels like a false promise
- It creates work for the partner without meaningful value
- Better to wait until we can deliver actual ROI

## Migration Steps

When ready to migrate (>100 users), follow these steps:

### 1. Contact Moon Creative
- [ ] Reach out to Moon Creative contact
- [ ] Explain the new partner dashboard system
- [ ] Get their real email address for account creation
- [ ] Confirm they want to continue as a sponsor

### 2. Create Partner Account
- [ ] Navigate to `/backoffice/partners/create`
- [ ] Enter Moon Creative details:
  - **Name**: Moon Creative
  - **Email**: [their real email]
  - **Description**: Professionel webudvikling og design
  - **Link**: https://www.mooncreative.dk/
  - **Logo URL**: [get their logo URL if available]
- [ ] Save the temporary password securely
- [ ] Send them the welcome email

### 3. Upload Their Current Ad Image
- [ ] Send Moon Creative their login credentials
- [ ] Guide them to upload their current ad (`mooncreative_mobile.png`)
  - They can upload via `/sponsor/dashboard`
  - Or you can manually upload and approve as admin
- [ ] Approve the image via `/backoffice/sponsors/{id}`

### 4. Update Ad Display Logic

Currently, ads are served from `HardcodedAds.cs`:

```csharp
// src/web/Jordnaer/Features/Ad/HardcodedAds.cs
public static List<AdData> GetAdsForSearch(int count)
{
    // Returns hardcoded Moon Creative ad
}
```

**Migration Changes:**

Replace hardcoded logic with database queries:

**Option A: Simple Replacement**
```csharp
// In the component that uses GetAdsForSearch
@inject ISponsorService SponsorService

// Replace:
var ads = HardcodedAds.GetAdsForSearch(count);

// With:
var sponsors = await SponsorService.GetAllSponsorsAsync();
var activeSponsors = sponsors
    .Where(s => !string.IsNullOrEmpty(s.MobileImageUrl) ||
                !string.IsNullOrEmpty(s.DesktopImageUrl))
    .ToList();
```

**Option B: Create AdService**
```csharp
// src/web/Jordnaer/Features/Ad/AdService.cs
public interface IAdService
{
    Task<List<SponsorAd>> GetAdsForSearchAsync(int count, CancellationToken cancellationToken = default);
}

public record SponsorAd
{
    public Guid SponsorId { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public string? MobileImageUrl { get; init; }
    public string? DesktopImageUrl { get; init; }
    public string Link { get; init; }
}

public class AdService : IAdService
{
    private readonly ISponsorService _sponsorService;

    public async Task<List<SponsorAd>> GetAdsForSearchAsync(int count, CancellationToken cancellationToken = default)
    {
        var sponsors = await _sponsorService.GetAllSponsorsAsync(cancellationToken);

        var activeSponsors = sponsors
            .Where(s => (!string.IsNullOrEmpty(s.MobileImageUrl) || !string.IsNullOrEmpty(s.DesktopImageUrl))
                        && !s.HasPendingImageApproval) // Only show approved ads
            .ToList();

        // Implement rotation/selection logic here
        // For now, return all active sponsors up to count
        return activeSponsors
            .Take(count)
            .Select(s => new SponsorAd
            {
                SponsorId = s.Id,
                Name = s.Name,
                Description = s.Description,
                MobileImageUrl = s.MobileImageUrl,
                DesktopImageUrl = s.DesktopImageUrl,
                Link = s.Link
            })
            .ToList();
    }
}
```

### 5. Update Components Using Ads

Find all usages of `HardcodedAds.GetAdsForSearch()`:

```bash
# Search for usages
grep -r "GetAdsForSearch" --include="*.razor" --include="*.cs"
```

Update each component to use the new service or direct sponsor queries.

### 6. Record Analytics

Update ad display components to record impressions and clicks:

```csharp
// When displaying ad
await SponsorService.RecordImpressionAsync(sponsorId);

// When user clicks ad
await SponsorService.RecordClickAsync(sponsorId);
```

### 7. Remove Hardcoded Files

Once migration is complete and verified:
- [ ] Delete `src/web/Jordnaer/Features/Ad/HardcodedAds.cs`
- [ ] Delete `src/web/Jordnaer/wwwroot/images/ads/mooncreative_mobile.png`
- [ ] Remove any related hardcoded ad infrastructure
- [ ] Update documentation

### 8. Verify Migration

- [ ] Verify Moon Creative can log in
- [ ] Verify their ad displays correctly
- [ ] Verify impressions are being tracked
- [ ] Verify clicks are being tracked
- [ ] Verify they can see analytics on their dashboard
- [ ] Verify admin can see their data in `/backoffice/sponsors/{id}`

## Future Enhancements (Post-Migration)

Once the migration is complete, consider these improvements:

### Ad Rotation Logic
- Fair rotation between multiple sponsors
- Weighted rotation based on sponsorship tier (future feature)
- A/B testing support

### Ad Placement Optimization
- Multiple ad sizes (mobile, desktop, banner, sidebar)
- Contextual ad placement
- Performance-based optimization

### Sponsor Tiers (Task 04)
- Basic tier: Limited impressions
- Premium tier: Unlimited impressions
- Featured tier: Priority placement

## Technical Notes

### Backward Compatibility
Keep `HardcodedAds.cs` as a fallback during migration:

```csharp
public async Task<List<SponsorAd>> GetAdsForSearchAsync(int count)
{
    var sponsors = await GetActiveSponsors();

    // Fallback to hardcoded ads if no sponsors available
    if (!sponsors.Any())
    {
        return HardcodedAds.GetAdsForSearch(count)
            .Select(ConvertToSponsorAd)
            .ToList();
    }

    return sponsors;
}
```

### Migration Checklist Files to Update

- [ ] Find all `HardcodedAds.GetAdsForSearch()` usages
- [ ] Update to use `ISponsorService` or new `IAdService`
- [ ] Add impression tracking
- [ ] Add click tracking
- [ ] Test on staging environment first
- [ ] Deploy to production
- [ ] Monitor analytics for 1 week
- [ ] Remove hardcoded files

## Success Criteria

- [ ] Moon Creative partner account created
- [ ] Their ad image uploaded and approved
- [ ] Ad displays correctly in all locations (mobile + desktop)
- [ ] Impressions are tracked accurately
- [ ] Clicks are tracked accurately
- [ ] Moon Creative can view their analytics
- [ ] No hardcoded ads remain in codebase
- [ ] System supports multiple sponsors (future-proof)

## Timeline

**Trigger**: When active user count reaches 100+

**Execution**:
1. Day 1: Contact Moon Creative, create account
2. Day 2-3: Help them upload images, verify system
3. Day 4: Deploy migration changes
4. Day 5-11: Monitor for 1 week
5. Day 12: Remove hardcoded files if all good

Total: ~2 weeks from start to complete cleanup

## Related Tasks

- ✅ **Task 01**: Sponsor Dashboard (Complete)
- **Task 03**: Advertisement Management System (Future)
- **Task 04**: User Subscriptions (Future)

## Notes

- Keep Moon Creative informed throughout the process
- Offer support during their onboarding
- Consider giving them early access as a thank you for being our first sponsor
- Use their feedback to improve the partner experience before onboarding more sponsors

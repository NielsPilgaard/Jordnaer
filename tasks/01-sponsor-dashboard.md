# Task 01: Sponsor Dashboard

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Sponsor Management
**Priority:** Medium
**Related:** Task 02 (Backoffice Claims Management) - requires claims/permissions for sponsor access

## Objective

Create a dashboard where sponsors can view analytics about their ad performance and manage their ad images for both mobile and desktop displays. Any image changes must be reviewed by admins before going live.

## Current State

- Sponsors are displayed on public `/sponsors` page via [SponsorCard.razor](src/web/Jordnaer/Features/Sponsors/SponsorCard.razor)
- Sponsor model exists at [Sponsor.cs](src/shared/Jordnaer.Shared/Sponsors/Sponsor.cs) with basic properties (Name, Description, LogoUrl, Link)
- No sponsor-specific dashboard or analytics exist
- No admin approval workflow for sponsor content changes
- No image management system for sponsors

## Requirements

### 1. Sponsor Dashboard Page

- Create new page at `/sponsor/dashboard` route
- Require `[Authorize]` attribute with sponsor claim check (from Task 02)
- Follow existing [UserDashboard.razor](src/web/Jordnaer/Features/Dashboard/UserDashboard.razor) pattern
- Use [DashboardCard.razor](src/web/Jordnaer/Features/Dashboard/DashboardCard.razor) component for sections
- Responsive grid layout (MudGrid with xs, sm, md breakpoints)

### 2. Analytics Display

Display the following metrics:
- **Impressions:** Total number of times the ad was displayed
- **Clicks:** Total number of clicks on the ad
- **Click-through Rate (CTR):** Clicks / Impressions percentage
- **Time Period Selector:** Last 7 days, 30 days, 90 days, all time

### 3. Image Management

- **Separate images for mobile and desktop:**
  - Mobile image: Displayed on screens < 768px
  - Desktop image: Displayed on screens >= 768px
- **Upload interface:**
  - Use existing [ImageService.cs](src/web/Jordnaer/Features/Images/ImageService.cs) for Azure Blob Storage uploads
  - New container: `sponsor-ads` (auto-created by ImageService)
  - File size validation: Max 5MB per image
  - Format validation: PNG, JPG, WEBP only
  - Preview uploaded images before submission
- **Approval workflow:**
  - When sponsor uploads new image(s), mark as "pending approval"
  - Send email to `kontakt@mini-moeder.dk` using existing [EmailService.cs](src/web/Jordnaer/Features/Email/EmailService.cs)
  - Email should include:
    - Sponsor name
    - Link to backoffice approval page
    - Preview/link to the new images
  - Use MassTransit pattern via [SendEmailConsumer.cs](src/web/Jordnaer/Consumers/SendEmailConsumer.cs)
  - Display "pending approval" badge on dashboard until approved

### 4. Data Model Extensions

Extend [Sponsor.cs](src/shared/Jordnaer.Shared/Sponsors/Sponsor.cs) with:
```csharp
public string? MobileImageUrl { get; set; }
public string? DesktopImageUrl { get; set; }
public string? PendingMobileImageUrl { get; set; }
public string? PendingDesktopImageUrl { get; set; }
public DateTime? LastImageUpdateUtc { get; set; }
public bool HasPendingImageApproval { get; set; }
```

Create new analytics model:
```csharp
public class SponsorAnalytics
{
    public Guid SponsorId { get; set; }
    public DateTime Date { get; set; }
    public int Impressions { get; set; }
    public int Clicks { get; set; }
}
```

### 5. Analytics Tracking

- Track impressions when ad is displayed (client-side or server-side)
- Track clicks when sponsor link is clicked
- Store daily aggregated data in `SponsorAnalytics` table
- Implement service methods:
  - `RecordImpressionAsync(Guid sponsorId)`
  - `RecordClickAsync(Guid sponsorId)`
  - `GetAnalyticsAsync(Guid sponsorId, DateTime from, DateTime to)`

### 6. Integration Points

- Update [SponsorCard.razor](src/web/Jordnaer/Features/Sponsors/SponsorCard.razor) to:
  - Use responsive image display (mobile vs desktop)
  - Track impressions on render
  - Track clicks on link interaction
- Create navigation link from sponsor dashboard to public sponsor page

## Acceptance Criteria

### Dashboard Page
- [ ] New page at `/sponsor/dashboard` with `[Authorize]` attribute
- [ ] Only accessible to users with sponsor claims (see Task 02)
- [ ] Follows existing dashboard design patterns
- [ ] Fully responsive layout

### Analytics Display
- [ ] Shows impressions, clicks, and CTR
- [ ] Time period selector with 4 options (7/30/90 days, all time)
- [ ] Charts/visualizations for analytics data (use MudBlazor charts)
- [ ] Handles zero data gracefully (e.g., "No data for this period")

### Image Management
- [ ] Upload interface for mobile and desktop images
- [ ] File validation (size, format)
- [ ] Image preview before submission
- [ ] Pending approval state displayed to sponsor
- [ ] Email sent to admin on new image upload
- [ ] Uses ImageService with `sponsor-ads` container

### Data & Tracking
- [ ] Database migration adds new fields to Sponsor table
- [ ] SponsorAnalytics table created
- [ ] Impression tracking implemented
- [ ] Click tracking implemented
- [ ] Analytics aggregation service implemented

### Email Workflow
- [ ] Admin notification email on image upload
- [ ] Email includes sponsor name and image links
- [ ] Uses existing SendEmail/MassTransit infrastructure
- [ ] Email sent to `kontakt@mini-moeder.dk`

## Files to Create/Modify

**New Files:**
- `src/web/Jordnaer/Pages/Sponsor/Dashboard.razor` - Sponsor dashboard page
- `src/web/Jordnaer/Features/Sponsors/SponsorService.cs` - Service for analytics and image management
- `src/web/Jordnaer/Features/Sponsors/SponsorAnalytics.cs` - Analytics model
- `src/web/Jordnaer/Database/Migrations/AddSponsorAnalytics.cs` - EF migration

**Modify:**
- [Sponsor.cs](src/shared/Jordnaer.Shared/Sponsors/Sponsor.cs) - Add image and approval fields
- [SponsorCard.razor](src/web/Jordnaer/Features/Sponsors/SponsorCard.razor) - Responsive images + tracking
- [JordnaerDbContext.cs](src/web/Jordnaer/Database/JordnaerDbContext.cs) - Add SponsorAnalytics DbSet
- [EmailService.cs](src/web/Jordnaer/Features/Email/EmailService.cs) - Add method for sponsor image approval emails

## Technical Notes

- Use existing [ImageService.cs](src/web/Jordnaer/Features/Images/ImageService.cs) for blob uploads
- Follow email pattern from [SendEmailConsumer.cs](src/web/Jordnaer/Consumers/SendEmailConsumer.cs)
- Analytics should aggregate daily to avoid large datasets
- Consider lazy loading for analytics charts
- Implement proper error handling for failed uploads
- GDPR: Don't track individual user interactions, only aggregate counts

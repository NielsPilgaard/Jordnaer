# Partner Ad Improvements

## Task 1: Rename Link to PartnerPageLink and Add Separate AdLink Field

**Problem:** Partners currently have a single `Link` field that's ambiguously named. We need clear separation between:
- `PartnerPageLink` - Link shown on the partner card at `/partners`
- `AdLink` - Link used when clicking ads in search results

**Changes Required:**

1. **Database Model** ([Partner.cs](src/shared/Jordnaer.Shared/Database/Partner.cs)):
   - Rename `Link` → `PartnerPageLink`
   - Rename `PendingLink` → `PendingPartnerPageLink`
   - Add `AdLink` property (nullable string, URL validated)
   - Add `PendingAdLink` property for approval workflow

2. **Partner Dashboard** ([PartnerDashboard.razor](src/web/Jordnaer/Pages/Partner/PartnerDashboard.razor)):
   - Rename "Website Link" field to "Partnerside Link" (for partner card)
   - Add "Annonce Link" text field in the Ad Image section (around line 140-165)
   - Update `HasChanges()` to include `_pendingAdLink`
   - Update `SubmitChangesAsync()` to pass the ad link
   - Update all references from `Link`/`PendingLink` to `PartnerPageLink`/`PendingPartnerPageLink`

3. **Partner Service** ([PartnerService.cs](src/web/Jordnaer/Features/Partners/PartnerService.cs)):
   - Update all references from `Link`/`PendingLink` to `PartnerPageLink`/`PendingPartnerPageLink`
   - Update `UploadPendingChangesAsync` to accept and save `pendingAdLink`
   - Update `ApproveChangesAsync` to copy `PendingAdLink` to `AdLink`

4. **PartnerCard Component** ([PartnerCard.razor](src/web/Jordnaer/Features/Partners/PartnerCard.razor)):
   - Update to use `Partner.PartnerPageLink` instead of `Partner.Link`

5. **AdCard / Ad Display Logic**:
   - Update places that render `AdCard` to pass `Partner.AdLink ?? Partner.PartnerPageLink`

6. **HasPartnerCard Logic** ([Partner.cs](src/shared/Jordnaer.Shared/Database/Partner.cs)):
   - Update `HasPartnerCard` property to check `PartnerPageLink` instead of `Link`

7. **Admin/Backoffice Pages**: Update any references to the old field names

8. **Database Migration**:
   - Rename `Link` column to `PartnerPageLink`
   - Rename `PendingLink` column to `PendingPartnerPageLink`
   - Add `AdLink` and `PendingAdLink` columns

---

## Task 2: Configurable "Annonce" Label Background Color

**Problem:** The "Annonce" label has a fixed semi-transparent dark background. When ads have similar dark colors, the label becomes hard to read.

**Changes Required:**

1. **Database Model** ([Partner.cs](src/shared/Jordnaer.Shared/Database/Partner.cs)):
   - Add `AdLabelColor` property (nullable string, stores hex color like `#FFFFFF`)
   - Add `PendingAdLabelColor` for approval workflow

2. **Partner Dashboard** ([PartnerDashboard.razor](src/web/Jordnaer/Pages/Partner/PartnerDashboard.razor)):
   - Add color picker in Ad Image section for "Annonce label farve"
   - Options: Dark (default), Light, or custom hex color

3. **AdCard Component** ([AdCard.razor](src/web/Jordnaer/Features/Ad/AdCard.razor)):
   - Add `LabelColor` parameter (optional)
   - Apply dynamic inline style to `.image-ad-label` div based on color

4. **Partner Service**: Update approval workflow for the new field

5. **Database Migration**: Add migration for the new column

---

## Task 3: Remove Description from Partner Details (if unused)

**Analysis:** The `Description` field IS used - it displays on the PartnerCard component ([PartnerCard.razor:14-17](src/web/Jordnaer/Features/Partners/PartnerCard.razor#L14-L17)) on the /partners page.

**No changes needed** - the description field serves a purpose.

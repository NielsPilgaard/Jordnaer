# Task: Show UN Sustainable Development Goals (Verdensmål) in Footer

## Context

Mini Møder helps parents find playgroups and parenting communities. The relevant UN SDGs are:

- **Goal 3 – Good Health and Well-Being** (God sundhed og trivsel): reducing isolation and supporting parental mental health through community
- **Goal 10 – Reduced Inequalities** (Færre uligheder): making parenting communities accessible regardless of social background
- **Goal 11 – Sustainable Cities and Communities** (Bæredygtige byer og lokalsamfund): building local community ties between families

## UN SDG Icon Usage Guidelines (Must Comply)

The official guidelines ([UN SDG Guidelines Sep 2023](https://www.un.org/sustainabledevelopment/wp-content/uploads/2023/09/E_SDG_Guidelines_Sep20238.pdf)) impose the following rules. All points below are binding.

### Permitted
- Using the 17 individual goal icons for illustrative, non-commercial purposes (e.g. on a website footer to communicate SDG alignment) — **no prior written permission required**.
- Using the icons in their original colours and proportions.
- Linking the icons to the official SDG website.

### Prohibited
- **Do not use the SDG logo that includes the UN emblem** — that version is reserved for UN System entities only. Use the icon set without the UN circular emblem.
- **Do not modify the colours** of the icons.
- **Do not distort or stretch** the icons.
- **Do not imply UN endorsement** — the icons must not suggest that Mini Møder is approved, sponsored, or endorsed by the United Nations.
- **Do not use for fundraising or commercial transactions** without prior written permission from the UN (contact: SDGpermissions@un.org).

### Required
1. **Disclaimer text** — include the following verbatim on any page or document that uses the icons (can be small print in the footer itself):

   > "The content of this publication has not been approved by the United Nations and does not reflect the views of the United Nations or its officials or Member States."

2. **Link to the UN SDG website** — the icons or the disclaimer must link to https://www.un.org/sustainabledevelopment or https://www.verdensmaal.org.

3. **These guidelines must be uploaded/accessible** on the same page as the icons when published online. Linking to the official guidelines PDF from the footer or an About page satisfies this.

### Source for Icons
Download the **Danish-language** individual goal icons (without UN emblem) from:
- https://www.verdensmaal.org/materialer

Choose the PNG versions for Goal 3, 10, and 11. Do **not** use a version that shows the UN circular logo/emblem.

---

## What to Build

Add a "Verdensmål" section to the footer in [src/web/Jordnaer/Components/Footer.razor](src/web/Jordnaer/Components/Footer.razor), and add the required disclaimer text below the existing footer content.

### 1. Download and Save Icons

Download the three Danish SDG icons from https://www.verdensmaal.org/materialer and save to:

- `src/web/Jordnaer/wwwroot/images/sdg/sdg-3.png`
- `src/web/Jordnaer/wwwroot/images/sdg/sdg-10.png`
- `src/web/Jordnaer/wwwroot/images/sdg/sdg-11.png`

Verify: the downloaded icons do **not** contain the UN circular emblem.

### 2. Footer Column — SDG Icons

Add a new column inside the existing `div.d-flex.flex-wrap.justify-center`:

```razor
<div class="d-flex flex-column justify-center align-center px-4 px-sm-6 px-md-8">
    <MudText Typo="Typo.body2" Class="mb-1">Verdensmål</MudText>
    <div class="d-flex gap-2 align-center">
        <a href="https://www.verdensmaal.org/maal/3" target="_blank" rel="noopener noreferrer"
           aria-label="Verdensmål 3: God sundhed og trivsel">
            <img src="/images/sdg/sdg-3.png" alt="Verdensmål 3" class="sdg-icon" />
        </a>
        <a href="https://www.verdensmaal.org/maal/10" target="_blank" rel="noopener noreferrer"
           aria-label="Verdensmål 10: Færre uligheder">
            <img src="/images/sdg/sdg-10.png" alt="Verdensmål 10" class="sdg-icon" />
        </a>
        <a href="https://www.verdensmaal.org/maal/11" target="_blank" rel="noopener noreferrer"
           aria-label="Verdensmål 11: Bæredygtige byer og lokalsamfund">
            <img src="/images/sdg/sdg-11.png" alt="Verdensmål 11" class="sdg-icon" />
        </a>
    </div>
</div>
```

### 3. Required Disclaimer Text

Add the following **below** the `div.d-flex.flex-wrap` icons row, still inside the `<footer>` element. This satisfies the UN's requirement that the disclaimer appear on the same page as the icons:

```razor
<div class="d-flex justify-center px-4 pb-2">
    <MudText Typo="Typo.caption" Align="Align.Center" Style="max-width: 600px; opacity: 0.7;">
        The content of this publication has not been approved by the United Nations and does not
        reflect the views of the United Nations or its officials or Member States.
        <MudLink Href="https://www.un.org/sustainabledevelopment" Target="_blank"
                 Rel="noopener noreferrer" Typo="Typo.caption">
            un.org/sustainabledevelopment
        </MudLink>
    </MudText>
</div>
```

### 4. CSS

Add to the existing `<style>` block in Footer.razor:

```css
.sdg-icon {
    height: 60px;
    width: auto;
    transition: transform 0.2s ease-in-out;
}

.sdg-icon:hover {
    transform: scale(1.1);
}
```

---

## Verification

1. Run the app (`dotnet run` from `src/web/Jordnaer/`)
2. Open any page and scroll to the footer
3. A "Verdensmål" column is visible with 3 SDG icons side by side
4. Each icon links to the correct `verdensmaal.org` goal page in a new tab
5. Icons scale up slightly on hover
6. No broken image placeholders — all 3 PNGs load correctly
7. **The icons do NOT show the UN circular emblem** — if they do, re-download the correct variant
8. The UN disclaimer text is visible in small print below the footer links
9. The disclaimer includes a working link to `un.org/sustainabledevelopment`

## Files to Change

- [src/web/Jordnaer/Components/Footer.razor](src/web/Jordnaer/Components/Footer.razor) — add the SDG column, disclaimer, and CSS
- `src/web/Jordnaer/wwwroot/images/sdg/sdg-3.png` — downloaded icon (no UN emblem)
- `src/web/Jordnaer/wwwroot/images/sdg/sdg-10.png` — downloaded icon (no UN emblem)
- `src/web/Jordnaer/wwwroot/images/sdg/sdg-11.png` — downloaded icon (no UN emblem)

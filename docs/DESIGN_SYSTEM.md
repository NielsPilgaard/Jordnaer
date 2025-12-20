# Jordnaer Design System Implementation Guide

## Overview

This document explains how to use the Jordnaer (Mini Møder) design system implemented based on the official `Mini Design Guide.pdf`.

## Color Palette

### CSS Variables

All brand colors are available as CSS variables:

```css
--color-glaede: #dbab45;      /* Joy - primary yellow-orange */
--color-ro: #878e64;          /* Calm - secondary green */
--color-moede: #41556b;       /* Meeting - body text blue */
--color-moede-red: #673417;   /* Meeting - small text/quotes red-brown */
--color-omsorg: #cfc1a6;      /* Care - light beige background */
--color-leg: #a9c0cf;         /* Play - light blue background */
```

### C# Palette (JordnaerPalette.cs)

For Blazor components, use the `JordnaerPalette` class:

```csharp
using Jordnaer.Features.Theme;

// Background colors
JordnaerPalette.YellowBackground   // #dbab45 (GLÆDE)
JordnaerPalette.GreenBackground    // #878e64 (RO)
JordnaerPalette.BeigeBackground    // #cfc1a6 (OMSORG)
JordnaerPalette.PaleBlueBackground // #a9c0cf (LEG)

// Text colors
JordnaerPalette.BlueBody           // #41556b (MØDE - body text)
JordnaerPalette.RedHeader          // #673417 (MØDE - small text/quotes)

// Transparent variants
JordnaerPalette.BeigeBackground60  // 60% opacity
JordnaerPalette.PaleBlueBackground40 // 40% opacity
```

### Usage Rules

- **GLÆDE (Yellow)**: Primary color for heading backgrounds and key elements
- **RO (Green)**: Secondary color for heading backgrounds
- **MØDE (Blue)**: Body text color - use for all readable text
- **MØDE Red (Red-brown)**: Small text, payoffs, quotes
- **OMSORG (Beige)**: Background when yellow/green are too dark
- **LEG (Blue)**: Rarely used lighter background

⚠️ **Important**: Never use yellow or light colors for body text (readability issues)

## Typography

### Font Families

```css
--font-heading: 'Cherry Bomb One', cursive;
--font-body: 'Open Sans Light', sans-serif;
--font-body-bold: 'Open Sans Bold', sans-serif;
--font-body-medium: 'Open Sans Medium', sans-serif;
```

### Heading Styles

**Default Headings**
- All headings (h1-h6) use the standard font by default
- Cherry Bomb One is **opt-in** using specific CSS classes (see below)

**Cherry Bomb One Headings (Opt-in)**
- Use `.heading-yellow`, `.heading-green`, or `.heading-blue` classes
- Letter spacing: `0.11em` (110 tracking)
- Text transform: lowercase
- Use for short words and main headings

```html
<h1 class="heading-yellow">velkommen til jordnaer</h1>
<h2 class="heading-green">find nye venner</h2>
<h3 class="heading-blue">lokal fællesskab</h3>
```

**Subheadings**
- Use `.subheading` or `.subheading-red` classes
- Letter spacing: `0.41em` (410 tracking)
- Color: MØDE blue or MØDE red
- Use where Cherry Bomb becomes unreadable

### Body Text

All body text automatically uses **Open Sans Light** with MØDE blue color.

```html
<p>This is standard body text in Open Sans Light.</p>
<p class="small-text">Small text or quote in red-brown.</p>
```

### CSS Classes

```css
.font-cherry-bomb-one     /* Cherry Bomb One with correct spacing */
.font-open-sans-light     /* Open Sans Light */
.font-open-sans-bold      /* Open Sans Bold */
.font-open-sans-medium    /* Open Sans Medium */

.subheading               /* Subheading style (Open Sans + tracking) */
.subheading-red          /* Subheading in red-brown */
.small-text              /* Small text in red-brown */
```

## Heading Color Rotation

To create visual variety, rotate heading colors across sections using the opt-in Cherry Bomb classes:

### Cherry Bomb Heading Classes (Opt-in)

These classes apply Cherry Bomb One font, letter-spacing, lowercase transform, and color:

```html
<h1 class="heading-yellow">overskrift 1</h1>  <!-- Yellow Cherry Bomb heading -->
<h2 class="heading-green">overskrift 2</h2>   <!-- Green Cherry Bomb heading -->
<h3 class="heading-blue">overskrift 3</h3>    <!-- Blue Cherry Bomb heading -->
```

Each class includes:
- `font-family: 'Cherry Bomb One'`
- `letter-spacing: 0.11em`
- `text-transform: lowercase`
- Brand color (yellow, green, or blue)

### Background Color Utilities

For headings with colored backgrounds:

```html
<h2 class="bg-heading-yellow">featured heading</h2>
<h3 class="bg-heading-green">secondary heading</h3>
<h4 class="bg-heading-blue">tertiary heading</h4>
```

For custom backgrounds:

```html
<div class="bg-glaede">Yellow background</div>
<div class="bg-ro">Green background</div>
<div class="bg-moede">Blue background</div>
<div class="bg-omsorg">Beige background</div>
<div class="bg-leg">Light blue background</div>
```

### Design Principle: Color Variety

✅ **DO**: Vary colors across sections
```html
<section>
  <h2 class="heading-yellow">første sektion</h2>
  <p>Content...</p>
</section>
<section>
  <h2 class="heading-green">anden sektion</h2>
  <p>Content...</p>
</section>
<section>
  <h2 class="heading-blue">tredje sektion</h2>
  <p>Content...</p>
</section>
```

❌ **DON'T**: Use multiple colors in one heading
```html
<h1>
  <span class="heading-yellow">multi</span>
  <span class="heading-green">colored</span>
</h1>
<!-- Only the logo does this! -->
```

## Visual Elements

### Separators

**MiniDivider Component** (for major section breaks):

Use the `MiniDivider` component with image-based dotted lines (bee flight path):

```razor
<MiniDivider Color="MiniDividerColor.Yellow" Class="my-4" />
<MiniDivider Color="MiniDividerColor.Green" Class="my-4" />
<MiniDivider Color="MiniDividerColor.Blue" Class="my-4" />
```

**MiniDivider with Center property**:

Use the `Center` parameter to center the divider image:

```razor
<MiniDivider Color="MiniDividerColor.Yellow" Center Class="my-4" />
<MiniDivider Color="MiniDividerColor.Green" Center="true" Class="my-4" />
```

Parameters:
- `Color` (required): `MiniDividerColor.Yellow`, `MiniDividerColor.Green`, or `MiniDividerColor.Blue`
- `Center` (optional, default: false): Centers the divider image in a flex container
- `Class` (optional): Additional CSS classes
- `Style` (optional): Inline styles

**MudDivider** (for card/content separators):

Use standard `MudDivider` for clean separators inside cards:

```razor
<MudDivider Class="my-4" />
```

## Warmth & Interactions

### Button Transitions

All buttons automatically have smooth hover effects:
- Slight upward movement on hover
- Subtle shadow enhancement
- 200ms transition

### Card Hover Effects

Cards have built-in warm hover states:

```html
<div class="mud-card warm-shadow">
  <!-- Card content -->
</div>
```

### Link Transitions

Links automatically transition to GLÆDE yellow on hover.

### Input Focus States

Input fields show GLÆDE yellow outline when focused.

## Utility Classes

### Warmth Utilities

```css
.warm-shadow          /* Soft shadow with brand green tint */
.warm-shadow-hover    /* Enhanced shadow on hover */
.warm-rounded         /* Friendly 12px border radius */
.warm-spacing         /* Consistent 1.5rem bottom margin */
```

### Example Usage

```html
<div class="mud-card warm-shadow warm-rounded warm-spacing">
  <h3 class="heading-yellow">bruger profil</h3>
  <p>Dette er en varm og venlig card...</p>
</div>
```

## Blazor Component Examples

### Using Colors in MudBlazor

```razor
@using Jordnaer.Features.Theme

<MudPaper Style="@($"background-color: {JordnaerPalette.YellowBackground}")">
    <MudText Typo="Typo.h2" Style="@($"color: white")">
        velkommen
    </MudText>
</MudPaper>

<MudButton Color="Color.Primary"
           Style="@($"background-color: {JordnaerPalette.GreenBackground}")">
    Klik her
</MudButton>
```

### Typography in Blazor

```razor
<!-- Opt-in Cherry Bomb heading with color -->
<MudText Typo="Typo.h1" Class="heading-yellow">
    hovedoverskrift
</MudText>

<!-- Standard heading without Cherry Bomb -->
<MudText Typo="Typo.h2">
    standard overskrift
</MudText>

<MudText Typo="Typo.body1" Style="@($"color: {JordnaerPalette.BlueBody}")">
    Body tekst i den rigtige farve
</MudText>

<MudText Class="small-text">
    Lille tekst eller citat
</MudText>
```

## Accessibility

All color combinations meet WCAG contrast standards:

✅ **Good Contrast**:
- MØDE blue (#41556b) on white background
- MØDE red (#673417) on white background
- White text on GLÆDE yellow (#dbab45)
- White text on RO green (#878e64)
- White text on MØDE blue (#41556b)

❌ **Poor Contrast** (avoid):
- Yellow text on white background
- Beige text on white background
- Light blue text on white background

## Design Checklist

When implementing new features, ensure:

- [ ] Cherry Bomb headings are opt-in using `.heading-yellow`, `.heading-green`, or `.heading-blue` classes
- [ ] Heading colors rotate across sections when using Cherry Bomb (not all same color)
- [ ] Body text uses MØDE blue or MØDE red (never yellow/light colors)
- [ ] Background colors follow usage rules (primary: yellow/green, light: beige/blue)
- [ ] `MiniDivider` component used for major section breaks (not CSS dotted separators)
- [ ] `MudDivider` used for clean separators inside cards
- [ ] Interactive elements have smooth transitions (200-300ms)
- [ ] Cards have warm shadows and hover effects
- [ ] Consistent spacing maintained throughout
- [ ] Overall feel is warm, friendly, and harmonious

## Resources

- **Design Guide**: `docs/Mini Design Guide.pdf`
- **Color Palette**: `src/web/Jordnaer/Features/Theme/JordnaerPalette.cs`
- **CSS Variables**: `src/web/Jordnaer/wwwroot/css/app.css`
- **Fonts**: `src/web/Jordnaer/wwwroot/css/fonts.css`
- **Task Documentation**: `tasks/01-warmer-ui-ux.md`

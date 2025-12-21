# Jordnaer Design System Implementation Guide

## Overview

This document provides practical guidelines for using the Jordnaer (Mini Møder) design system based on the official `Mini Design Guide.pdf`.

**Philosophy**: Pragmatic and flexible. Use MudBlazor components, plain HTML, or CSS classes - whatever gets the job done efficiently while maintaining brand consistency. These are guidelines, not strict rules. Prioritize dev speed, maintainability, and good results.

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

### Usage Guidelines

- **GLÆDE (Yellow)**: Primary color for heading backgrounds and key elements
- **RO (Green)**: Secondary color for heading backgrounds, success actions (save buttons)
- **MØDE (Blue)**: Body text color - use for readable text
- **MØDE Red (Red-brown)**: Small text, payoffs, quotes
- **OMSORG (Beige)**: Background when yellow/green are too saturated
- **LEG (Blue)**: Lighter background option

⚠️ **Accessibility Note**: Avoid yellow or light colors for body text (readability issues)

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
- Headings use the standard font by default
- Cherry Bomb One is **opt-in** for marketing pages and special emphasis

**Cherry Bomb One Headings (Optional)**
- Add `.heading-yellow`, `.heading-green`, or `.heading-blue` classes when you want the fun brand font
- Letter spacing: `0.11em`, lowercase transform
- Best for short words and main headings on public-facing pages

```html
<h1 class="heading-yellow">velkommen til jordnaer</h1>
<h2 class="heading-green">find nye venner</h2>
<h3 class="heading-blue">lokal fællesskab</h3>
```

**Or just use MudBlazor directly with inline styles:**
```razor
<MudText Typo="Typo.h4" Style="@($"color: {JordnaerPalette.RedHeader}; font-family: 'Cherry Bomb One', cursive; letter-spacing: 0.11em;")">
    Din Profil
</MudText>
```

**Subheadings**
- Use `.subheading` or `.subheading-red` classes, or just style directly
- Letter spacing: `0.05em` is good for readability
- Color: MØDE blue or MØDE red

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

## Creating Visual Variety

**Optional**: When you want visual interest on marketing/public pages, consider rotating heading colors across sections.

### Quick Ways to Add Color

**CSS Classes** (easiest):
```html
<h1 class="heading-yellow">overskrift 1</h1>
<h2 class="heading-green">overskrift 2</h2>
<h3 class="heading-blue">overskrift 3</h3>
```

**Inline Styles** (when you need more control):
```razor
<MudText Typo="Typo.h4" Style="@($"color: {JordnaerPalette.RedHeader};")">
    Section Title
</MudText>
```

**Plain Colors** (simplest):
```html
<h2 style="color: #41556b;">Simple heading</h2>
```

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

### Optional: Color Variety

If you want to add visual interest (especially on public pages), you can vary colors:
```html
<section>
  <h2 class="heading-yellow">første sektion</h2>
  <p>Content...</p>
</section>
<section>
  <h2 class="heading-green">anden sektion</h2>
  <p>Content...</p>
</section>
```

**Note**: Multiple colors in one heading is only for the logo, but honestly, do what works.

## Visual Elements

### Separators

**MiniDivider** - For fun visual breaks (bee flight path dotted lines):
```razor
<MiniDivider Color="MiniDividerColor.Blue" Center Class="my-6" />
<MiniDivider Color="MiniDividerColor.Green" Center Class="my-6" />
```

**MudDivider** - For clean, simple separators:
```razor
<MudDivider Class="my-4" />
```

**Or just use whatever works:**
```html
<hr class="my-4" />
```

Use whichever fits your needs. MiniDivider adds brand personality, MudDivider is cleaner.

## Warmth & Interactions (Optional)

We have some nice-to-have interactive effects already set up:
- Buttons have smooth hover effects
- Links transition to GLÆDE yellow on hover
- Input fields show yellow outline when focused
- Cards can use `.warm-shadow` class for soft shadows

Feel free to use these utility classes when they help:
```css
.warm-shadow          /* Soft shadow with brand green tint */
.warm-rounded         /* Friendly 12px border radius */
.warm-spacing         /* Consistent 1.5rem bottom margin */
```

But don't stress about it - MudBlazor's defaults usually look fine.

## Practical Examples

### Colors in MudBlazor

```razor
@using Jordnaer.Features.Theme

<!-- Green save button -->
<MudButton ButtonType="ButtonType.Submit"
           Style="@($"background-color: {JordnaerPalette.GreenBackground}; color: white;")">
    Gem
</MudButton>

<!-- Section heading with brand color -->
<MudText Typo="Typo.h4"
         Style="@($"color: {JordnaerPalette.RedHeader}; font-family: 'Cherry Bomb One', cursive; letter-spacing: 0.11em;")">
    Din Profil
</MudText>

<!-- Or keep it simple -->
<MudText Typo="Typo.h5" Style="@($"color: {JordnaerPalette.BlueBody};")">
    Section Title
</MudText>
```

### Plain HTML Works Too

```html
<button type="submit" class="account-button-primary">Log ind</button>
<h1 class="heading-yellow">velkommen</h1>
```

Use whatever approach makes sense for your component.

## Accessibility

**Important**: Keep text readable!

✅ **Good contrast combinations**:
- MØDE blue (#41556b) on white
- MØDE red (#673417) on white
- White text on green, yellow, or blue backgrounds

❌ **Bad contrast** (avoid):
- Yellow, beige, or light blue text on white backgrounds

## Quick Guidelines

When building features, keep in mind:

**Must Do**:
- Use readable text colors (MØDE blue or red for body text)
- Maintain decent contrast ratios for accessibility

**Nice to Have**:
- Use brand colors for visual interest on public pages
- Add MiniDividers for fun visual breaks
- Keep spacing consistent
- Make it feel warm and friendly

**Don't Worry About**:
- Being pixel-perfect
- Memorizing all the CSS classes
- Using every utility class
- Strict color rotation rules

**When in doubt**: Use MudBlazor defaults and add brand colors where they make sense.

## Resources

- **Design Guide**: `docs/Mini Design Guide.pdf`
- **Color Palette**: `src/web/Jordnaer/Features/Theme/JordnaerPalette.cs`
- **CSS Variables**: `src/web/Jordnaer/wwwroot/css/app.css`
- **Fonts**: `src/web/Jordnaer/wwwroot/css/fonts.css`
- **Task Documentation**: `tasks/01-warmer-ui-ux.md`

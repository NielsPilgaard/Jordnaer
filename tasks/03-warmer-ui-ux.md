# Task 03: Make UI/UX Warmer

## Context

**App:** Jordnaer (.NET Blazor Server)  
**Area:** Global UI/UX  
**Priority:** High  
**Design Reference:** `docs/Mini Design Guide.pdf`

## Objective

Implement the official Mini Møder design system to create a warm, friendly, and harmonious visual identity throughout the application.

## Design System Overview

### Color Palette

The design uses a warm, nature-inspired palette with specific usage rules:

| Color Name | Hex Code | Usage |
|------------|----------|-------|
| **GLÆDE** (Joy) | `#dbab45` | Primary color - backgrounds for text and headings |
| **RO** (Calm) | `#878e64` | Secondary color - backgrounds for text and headings |
| **MØDE** (Meeting) | `#41556b` | Body text color |
| **MØDE** (Meeting) | `#673417` | Small text, payoff, quotes (red-brown) |
| **OMSORG** (Care) | `#cfc1a6` | Background when primary/secondary too dark, can use 50% transparency for better readability |
| **LEG** (Play) | `#a9c0cf` | Rarely used background when primary/secondary too dark/strong |

### Typography

**Headings: Cherry Bomb One** (Google Font)

- Use for short words and headings
- Primarily lowercase (not caps)
- Letter spacing: 110 (tracking between characters)
- Color on white background: green, yellow, or blue
- Color on colored background: blue or white
- **Important:** Don't use multiple colors in one heading (only logo does this)
- If too small to read, use Open Sans Bold instead

**Body Text: Open Sans Light** (Google Font)

- Use for body text
- Can use variants (Bold, Italic) to emphasize words/sentences
- Letter spacing: 410 for logo and subheadings, normal otherwise
- Color: Blue (#41556b) or red-brown (#673417)
- **Important:** Avoid light colors (like yellow) for readability

**Subheadings: Open Sans Light** (Google Font)

- Use where Cherry Bomb becomes unreadable
- Letter spacing: 410
- Color: Blue or red-brown (not light colors)

### Design Principles

1. **Color Variation:** Vary heading colors to create visual interest
   - If one heading is blue, make the next green, etc.
   - Use the dotted line separator and background colors to create variety
   - Only headings change colors, not individual letters (except in logo)

2. **Visual Elements:**
   - Bee icons can be used as decorative elements
   - Dotted "flight path" line can be used as separator between sections
   - Available in all brand colors

3. **Warmth & Harmony:**
   - Use warm earth tones throughout
   - Create visual rhythm with consistent spacing
   - Add subtle transitions to interactive elements

## Requirements

1. **Implement Color System**
   - Update CSS variables/design tokens with exact hex codes
   - Apply colors according to usage guidelines
   - Ensure proper contrast for accessibility

2. **Implement Typography**
   - Add Cherry Bomb One and Open Sans from Google Fonts
   - Set up proper font hierarchy (headings, subheadings, body)
   - Apply correct letter spacing (tracking)
   - Use lowercase for Cherry Bomb headings

3. **Create Visual Variety**
   - Rotate heading colors across pages/sections
   - Use background colors strategically
   - Implement dotted line separators

4. **Add Warmth**
   - Subtle transitions on interactive elements
   - Consistent spacing system
   - Warm, inviting overall feel

5. **Maintain Consistency**
   - Apply design system across all pages
   - Ensure brand identity is recognizable
   - Test readability and accessibility

## Acceptance Criteria

- [ ] Color palette implemented with exact hex codes from design guide
- [ ] CSS variables/tokens created for all brand colors
- [ ] Cherry Bomb One font loaded and applied to headings
- [ ] Open Sans Light loaded and applied to body text
- [ ] Letter spacing (tracking) correctly applied (110 for Cherry Bomb, 410 for subheadings)
- [ ] Heading colors vary across sections (not all same color)
- [ ] Text colors follow usage rules (blue/red-brown for body, no yellow text)
- [ ] Background colors used appropriately (GLÆDE/RO for primary, OMSORG/LEG for lighter needs)
- [ ] Dotted line separator implemented and used between sections
- [ ] Subtle transitions added to buttons, cards, links
- [ ] Consistent spacing system applied
- [ ] Design tested across key pages (home, profile, groups, search)
- [ ] Accessibility verified (contrast ratios meet WCAG standards)
- [ ] Overall feel is warm, friendly, and harmonious

## Implementation Steps

1. **Setup Fonts**

   ```css
   @import url('https://fonts.googleapis.com/css2?family=Cherry+Bomb+One&family=Open+Sans:ital,wght@0,300;0,400;0,600;0,700;1,300&display=swap');
   ```

2. **Create CSS Variables**

   ```css
   :root {
     /* Brand Colors */
     --color-glaede: #dbab45;    /* Joy - primary */
     --color-ro: #878e64;        /* Calm - secondary */
     --color-moede: #41556b;     /* Meeting - body text */
     --color-moede-red: #673417; /* Meeting - small text/quotes */
     --color-omsorg: #cfc1a6;    /* Care - light background */
     --color-leg: #a9c0cf;       /* Play - light background */
     
     /* Typography */
     --font-heading: 'Cherry Bomb One', cursive;
     --font-body: 'Open Sans', sans-serif;
   }
   ```

3. **Apply Typography**
   - Set headings to Cherry Bomb One with letter-spacing: 0.11em
   - Set body to Open Sans Light
   - Use lowercase for h1-h3 with Cherry Bomb

4. **Implement Color Rotation**
   - Create utility classes for heading color variants
   - Rotate colors across sections

5. **Add Transitions**
   - Buttons, links, cards: 200-300ms ease transitions
   - Hover states with subtle color shifts

## Files to Modify

- Global CSS/SCSS files (color variables, typography)
- Layout components (apply background colors)
- Heading components (Cherry Bomb font, color rotation)
- Button/card components (transitions)
- Any component with text (ensure correct font usage)
- **`src/web/Jordnaer/Features/Theme/JordnaerPalette.cs`** - Update yellow color

## Existing Implementation

**Good News:** Much of the design system is already partially implemented!

### Current State in `JordnaerPalette.cs`:

| Design Guide Color | Current Code | Status |
|-------------------|--------------|--------|
| **GLÆDE** `#dbab45` | `#fcca3f` ❌ | **NEEDS FIX** - Yellow is too bright |
| **RO** `#878e64` | `#878e64` ✅ | Correct |
| **MØDE** `#41556b` | `#41556b` ✅ | Correct |
| **MØDE Red** `#673417` | `#673417` ✅ | Correct |
| **OMSORG** `#cfc1a6` | `#cfc1a699` ✅ | Correct (60% opacity) |
| **LEG** `#a9c0cf` | `#a9c0cf66` ✅ | Correct (40% opacity) |

**Typography:** Open Sans Light is already configured for body text ✅

### Priority Fix

**Update `YellowBackground` in `JordnaerPalette.cs`:**

```csharp
// Current (too bright):
public static readonly MudColor YellowBackground = "#fcca3f";

// Should be (from design guide):
public static readonly MudColor YellowBackground = "#dbab45";
```

This will make the yellow warmer and more harmonious with the rest of the palette.

## Reference

See `docs/Mini Design Guide.pdf` for visual examples and full design system documentation.

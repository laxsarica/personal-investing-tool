---
name: ui-design-system
description: Skill for the frontend UI design system — color palette, typography, layout tokens, component rules, and design philosophy. Desktop-only, light theme, data-dense, table-first screener UI inspired by Tickertape density. Uses Inter + JetBrains Mono, no animations, hairline borders, monospace tabular numerals.
---

# Stock Screener — UI Design System

Personal stock screener/tracker. Desktop-only, light theme, data-dense, no framework. Reference: Tickertape-style density — the data is the interface, chrome stays out of the way.

---

## Color Palette

Two-color system: one neutral scale for structure, one accent for interactive/highlighted states. Gain/loss colors are functional, not decorative — required for reading price data at a glance.

| Token | Hex | Use |
|---|---|---|
| `--bg` | `#F7F8FA` | Page background |
| `--surface` | `#FFFFFF` | Table rows, cards, panels |
| `--border` | `#E4E7EC` | Hairline dividers, table borders |
| `--text-primary` | `#14181F` | Tickers, headings, key numbers |
| `--text-secondary` | `#6B7280` | Labels, metadata, muted text |
| `--accent` | `#2B4C7E` | Selected row, active tab, links, focus ring |
| `--accent-soft` | `#EAF0F8` | Accent background (hover, selected row fill) |
| `--gain` | `#158443` | Positive price change |
| `--loss` | `#C4291C` | Negative price change |

**Why this accent:** a muted slate-blue instead of the generic SaaS blue (`#3B82F6`) or fintech teal — reads as "instrument panel," not "marketing site." Desaturated enough to stay quiet next to red/green price data, which needs to dominate visually.

**Rule:** accent color is reserved for interaction (selection, active state, links). It never competes with gain/loss coloring in the same glance.

---

## Typography

Single scale, fixed px sizes (no responsive scaling needed — desktop only).

| Token | Font | Size | Weight | Use |
|---|---|---|---|---|
| `--font-sans` | Inter, system-ui | — | — | All text (UI labels, headings, body, numbers) |
| `text-heading` | sans | 16px | 600 | Section titles |
| `text-body` | sans | 13px | 400 | Labels, descriptions |
| `text-label` | sans | 11px | 500, uppercase, +0.03em tracking | Column headers |
| `text-data` | sans | 13px | 500, tabular-nums | Ticker, price, % change |
| `text-data-lg` | sans | 15px | 600, tabular-nums | Featured/watchlist price |

**Why single font:** We stripped out monospace fonts entirely to maintain a cleaner, unified look. We rely exclusively on the `--font-sans` font, using `tabular-nums` when decimal points and digits need to remain perfectly aligned down a column.

---

## Layout

Fixed, single-size — no breakpoints.

| Token | Value |
|---|---|
| `--radius` | 4px (rows/inputs), 6px (cards) — kept small, this isn't a marketing UI |
| `--row-height` | 36px (table rows) |
| `--space-1` | 4px |
| `--space-2` | 8px |
| `--space-3` | 12px |
| `--space-4` | 16px |
| `--space-5` | 24px |
| `--container-max` | 1280px |
| Borders | 1px hairline (`--border`), used instead of box-shadow/cards where possible |

**Table-first, not card-first.** Rows separated by hairlines, not individual boxed cards — keeps vertical density high, which is the point of a screener.

### Global Page Layout Classes
Use these exact classes for the layout of standard pages (like Dashboard, History, Jobs) to ensure complete consistency:
- `.main-content`: Flex-column wrapper for the page.
- `.main-header`: The top navigation/title bar of the page.
  - `.main-title`: The page title (e.g. `18px`, `600` weight).
  - `.header-actions`: Container for top right action buttons.
- `.btn-icon` / `.btn-save` / `.btn-export`: Standardized action buttons.
- `.results-bar`: A secondary bar below the header for showing result counts and refresh actions.
  - `.results-info` / `.results-actions`
- `.table-container`: The flex-wrapper for the core data table.

---

## Component Notes

- **Row hover:** `--accent-soft` background, no shadow, no scale/transform.
- **Gain/loss cells:** color text only (no background pill) — keep the row calm, let the number carry it.
- **Selected/watchlisted row:** left 2px accent border + `--accent-soft` fill.
- **Sort-active column header:** `--text-primary` + small direction arrow; inactive headers stay `--text-secondary`.
- **No animation beyond instant hover/focus states.** Nothing time-based — a screener is scanned, not admired.

---

## Signature element

A single hairline-bordered data table with monospace tabular numerals and color-only gain/loss (no pills, no icons) — the density and restraint *is* the design.

---

## Implementation Rules

### Tech Stack
- **Angular 20** — standalone components, no NgModules
- **TypeScript 5.8** — strict mode
- **Vanilla CSS** with CSS custom properties (design tokens) — no TailwindCSS, no PrimeNG
- **Google Fonts:** Inter (sans) + JetBrains Mono (mono)
- **Feature-based folder structure** under `features/`
- **kebab-case** file names for components

### CSS Custom Properties (Root)
```css
:root {
  /* Colors */
  --bg: #F7F8FA;
  --surface: #FFFFFF;
  --border: #E4E7EC;
  --text-primary: #14181F;
  --text-secondary: #6B7280;
  --accent: #2B4C7E;
  --accent-soft: #EAF0F8;
  --gain: #158443;
  --loss: #C4291C;

  /* Typography */
  --font-sans: 'Inter', system-ui, -apple-system, sans-serif;

  /* Layout */
  --radius: 4px;
  --radius-card: 6px;
  --row-height: 36px;
  --space-1: 4px;
  --space-2: 8px;
  --space-3: 12px;
  --space-4: 16px;
  --space-5: 24px;
  --container-max: 1280px;
}
```

### Anti-Patterns (Do NOT Do)
1. ❌ No card-based layouts for list data — use tables with hairline rows
2. ❌ No colored pills/badges for gain/loss — color the text only
3. ❌ No box-shadows on table rows — hairline borders only
4. ❌ No time-based animations, transitions, or transforms on data elements
5. ❌ No responsive breakpoints — this is desktop-only (1280px container)
6. ❌ No TailwindCSS, Bootstrap, or utility-class frameworks
7. ❌ No marketing-style hero sections or decorative elements
8. ❌ No dark mode (light theme only)

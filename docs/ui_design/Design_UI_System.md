# Flight Supervisor UI Design System

This document outlines the visual language, typography, and component specifications used across the Flight Supervisor application. Future agents and developers MUST respect these guidelines to maintain a cohesive, high-density, and professional aviation aesthetic.

## 1. Color Palette

The application uses a dark mode, high-contrast, professional scheme inspired by modern avionics.

*   **App Background:** `#030712` (Tailwind `gray-950`) or `#0F111A`
*   **Panel / Card Background:** `#1C1F26` (often combined with `bg-gradient-to-br from-[#1C1F26] to-[#12141A]`)
*   **Borders:** Soft translucent white borders (`border border-white/5` or `border-slate-800`).
*   **Primary Accent:** Tailwind `sky-400` (`#38bdf8`) to `sky-500` (`#0ea5e9`). Used for active states, primary headers, highlights, and primary action buttons.
*   **Secondary Text (Labels/Hint):** Tailwind `slate-400` (`#94a3b8`) or `slate-500`.
*   **Primary Text:** Tailwind `slate-200` (`#e2e8f0`) or `white`.

### Status Colors
*   **Success/Good:** `emerald-400` (`#34D399`)
*   **Warning/Delay:** `amber-400` (`#FBBF24`) or `orange-500`
*   **Danger/Penalty:** `red-400` (`#F87171`) or `rose-500`
*   **Info:** `sky-400`

## 2. Typography

We rely on three core typefaces to build hierarchy and a "chic" dashboard feel. 
(Fonts are declared in `index.html` via Google Fonts and Tailwind config).

*   **Headlines / Titles (`font-headline`):** (e.g., Mona Sans or similar strong sans-serif).
*   **Data / Code (`font-mono`):** (e.g., JetBrains Mono). Used for all aviation data (altitudes, speeds, fuel, ICAO codes).
*   **Labels / Caps (`font-label`):** Used for section headers and small labels.
*   **Standard Text (`font-body`):** (e.g., Inter).

### Shared Title Styling
*   **Window/Panel Titles (Settings, Manifest, Ground Ops):**
    *   Classes: `text-sky-400 font-label tracking-[0.4em] uppercase text-xs opacity-80 mb-6`.
    *   *Rule:* ALL panel titles and section subheadings must feature wide tracking (`tracking-widest` or `tracking-[0.4em]`) and be uppercase to give a premium, spacious feel.

## 3. UI Components

### Buttons
*   **Primary Settings Save Button:** 
    *   Design: Fully rounded (pill shape).
    *   Classes: `rounded-full bg-sky-600 hover:bg-sky-500 text-white font-bold text-xs uppercase tracking-widest px-8 py-3`.
*   **Bottom Action Bar Buttons (e.g., START OPS) & Modals:** 
    *   Design: Squared-off corners (rounded-xl) - also referred to as "Design Carré".
    *   Classes: `rounded-xl uppercase font-bold text-[11px] tracking-widest`.
    *   *Rule:* All action buttons must be explicitly uppercase in the HTML and JS logic (e.g. `START OPS`, `FETCH PLAN`, `CANCEL FLIGHT`, `CONTINUE`).

### Cards & Containers
*   **Standard Card:** `bg-[#1C1F26] rounded-xl border border-white/5 p-6 shadow-xl`.
*   Layout grids should prefer CSS Flexbox in columns for aligning components vertically perfectly (e.g., Settings screen uses two distinct flex columns to avoid vertical gaps between differently sized cards).

### Icons
*   Library: **Google Material Symbols Outlined**.
*   Size: Usually `text-[18px]` to `text-[24px]`.
*   Ground Ops styling: Custom coloring per service (Refuel: orange, Boarding: sky, Catering: pink, Water/Waste: emerald). 

## 4. Window Metrics
*   **Default Window Size:** `1850 x 1020`. The UI is designed to be spacious and readable in a windowed state without requiring maximum screen utilization. Use large fonts to prevent "squinting".

## 5. Development Principles
1.  **Don't reinvent the wheel:** If a new panel is needed, duplicate the layout structure, borders, and typography of an existing one (e.g., Manifest or Settings).
2.  **No gaps / Ragged edges:** Ensure sections align vertically. Use flex columns if grid elements vary significantly in height.
3.  **Caps & Tracking:** When in doubt for a UI label or title, use uppercase with wide tracking for immediate "dashboard" credibility.

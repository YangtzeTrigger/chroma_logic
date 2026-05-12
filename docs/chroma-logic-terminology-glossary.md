# Chroma-Logic Gallery — Terminology & Hierarchy Glossary
**Version:** 1.0 | **Status:** Pre-Development Handoff Reference
**Purpose:** Single source of truth for all named systems, screens, tiers, ranks, and vocabulary. Every developer, designer, and writer working on this project must use these terms consistently.

---

## 1. The Institution

| Term | Definition |
|---|---|
| **The Gallery** | The overarching institutional identity of the game world. Used as the primary header across all screens. Never referred to as "the game" in UI copy. |
| **Logic Vessels** | The collective name for all puzzle experiences within The Gallery. A Vessel is a single puzzle (Sudoku grid + hidden image + Jigsaw). |
| **Curator** | The player's identity within The Gallery. Players are never called "players" in UI copy. |
| **The Sanctuary** | The broader world the player inhabits. Includes the Curator's Sanctuary (Daily Meditation space) and the institutional atmosphere. |

---

## 2. Gameplay Terms

| Term | Definition |
|---|---|
| **Vessel** | A single complete puzzle unit: one Sudoku grid, one hidden image, one Jigsaw. |
| **Sequence** | A series of Vessels within a Pack, played in progression. |
| **Logic Fragment** | An individual puzzle step or sub-completion within a Vessel (e.g. Fragment 12/14). |
| **Gemstone Tile** | One of the nine 3D-rendered puzzle pieces used in the Sudoku grid. |
| **Visual Flash** | The mechanic where completed rows, columns, or boxes briefly reveal the hidden image beneath the tiles. |
| **The Reveal** | Phase B of the triple loop — the full image bloom animation after a Sudoku grid is completed. |
| **Clarity Level** | The difficulty system. Four tiers: Sunlit (easy), Overcast (medium), Moonlit (hard), Void (expert). Never referred to as "difficulty" in UI copy. |
| **Safety Net** | The three-error system. Three dots displayed in the Sudoku UI. Depleting all three triggers a Rewarded Ad prompt. |
| **Insight** | The hint mechanic — reveals one correct tile (Sudoku) or snaps nearest piece (Jigsaw). Also the currency unit used in the Path of Prestige progress bar. |

---

## 3. The Nine Gemstone Tiles

| Slot | Shape Name | Notes |
|---|---|---|
| 1 | **Infinity** | Replaces Circle. Figure-8 silhouette. Orientation TBD (see Open Question Q16). |
| 2 | **Star** | 5-pointed, chunky. Not thin-spoked. |
| 3 | **Hexagon** | Flat-top orientation. |
| 4 | **Crescent** | Clear horns. Not ambiguous with Ring. |
| 5 | **Diamond** | Rotated square. Tall aspect ratio. |
| 6 | **Plus** | Equal arm lengths. Rounded ends. |
| 7 | **Heart** | Symmetrical. Legible at 48×48dp minimum. |
| 8 | **Ring** | Thick annulus. Clear centre hole. |
| 9 | **Triangle** | Equilateral. Point-up orientation. |

**Nine Colour Identities (one per tile in a solved grid):**
Crimson, Amber, Jade, Cobalt, Violet, Teal, Rose, Slate, Gold

---

## 4. The Triple Loop

| Phase | Name | Informal Label |
|---|---|---|
| **Phase A** | The Sudoku Grid | "The Work" |
| **Phase B** | The Reveal | "The Reward" |
| **Phase C** | The Jigsaw Gallery | "The Retention" |

---

## 5. Jigsaw Mode

| Term | Definition |
|---|---|
| **Meditative** | 12-piece jigsaw difficulty |
| **Focused** | 24-piece jigsaw difficulty |
| **Challenging** | 48-piece jigsaw difficulty |
| **Grand Master** | 96-piece jigsaw difficulty — unlocked at Gallery XP Level 10 |
| **Scatter Animation** | The opening animation when a jigsaw begins. 48-piece: physics cascade scatter. 96-piece: slow drift. |
| **Living Image** | The hidden animated reward revealed when a jigsaw is completed. An undisclosed feature — no UI hint. |

---

## 6. Collection & Progression

| Term | Definition |
|---|---|
| **Pack** | A collection of 12 Vessels grouped by theme (e.g. Coastal Solitude, Deep Space). |
| **Grand Masterpiece** | A bonus panoramic image unlocked by completing all 12 Vessels in a Pack. Only playable as a 96-piece jigsaw. |
| **Archive** | The player's permanent record of all completed Vessels, accessible from the main navigation. |
| **Archive of Logic Vessels** | The formal screen title for the Archive. Header: "Authenticated Collection." |
| **Logic Points** | The primary progression currency. Earned through puzzle completion, engagement, and social reciprocity. Displayed in Curator's Ledger. |
| **Gems Collected** | Lifetime count of individual Gemstone Tile placements. Displayed in Curator's Ledger. |
| **Revelation** | The time metric replacing "hours played." E.g. "128h Revelation." Never referred to as "time played" in UI copy. |
| **Gallery XP** | Experience points earned through Jigsaw completion. Unlocks cosmetic rewards and jigsaw difficulty tiers. |

---

## 7. Path of Prestige — Curator Rank System

Six tiers in ascending order. Each has a formal name, subtitle, and a single unlock reward.

| Tier | Formal Name | Subtitle | Unlock Reward |
|---|---|---|---|
| 1 | **Neophyte** | *The Awakening* | Access to The Gallery. Entry rank. |
| 2 | **Scholar** | *The Focused Mind* | Custom Haptic Resonance |
| 3 | **Architect** | *The Pattern Seeker* | Teardrop gemstone geometry (cosmetic skin) |
| 4 | **Master** | *The Resonant Soul* | Infinity symbols and gold leaf tile accents |
| 5 | **Grandmaster** | *The Logic Sage* | 96-piece Grand Master jigsaw difficulty |
| 6 | **Arch-Curator of the Void** | *The Eternal* | The Eternal Void Aura — unique particle trail across all vessels |

**Progress bar shorthand:** "Eternal" is the confirmed shorthand for Arch-Curator of the Void in progress bar contexts (e.g. "2,000 for Eternal").

**Icon system:** Each tier has a unique symbol displayed in a circle node on the Path of Prestige screen. Illuminated (gold fill) for completed tiers, dark for locked tiers.

---

## 8. Earned Designations

Designations are distinct from Rank. They are awarded for specific achievements and displayed on the Curator Profile under "Earned Designations."

| Designation | Earn Condition |
|---|---|
| **Sovereign of Symmetry** | 500 consecutive logic matches |
| **Ethereal Visionary** | Complete 'The Zenith' (landmark puzzle — see Open Question Q11) |
| **Prismatic Scholar** | Achieve mastery of the Color Spectrum |
| **Curator Ascended: Master of Logic** | Attain Grandmaster rank |

*Additional designations to be defined during content production.*

---

## 9. Screens & Spaces

| Screen Name | Navigation Label | Description |
|---|---|---|
| **Main Gallery Dashboard** | Gallery | Primary home screen. Featured Vessel, Active Sequences, Curator's Ledger. |
| **The Work** | Solve | Active Sudoku puzzle screen. |
| **Curator's Sanctuary** | Meditate | Daily Meditation landing and active 4×4 grid screen. |
| **Archive of Logic Vessels** | Archive | Completed Vessel catalogue with filter tabs. |
| **Path of Prestige** | — | Rank progression screen. Accessed from Profile. |
| **Aesthetic Calibration** | Settings | Accessibility and visual customisation screen. |
| **Gemstone Collection** | — | Tile set selection and premium collection unlock screen. |
| **Curator Showcase: The Resonant Elite** | — | Community exhibition screen. Formerly "Rankings of the Resonant." |
| **The Shared Ledger** | — | Community feed of shared Vessel reflections. v1.5 feature. |
| **Zen Pass** | — | Premium upgrade screen. One-time $4.99. |

**Confirmed navigation structure (four tabs):**
Gallery / Solve / Meditate / Archive
Settings accessible via hamburger menu or profile icon.

---

## 10. Monetisation Terms

| Term | Type | Description |
|---|---|---|
| **Zen Pass** | One-time IAP ($4.99) | Removes all interstitial/banner ads. Unlocks 3 exclusive Shape Style packs. Unlocks all jigsaw difficulty tiers immediately. |
| **Safety Net** | Rewarded Ad | "Watch to restore your focus." Resets error count. Max once per puzzle session. |
| **The Insight** | Rewarded Ad | Opt-in hint. Reveals one correct tile or snaps nearest jigsaw piece. |
| **The Souvenir** | Rewarded Ad | "Download as wallpaper (4K)." Post-reveal only. Once per image. |
| **Quick-Flip** | Interstitial Ad | 5-second skippable. Between puzzle levels only. Max once per 2 completions. Never on first session. Never during Daily Canvas. Suppressed by Zen Pass. |
| **Pack Unlock** | IAP ($1.99/pack or $9.99 annual bundle) | Unlocks a full 12-Vessel image pack. Annual bundle includes all future packs for 12 months. |

---

## 11. Social Layer Terms (v1.5)

| Term | Definition |
|---|---|
| **The Shared Ledger** | Community feed of Logic Vessels shared by curators with personal reflections. |
| **Resonate** | Social action replacing "Like." Implies genuine connection with another curator's experience. Count is private — no public tally. |
| **Observe** | Social action replacing "Follow" or "View." Implies scholarly attention to a vessel or curator. |
| **Reflection** | A short personal statement written by a curator about a completed Vessel. Displayed on Shared Ledger cards and Curator Showcase profiles. Origin (user-generated vs AI-assisted) TBD — see Open Question Q7. |
| **Recipient Aura** | The visual treatment applied to an Invite to Solve. Three options: Celestial, Nebula, Ether. Selected by the inviting curator. |
| **Export Essence** | CTA that generates a shareable composed asset from a completed Vessel. Output format TBD — see Open Question Q8. |
| **Curator Showcase** | The community exhibition screen. Curators featured based on weighted composite score, not raw ranking. |
| **Collective** | The community of all curators within The Gallery. Referenced in the Showcase as "Your Contribution to the Collective." |

---

## 12. Aesthetic & Visual Terms

| Term | Definition |
|---|---|
| **Quiet Luxury Glassmorphism** | The visual style of the game. Deep desaturated backgrounds, frosted glass UI panels, warm gold accents. Specifically not garish neon glassmorphism. |
| **Active Atmosphere** | The current visual theme environment. v1.0 launches with: Quiet Luxury and Obsidian Gold. |
| **Component Tuning** | The three adjustable UI parameters in Aesthetic Calibration: Glass Opacity (default 82%), Symbol Glow Intensity (default 45%), Refraction Depth (default 34px). |
| **Symbol-Only Mode** | Accessibility mode removing all colour information. Shapes become the sole discriminator. Referred to in copy as "high-contrast etched glyphs." |
| **Void Aura** | The Arch-Curator unlock reward. A deep obsidian particle trail that follows selection movements across all puzzle interactions. |

---

## 13. Brand & Copy

| Term / Line | Usage |
|---|---|
| **"Logic Vessels. Eternal Patterns."** | Primary tagline. Used on splash screen, marketing materials, App Store listing. |
| **"Logic dictates the flow. Color reveals the path."** | Onboarding screen 2 (Chromatic Harmony). Explains dual-axis mechanic. Keep verbatim. |
| **"The pattern follows the breath. Inhale on placement, exhale on revelation."** | Daily Meditation active screen. Keep verbatim. |
| **"A selection of precision-cut logic vessels for the modern curator."** | Gemstone Collection screen subtitle. Keep verbatim. |
| **"The Archive Grows — You have successfully reconstructed X of the 120 known Logic Vessels. Your precision as a curator is noted."** | Archive milestone card. Keep verbatim. X is dynamic. |
| **"Your Contribution to the Collective"** | Curator Showcase personal standing header. Keep verbatim. |
| **"Arranging the celestial gemstones..."** | Loading screen message. One of a rotating set. Keep verbatim as first rotation. |
| **"Enter The Gallery"** | Primary CTA on splash/title screen. Keep verbatim. |
| **"Accept Honor"** | Achievement screen CTA on rank promotion. Keep verbatim. |
| **"Revelation"** | Always capitalised when used as a time metric. "128h Revelation" not "128 hours played." |

---

## 14. Loading Screen Message Rotation

All messages use the same register — present continuous, poetic, preparation-focused:

1. *"Arranging the celestial gemstones..."*
2. *"Calibrating the logic of light..."*
3. *"The patterns are finding their order..."*
4. *"Your vessels await curation..."*
5. *"Preparing the sanctuary..."*

---

*Document owner: [Project Lead]*
*Last updated: May 2026*
*To be read alongside: Master Brief v1.1 and Design Session Debrief*
*This document is the authoritative source for all terminology. Any new term introduced during development must be added here before use.*

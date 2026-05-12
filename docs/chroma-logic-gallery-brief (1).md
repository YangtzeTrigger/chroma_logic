# Master Brief: *Chroma-Logic Gallery*

**Version:** 1.1 | **Status:** Pre-Production Reference Document
**Genre:** Hybrid-Casual Puzzle — Logic / Discovery / ASMR
**Platform:** Android (Google Play) — primary; iOS (App Store) — Phase 2
**Target Audience:** 25–45, puzzle-curious, aesthetics-driven, casual-to-mid-core
**Monetisation Model:** Ad-supported F2P with optional one-time premium tier

---

## 1. Elevator Pitch

> *"Chroma-Logic Gallery replaces the cold anxiety of Sudoku numbers with nine jewel-toned shapes. Solve the grid. Shatter the tiles. Reveal a masterpiece."*

The player never stares at a blank puzzle — a hidden high-resolution image sits beneath every grid, teased through partial reveals as they progress. Completion is not an endpoint; it opens a curated gallery they own permanently. The loop runs: **Work → Wonder → Rest**, designed to satisfy completionists, ASMR seekers, and players who just want something beautiful to look at before bed.

---

## 2. Core Design Philosophy

| Principle | Application |
|---|---|
| **Anxiety removal** | No timers on any default mode. Numbers replaced by shapes. Difficulty framed as "Clarity Level," not "Hard." |
| **Progressive revelation** | The hidden image is the emotional engine. Every solved section is a small reward; full reveal is the milestone. |
| **Tactile prestige** | Sound and haptics must feel like premium physical objects — glass, stone, silk — not generic mobile taps. |
| **Respectful monetisation** | Ads are never blocking. Every paid prompt has a free alternative path, even if slower. |

---

## 3. The Triple-Loop: Gameplay Mechanics

### Phase A — The Sudoku Grid ("The Work")

**Core Rules**
Standard 9×9 Sudoku constraint: no shape *and* no color may repeat in any row, column, or 3×3 box. Both dimensions must be satisfied, giving players two simultaneous logical axes and naturally doubling the puzzle depth without increasing perceived complexity.

**Input System**
- Nine distinct 3D-rendered "Gemstone" tiles: *Circle, Star, Hexagon, Crescent, Diamond, Plus, Heart, Ring, Triangle.*
- Each tile rendered in one of nine color identities (Crimson, Amber, Jade, Cobalt, Violet, Teal, Rose, Slate, Gold).
- Shape + color combinations each occupy exactly one cell in a solved grid.
- Tiles are placed via **drag-and-drop** (primary) or **tap-to-select + tap-to-place** (accessibility fallback).

**Tactile & Audio Feedback**
- **Placement:** A soft glass "clink" with a micro-vibration pulse (12ms).
- **Correct row/column/box completion:** A resonant tonal chime, ascending pitch per consecutive completion.
- **Error state:** No harsh buzz — a subtle low pulse, tile wobbles and returns. Three errors available before "Safety Net" prompt (see Monetisation).
- **Wrong-tile indicator:** Cell glows amber with a soft ripple; never red (avoids stress association).

**The Visual Flash Mechanic**
Upon completing any 3×3 box, that box's tiles turn frosted-transparent for 2.5 seconds, revealing the HD image segment beneath in full saturation. This is the primary retention hook — players lean forward to glimpse what's hidden. Upon completing a full row or column, *all* tiles in that line briefly illuminate their underlying image, then fade. This creates escalating anticipation as the grid nears completion.

**Difficulty Tiers (re-framed language)**
| Display Name | Technical Equivalent | Pre-filled Cells |
|---|---|---|
| Clarity — Sunlit | Easy | 38–45 |
| Clarity — Overcast | Medium | 28–35 |
| Clarity — Moonlit | Hard | 22–27 |
| Clarity — Void | Expert | 17–21 |

All tiers available from the start; "Sunlit" is the onboarding default.

---

### Phase B — The Reveal ("The Reward")

- On final tile placement, a 1.5-second "crystallise" pause — the grid vibrates gently.
- Tiles shatter outward in a physics-driven particle burst (each shard retains its gem color).
- The full image rises from beneath in a slow, top-to-bottom bloom animation (≈2.5 seconds).
- A soft, cinematic ambient swell plays — distinct from all in-game SFX.
- Image is auto-saved to the player's **Permanent Collection** with metadata: date, difficulty, completion time, puzzle pack name.
- A one-tap "Share" option appears (see Social API, Section 7).

---

### Phase C — The Masterpiece Gallery ("The Retention")

- A scrollable gallery of all unlocked images, styled as a curated art exhibition (dark walls, subtle lighting halos per frame).
- Each image can be tapped to launch a **Jigsaw mode** at the player's chosen piece count: 12 / 24 / 48 / 96 (96-piece unlocked via Gallery XP).
- Jigsaw controls: two-finger pinch-zoom; piece rotation via long-press + drag.
- **Snap radius** is generous on 12/24, tightens on 48/96 to maintain challenge.
- Completed jigsaws grant **Gallery XP**. XP milestones unlock:
  - New color palette sets for Sudoku gemstones (cosmetic).
  - Animated "foil" tile variants (subtle shimmer on placed tiles).
  - Additional jigsaw piece counts.
- Gallery can be toggled between **Grid View** and **Exhibition View** (single-image spotlight with pan/zoom).

---

## 4. Meta-Progression & Player Retention Systems

### The Daily Ritual
A non-pressurised daily engagement layer:
- **"Today's Canvas"** — one free daily puzzle with an exclusive image not available in the standard pack rotation. Completing it adds the image to the gallery permanently.
- **Streak Tracker** displayed as a calendar strip, not a countdown timer. Streaks grant cosmetic tile effects (no gameplay advantage). Breaking a streak costs nothing mechanically.

### The Collection Arc
- Images organised into **Packs** of 12 (e.g., *Coastal Solitude*, *Deep Space*, *Botanical Hour*, *Urban Geometry*).
- Each pack has a completion badge. Completing all 12 images in a pack reveals a **bonus "Grand Masterpiece"** — a panoramic image that can only be assembled as a 96-piece jigsaw.
- The incomplete pack progress bar is always visible as a passive background motivator.

### The Progression Spine
```
New Player
    ↓
Onboarding (3 guided puzzles, Sunlit difficulty, pre-selected pack)
    ↓
Free Rotation (rolling 3-pack selection; packs rotate weekly)
    ↓
Gallery XP unlocks cosmetic depth
    ↓
Daily Canvas drives 7-day habit formation
    ↓
Pack completion → Grand Masterpiece → Social share moment
    ↓
New pack released (biweekly cadence)
```

---

## 5. Onboarding Design

The first session must be frictionless, emotionally engaging, and zero-text where possible.

**Session 1: The Guided Reveal (≈4 minutes)**
1. Player opens app → a single tile glows on an otherwise blank grid. A gentle hand-drawn arrow indicates "drag here."
2. Three-cell tutorial: place three pre-selected tiles correctly with guided highlights. No mistakes possible (incorrect cells are locked).
3. Complete a 3×3 box → first "Visual Flash." The image fragment is designed to be maximally compelling (e.g., a vivid macro of a butterfly wing or a galaxy spiral).
4. Complete the tutorial grid (simplified 6×6 rather than 9×9) → full reveal animation plays. No skip.
5. Gallery opens. One image is pre-populated. Tutorial ends.

**Session 1 → Session 2 Hook**
End of Session 1 shows a silhouetted second image: *"Your next masterpiece is waiting."* No timer, no forced return prompt — purely aspirational.

---

## 6. Visual & Audio Design Specification

### Visual Style
**"Quiet Luxury Glassmorphism"** — not the garish neon variant. Think frosted quartz, not nightclub.

- **Backgrounds:** Deep, desaturated gradients (near-black indigo to near-black teal). Subtle animated particle drift (very slow, ≤0.3 opacity).
- **UI panels:** True frosted glass — `backdrop-filter: blur(20px)`, 12–15% white fill, 1px white border at 30% opacity.
- **Gemstone tiles:** 3D-rendered with subsurface light scattering. Each color has a distinctive light temperature (e.g., Crimson = warm orange-red glow; Cobalt = cool blue-white internal light). Tiles should look like they're lit from within.
- **Typography:** Thin-weight, high-legibility display font for headers (e.g., Cormorant Garamond Light or similar serif with optical presence). Clean geometric sans for UI labels. No system fonts in shipping build.
- **Color Palette (UI Chrome):**
  - Background base: `#0D0F1A`
  - Glass surface: `rgba(255,255,255,0.08)`
  - Accent glow: `#C8A97E` (warm gold — prestige signal)
  - Success state: `#7ECBA1` (soft mint)
  - Alert state: `#D4A853` (amber — not red)

### Audio Design
- **Background:** Generative ambient — slow-evolving pads, no beat, no lyrics. Three distinct "moods" cycle across puzzle sessions (Coastal, Celestial, Forest). Player can lock a mood in settings.
- **Tile placement SFX:** Three layered samples for variation — glass clink, soft stone tap, crystal tap. Randomly selected on each placement to avoid mechanical repetition.
- **Row/column/box complete:** Ascending pentatonic chime sequence (C – E – G – A – C). Each consecutive completion raises the root note by a semitone, creating a satisfying harmonic build toward the final reveal.
- **Reveal SFX:** A single reverb-washed orchestral swell (≈3 seconds), non-looping. This is reserved *only* for the full grid reveal — players should feel its rarity.
- **All audio mixable** via a four-channel in-settings mixer: Music / Tile SFX / UI SFX / Haptic Intensity.

---

## 7. Monetisation Architecture

### Design Principle
Every monetisation touchpoint passes the **"Would this annoy me?"** test. No mid-session interruptions. No dark patterns. Premium purchase is a quality signal, not a paywall.

| Feature | Mechanism | Placement | Notes |
|---|---|---|---|
| **Zen Pass** (one-time $4.99) | IAP — Permanent | Post-reveal of 3rd puzzle | Removes all interstitial/banner ads; unlocks 3 exclusive Shape Style packs (alternate tile silhouette sets, e.g., Botanical, Celestial, Architectural); unlocks all four jigsaw difficulty tiers immediately |
| **Safety Net** | Rewarded Ad | After 3rd error in Sudoku | "Watch to restore your focus" — resets error count; never shown more than once per puzzle session |
| **The Insight** | Rewarded Ad | Opt-in only, via hint button | Reveals one correct tile (Sudoku) or snaps nearest piece (Jigsaw); button visible always, prompt only on tap |
| **The Souvenir** | Rewarded Ad | Post-reveal screen | "Download as wallpaper (4K)" — triggers only after full reveal; single prompt, never repeated for same image |
| **Pack Unlock** | IAP ($1.99/pack or $9.99 annual bundle) | Gallery → locked pack | New packs release biweekly; free rotation gives 3 packs at all times; bundle includes all future packs for 12 months |
| **Quick-Flip** | Interstitial (5-sec skippable) | Between puzzle levels only | Maximum once per 2 completed puzzles; *never* on first session; *never* during Daily Canvas; suppressed by Zen Pass |
| **Banner Ad** | Standard banner | Gallery screen bottom rail | Non-intrusive placement; suppressed by Zen Pass |

**Revenue Projection Levers (developer reference):**
- Primary volume: Rewarded ads (Safety Net + Insight) — maximised by correct difficulty tuning
- Primary LTV: Zen Pass conversion ≈ target 8–12% of D7 retained users
- Secondary LTV: Pack IAP — driven by collection completion psychology

---

## 8. Technical Requirements

### Engine
**Unity** (preferred) — for 3D gemstone physics, particle systems, and cross-platform parity for iOS Phase 2.
Godot acceptable if team has existing expertise; confirm 3D tile render pipeline before committing.

### Rendering
- Gemstone tiles: PBR shader with custom subsurface scattering parameter; baked light maps for performance on mid-range Android (Snapdragon 6 series target minimum).
- Image reveals: Streamed from compressed asset bundles (ASTC for Android); never loaded fully into memory until puzzle is ≥80% complete.
- Target: **60fps stable** on Android devices from 2021 onwards. 30fps fallback mode auto-detected on low-RAM devices.

### Accessibility
| Mode | Specification |
|---|---|
| **Symbol-Only** | Color information removed entirely; shapes become the sole discriminator; UI labels replace color names with shape names |
| **High Contrast** | Black backgrounds, white UI outlines, no glassmorphism blur; all decorative animation disabled |
| **Large Tile Mode** | 130% tile scale; UI elements reflow; for motor-impaired users |
| **Haptic Toggle** | All haptics individually toggleable; not a binary on/off |
| **Audio Descriptions** | VoiceOver/TalkBack compatible cell coordinates announced on placement |

All modes accessible from the **first session** via a persistent accessibility button in the top corner — not buried in settings.

### Asset Pipeline
- Images stored as **Pack Bundles** (12 images per pack, downloadable OTA after install).
- Each image: source 4K (3840×2160), delivery 1080p in-game, 4K reserved for "Souvenir" download.
- Pack metadata file (JSON): pack ID, image IDs, unlock state, completion flags.
- New pack release = new bundle; no app update required.
- IAP integration via **Google Play Billing Library 6+**; receipt validation server-side.

### Social API
- Post-completion share generates a 1:1 square composite: completed jigsaw image + subtle game logo watermark (bottom-right, 8% opacity) + pack name as caption suggestion.
- Output as `.jpg` (85% quality) directly to Android share sheet.
- Deep link in share URL opens app to matching puzzle pack (or Play Store if not installed).
- No mandatory social login. No leaderboards in v1.0.

### Analytics Events (minimum viable)
`puzzle_started`, `puzzle_completed`, `puzzle_abandoned`, `phase_b_reveal_watched` (duration), `gallery_opened`, `jigsaw_started`, `jigsaw_completed`, `ad_shown`, `ad_completed`, `ad_skipped`, `iap_prompt_shown`, `iap_completed`, `daily_canvas_claimed`, `streak_broken`, `share_tapped`, `share_completed`

---

## 9. Launch & Content Roadmap

| Phase | Content | Notes |
|---|---|---|
| **v1.0 Launch** | 4 packs (48 puzzles), all 4 difficulty tiers, Daily Canvas, Jigsaw (12/24/48 pieces), Zen Pass, Rewarded Ads | Feature-complete core loop |
| **v1.1 (Week 4)** | 2 new packs, seasonal Daily Canvas calendar | Driven by retention data from launch cohort |
| **v1.2 (Month 3)** | 96-piece jigsaw tier, Pack Bundle IAP, alternate Shape Style packs | Revenue expansion after LTV baseline established |
| **v2.0 (Month 6)** | iOS port, "Community Canvas" (player-voted image for next pack), animated image tier | Growth phase |

---

## 10. Success Metrics (KPIs)

| Metric | Target (D30 cohort) | Rationale |
|---|---|---|
| **D1 Retention** | ≥42% | Industry mid-core benchmark |
| **D7 Retention** | ≥18% | Daily Canvas habit formation indicator |
| **D30 Retention** | ≥9% | Collection arc depth indicator |
| **Session Length (median)** | 8–14 minutes | One full puzzle cycle |
| **Sessions/DAU** | 1.8 | Daily Canvas driving return |
| **Rewarded Ad Rate** | ≥35% of active users/day | Correct difficulty calibration drives this |
| **Zen Pass CVR** | ≥8% of D7 retained | Positioned as quality signal, not pressure |
| **ARPU (D30)** | ≥$0.35 | Blended ad + IAP |

---

## 11. Creative & Marketing Direction

### Brand Voice
Calm. Considered. Slightly luxurious. Never frantic, never gamified in the aggressive sense. This is a *gallery*, not an arcade.

### Key Visual Language
- Hero image: A single gemstone tile hovering above a near-complete grid, its facets casting soft coloured light, the image below just barely visible.
- Colour grade: Deep indigo environment, single warm-gold light source, cool-blue tile glow.

### Ad Creative Formats

**15-second Performance Ad (Primary)**
*[0–3s]* A messy, incomplete grid. Player places one tile. Soft clink.
*[3–8s]* Three tiles placed rapidly. A 3×3 box completes. Flash — a kitten's eye, a galaxy spiral, a forest waterfall — glimpsed for 1 second, then gone.
*[8–13s]* Final tile placed. Shatter. A full landscape image blooms. Player gasps (diegetic audio).
*[13–15s]* "Unlock the masterpiece." App icon. "Free on Google Play."
Caption: *"Only 1% solve it without hints. Can you see the full picture?"*

**6-second Bumper Ad (YouTube)**
Full reveal animation only — shatter to image bloom — no gameplay. Pure emotional payoff. No text until final frame.

**Store Listing Screenshots**
1. Split-screen: grid (left) / reveal moment (right). Caption: "Solve. Shatter. Reveal."
2. Gallery view of 6 framed masterpieces. Caption: "Your collection, earned."
3. Jigsaw mode mid-assembly, beautiful image half-visible. Caption: "Then play it again."
4. Accessibility mode comparison panel.

---

## Appendix A: Image Pack Catalogue (v1.0)

| Pack ID | Pack Name | Theme | Hero Image Description |
|---|---|---|---|
| P001 | *Coastal Solitude* | Seascape photography | Long-exposure sea fog over dark volcanic rocks |
| P002 | *Deep Space* | Astrophotography / NASA open licence | Pillars of Creation — vivid nebula detail |
| P003 | *Botanical Hour* | Macro flower/plant photography | Dew-covered spider web, backlit at sunrise |
| P004 | *Urban Geometry* | Architecture / symmetry photography | Spiralling staircase from below, Dubai or Bilbao |

*All images must be: licensed for commercial use (own, CC0, or licensed stock), minimum 4K native resolution, suitable for 9×9 grid segmentation with visually distinct sub-regions.*

---

## Appendix B: Shape Reference Sheet

| Slot | Shape | Silhouette Notes |
|---|---|---|
| 1 | Circle | Perfect round; slight bevelled edge in 3D render |
| 2 | Star | 5-pointed, chunky; not thin-spoked |
| 3 | Hexagon | Flat-top orientation |
| 4 | Crescent | Clear horns; not ambiguous with ring |
| 5 | Diamond | Rotated square; tall aspect ratio |
| 6 | Plus | Equal arm lengths; rounded ends |
| 7 | Heart | Symmetrical; clearly legible at 48×48px (minimum legibility target) |
| 8 | Ring | Thick annulus; clear centre hole distinguishing from Circle |
| 9 | Triangle | Equilateral; point-up orientation |

*All shapes must be legible at minimum tile render size (48×48dp) in both full-colour and Symbol-Only mode.*

---

*Document owner: [Project Lead]*
*Last updated: May 2026*
*Next review: Pre-alpha milestone*

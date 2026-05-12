# Chroma-Logic Gallery — Design Session Debrief
**Date:** May 2026 | **Status:** Pre-Development Handoff Reference

---

## SECTION 1 — KEEP: Confirmed Strong Decisions

### Core Identity & Language
- "The Gallery" / "THE GALLERY" as the institutional header — sets tone immediately
- Full vocabulary system: Vessels, Sequences, Fragments, Revelation, Curator, Sanctuary, Designated, Authenticated, Resonant, Logic Points, Insight — coherent and distinctive; must be preserved as a glossary
- "Revelation" as the time metric replacing "hours played" — single best reframe in the product
- Subtitle pairs for each tier (e.g. Grandmaster / *The Logic Sage*) — emotional + institutional in one line
- "A selection of precision-cut logic vessels for the modern curator" — keep verbatim

### Onboarding
- Four-screen sequence: The First Vessel → Chromatic Harmony → The Quiet Gallery → Curator's Welcome
- Simplified 6×6 tutorial grid rather than a full 9×9 — correct difficulty entry point
- "Logic dictates the flow. Color reveals the path" — perfect two-sentence mechanic explanation, keep verbatim
- The Visual Flash shown during onboarding before it's earned — correct decision, players need to see the destination
- QUICK START vs BROWSE GALLERY as exit bifurcation — serves both player types correctly
- "EST. 2024" footer on first screen — trust and permanence signal

### Gemstone Tile System
- Infinity swap replacing Circle — resolves Ring/Circle ambiguity completely
- Final nine: Infinity, Star, Hexagon, Crescent, Diamond, Plus, Heart, Ring, Triangle — no legibility conflicts at tile scale
- Gold outline engraved style on dark cards — reads as material object not screen graphic
- Botanical preview icons (sprout, leaf, seed head) with green tint against dark leaf background — correct aesthetic tease for premium tier

### Gameplay Mechanics
- Dual-axis constraint (shape AND colour) — doubles puzzle depth without additional rules
- Three-dot Safety Net indicator — communicates lives without aggressive gamification language
- "Clarity —" prefix for all difficulty names (Sunlit / Overcast / Moonlit / Void) — anxiety-free framing
- Ascending pentatonic chime build across row/column completions — harmonic progression toward reveal
- Amber wobble for error state, never red — consistent with anxiety-removal philosophy
- Visual Flash on 3×3 box, row, and column completion — escalating anticipation correctly structured

### Jigsaw Mode
- Difficulty naming: Meditative / Focused / Challenging / Grand Master — superior to plain numbers, keep
- Scatter-full board with rotation as confirmed mechanic
- Physics-driven cascade scatter animation for 48-piece; slow drift for 96-piece — mood differentiation correct
- 2-second full-image flash before pieces scatter — memory aid and psychological hook
- Hidden animation reward on jigsaw completion — living image as undisclosed feature, no UI hint
- Animation native to each pack's world (drifting fog, pulsing stars, trembling dew) — correct approach

### Gallery & Collection
- "Logic: Purity" / "Logic: Symmetry" / "Logic: Symmetry" metadata tags on gallery cards — implied variety without rule changes
- Favourite star + solved date on each card — personal diary quality, not completion checklist
- Pack progress counter (e.g. 11/12 Masterpieces) — passive completion hook, correct placement
- Grand Masterpiece panoramic image unlocked on full pack completion — collection arc payoff

### Dashboard
- "85% Revealed · Logic Fragment 12/14" as progress metadata — tells position without imperative language
- "CONTINUE SEQUENCE" as return CTA — narrative framing, not task framing
- Curator's Ledger: Gems Collected + Logic Points + Rank as lifetime identity stats
- Active Sequences horizontal scroll with partially visible second card — signals scrollability correctly

### Daily Meditation (Curator's Sanctuary)
- "Find clarity in the rhythm of logic" — correct tone
- "The pattern follows the breath. Inhale on placement, exhale on revelation" — best line of copy in the project, keep verbatim
- 4×4 grid for meditation mode — completable in 3–5 minutes, correct daily ritual length
- Streak displayed as subdued weekly dot-calendar, not countdown timer — no pressure
- Golden smoke texture as background — most atmospheric treatment in the design set

### Curator Profile & Archive
- "128h Revelation" framing — investment not consumption
- Achievement Gallery teased with four badges + VIEW ARCHIVE — restrained, correct
- "The Archive Grows — You have successfully reconstructed 4 of the 120 known Logic Vessels. Your precision as a curator is noted." — keep verbatim, defines a completable universe
- "Authenticated Collection" header for Archive — institutional weight
- Named puzzles with completion dates as archive entries — personal history quality

### Progression: Path of Prestige
- Six tiers: Neophyte → Scholar → Architect → Master → Grandmaster → Arch-Curator of the Void
- Each tier has unique name + subtitle + a single meaningful unlock reward
- Vertical scroll with connecting line — journey not list
- "Neophyte — The Awakening: Entrance into the sanctuary of logic. You begin to see the hidden geometries." — strongest tier description, should be first thing new players read on this screen
- Unlock escalation: sensory → visual → prestige cosmetic → gameplay expansion → aesthetic transformation

### Monetisation
- Zen Pass screen: frosted glass card, three specific checkmark benefits, "One-time $4.99" pill, "Your purchase supports the creation of new logic galleries" footer — ready as designed
- Rewarded ads framed as opt-in always, never blocking
- Safety Net: "Watch to restore your focus" — anxiety-free framing, correct

### Curator Showcase (formerly Rankings)
- "Curator Showcase: The Resonant Elite" — exhibition not competition
- "Your Contribution to the Collective — 42nd Curator" — additive framing of positional data
- Profile cards with Notable Vessels strip + personal reflection — story per card, not number per row
- Resonate / Observe replacing Like / Share — changes psychological register of interaction
- Weighted showcase algorithm: consistency + engagement depth + collection breadth + recency + social reciprocity — whole curator philosophy
- Tier-scoped random selection (top 3 per tier, randomised display) — invisible in both directions, no UI disclosure

### Accessibility
- "Aesthetic Calibration" screen name — reframes accessibility as preference
- "Designed for curators who seek clarity beyond the spectrum of color" — inclusive without condescension
- Explicit naming of deuteranopia, protanopia, tritanopia in Curator's Note — genuine care signal
- Component Tuning sliders (Glass Opacity / Symbol Glow Intensity / Refraction Depth) — self-directed accessibility without the label
- Nav bar icons shifting to outline-only glyphs in Symbol-Only mode — correct system-wide continuity

### Technical
- Unity 6 Personal tier — free to $200k revenue, splash screen removal, correct engine choice
- Build on Unity 6 specifically for mobile rendering pipeline improvements
- Asset bundles per pack — OTA delivery, no app update required for new content

---

## SECTION 2 — CHANGE: Flagged Issues Requiring Action

### Critical — Must Resolve Before Development Handoff

**1. Curator portrait — replace entirely**
All photorealistic AI-generated human portraits used as named characters (Lead Curator in onboarding, Aurelius Vance / Elena Thorne / Julian Kross in Showcase) must be replaced before any public build. Legal exposure around likeness rights is active in 2025/2026. Replace with hand-illustrated portraits in engraving or chiaroscuro style — more tonally consistent with the gallery aesthetic and legally clean.

**2. Navigation structure — resolve to single system**
Three different nav configurations appear across screens:
- Gallery / Solve / Canvas / Archive (early screens)
- Gallery / Solve / Collection / Profile (dashboard)
- Gallery / Meditate / Archive / Settings (Sanctuary screens)

Must be resolved to one consistent four-tab structure before developer handoff. Recommended: **Gallery / Solve / Meditate / Archive** with Settings accessible via hamburger or profile icon. Daily Canvas entry point must be confirmed — it's a key retention driver and cannot be buried.

**3. Ring vs Circle — tile legibility audit required**
Even with the Infinity swap resolving the primary conflict, Ring remains potentially ambiguous at minimum tile render size (48×48dp). Specifically test Ring vs Circle distinction on a mid-range Android device (Snapdragon 6 series) at actual grid scale. If they collapse, Ring needs a thicker annulus with proportionally larger centre hole.

**4. Symbol-Only mode — X glyph replacement**
The X symbol in the accessibility preview grid carries strong error-state connotations in mobile UI. Players may instinctively read a grid cell containing X as "wrong placement." Replace with a double-bar ≡, lozenge ◇, or asterisk ✳. Also expand preview from 6 to all 9 symbols in a 3×3 arrangement so players can verify legibility for their specific vision profile.

**5. Teardrop geometry unlock — clarify scope**
Architect tier unlocks "Teardrop gemstone geometry." If this introduces a tenth active tile shape into puzzles, the 9×9 grid constraint system breaks — a 9×9 Sudoku requires exactly nine values. Confirm whether Teardrop is: (a) a purely cosmetic skin replacing one of the nine existing tile silhouettes visually, or (b) a new shape that changes puzzle logic. If (b), specify how the grid accommodates it before committing to the design.

### Important — Resolve Before v1.0 Launch

**6. "Manage Membership" placement**
Currently sits at the bottom of the Curator Profile beneath prestige content. A Grandmaster with 128h Revelation should not have a transactional CTA as the last element on their profile screen. Move to Settings or Account screen.

**7. Mint tile colour vs teal photography conflict**
The mint green (`#7ECBA1`) secondary/tertiary UI colour is tonally close to the teal in the Coastal Solitude butterfly photography. During Visual Flash reveals, a mint-coloured tile over that image could blur the boundary between tile and revealed image. Test specifically with the Coastal Solitude image active.

**8. Daily Canvas image tease**
The Today's Canvas card currently shows a generic grid icon with SOLVE button. Replace with a blurred or silhouetted hint of the day's hidden image. The image tease is the core emotional hook of the entire game — even 5% visibility would significantly strengthen the pull to engage.

**9. Resonance count visibility**
Define whether Resonate interactions show a public count. A visible tally could become a vanity metric that undermines the "contribution not competition" philosophy. Consider making resonances private — sender and recipient know, no public display.

**10. Social layer scoping**
The Shared Ledger (community feed, Invite to Solve, Export Essence) requires backend infrastructure: user accounts, feed API, content moderation, social graph. This meaningfully increases development scope. Recommend flagging as v1.5 or v2.0, with infrastructure planned from the start but not blocking launch. The core game is strong enough to ship without it.

---

## SECTION 3 — OPEN QUESTIONS: Unresolved by Design Work

### Gameplay Systems

**Q1. Curator IV badge — what is the tier system?**
"CURATOR IV" appears as a badge on the reveal screen. Is this the same as the Path of Prestige rank system, or a separate curator progression? If separate, what are the tiers, how many exist, and what differentiates IV from III or V?

**Q2. Sequential Logic / Purity / Symmetry — taxonomy definition**
Gallery cards show descriptors like "Logic: Purity," "Logic: Symmetry," and "Tier III · Sequential Logic." Are these: (a) purely flavour names with no gameplay meaning, (b) descriptors of actual solution pattern properties, or (c) a separate difficulty taxonomy from the Clarity tier system? All three are valid choices but the developer needs to know which one.

**Q3. 4×4 Meditation grid — which four shapes and colours?**
The meditation mode uses a 4×4 grid. Standard 4×4 Sudoku requires exactly four values per axis. Which four of the nine tile shapes are used? Which four colours? Is the selection fixed, or does it rotate per puzzle? Same question for the logic engine — is this the same solver with different parameters, or a separate implementation?

**Q4. PULSE toggle — what does it do exactly?**
"Rhythmic Pulse" appears as a toggleable preference in Curator's Sanctuary settings. Define the specific behaviour: (a) tiles pulse in rhythm with the ambient soundtrack, (b) haptic pulse on correct placements, (c) background particle system pulses, or (d) something else. The name creates expectation — the implementation must match it.

**Q5. Curator Insights toggle — what content does it surface?**
"Curator Insights" is on by default. Define what this shows: photography metadata (location, photographer, technique), puzzle logic analysis, collection history commentary, or something else. If it includes photography information, this has content production implications — someone must write insights for every image in every pack.

**Q6. "Constructs" archive filter — what is this category?**
The Archive shows filter tabs: All Vessels / Gemstones / Constructs. "Constructs" hasn't appeared elsewhere. Does this refer to: (a) completed jigsaw assemblies as distinct from sudoku-solved images, (b) a second category of puzzle type beyond the gemstone tile system, or (c) something else? Needs a glossary definition.

**Q7. Reflections — user-generated or AI-generated?**
The Shared Ledger feed shows personal reflections attributed to named curators. Are these: (a) written by players at a post-solve prompt, (b) AI-generated using the puzzle name and player stats as inputs, or (c) pre-written by the studio per puzzle? Each has significantly different technical and content moderation implications. If user-generated, a text input moment needs to be added to the post-reveal flow, with character limit, moderation infrastructure, and content policy.

**Q8. "Export Essence" — what does it produce?**
The social sharing panel includes "EXPORT ESSENCE" as a secondary CTA alongside "INVITE TO SOLVE." Define the specific output: a composed shareable image (vessel artwork + curator seal + aura treatment + reflection text), a video loop of the animated completed jigsaw, raw image export, or something else.

**Q9. Invite to Solve — deep link destination**
When an invited player taps a shared invitation, where do they land? Option A: directly into the specific puzzle. Option B: full onboarding flow with the shared puzzle queued as the first solve after completion. Option B is strongly recommended — every invited player should experience the mythology of The First Vessel before reaching the shared puzzle — but this needs to be explicitly specified.

**Q10. "Eternal" vs "Arch-Curator of the Void" — progress bar label**
The Path of Prestige progress bar reads "2,000 for Eternal" but the tier is named "Arch-Curator of the Void." Is "Eternal" an intentional shorthand for the progress bar context, or a truncation that needs resolving? If intentional, confirm it's used consistently across all references to this tier.

### Content & Production

**Q11. Named landmark puzzles — which vessels are designated?**
The Earned Designation "Ethereal Visionary" is unlocked "after completing 'The Zenith' puzzle," implying certain puzzles are designated landmark challenges. How many named landmark puzzles exist in v1.0? How are they distinguished in the UI? Does the developer need to flag specific puzzles during content integration, or does the studio assign landmark status post-launch?

**Q12. Notable Vessels thumbnails in Showcase cards**
Currently rendered as empty dark squares. When populated, should these show: (a) the completed jigsaw version carrying the player's labour, (b) the raw revealed photograph, or (c) the sudoku grid at completion moment? The first option is recommended as it shows the player's specific achievement.

**Q13. Animated image asset pipeline**
The hidden living image reward on jigsaw completion requires per-image animation. Confirmed approach: animations are optional metadata per image, sparse at launch (suggested 4 of 12 in v1.0 packs), scaling with asset library. Questions: Who produces these animations? What format (Spine, Lottie, Unity timeline, video loop)? What is the file size budget per animated image? What is the minimum loop length?

**Q14. Pack image licensing**
All images must be licensed for commercial use at minimum 4K native resolution. Confirm for each v1.0 pack: own photography, CC0, or licensed stock? If licensed stock, which agency, and does the licence cover the "Souvenir" wallpaper download feature (which constitutes redistribution to end users)?

### Technical Architecture

**Q15. User account system — required at launch?**
The Shared Ledger, Curator Showcase weighting algorithm, streak tracking, and cross-device collection sync all require a user account backend. If the social features are deferred to v1.5, confirm which features still require account infrastructure at v1.0 — specifically whether streak and collection data is stored locally only or server-synced from day one.

**Q16. Infinity tile orientation**
The Infinity shape has inherent directional ambiguity — horizontal and vertical orientations look identical. Two options: (a) lock all Infinity tiles to the same orientation in the grid, treating it as directionless, or (b) allow rotation and make orientation part of the constraint system for higher difficulty tiers. Option (b) adds meaningful depth to Moonlit and Void tiers. Decision needed before the logic engine is built.

**Q17. Showcase algorithm — what triggers recalculation?**
The weighted curator showcase algorithm combines streak, engagement depth, collection breadth, recency, and social reciprocity. How frequently does the composite score recalculate? On session open, daily, or on a background schedule? What is the selection pool size per tier at launch with a small player base?

---

## Summary Counts

| Category | Count |
|---|---|
| Keep (confirmed decisions) | 63 items |
| Change — Critical (pre-handoff) | 5 items |
| Change — Important (pre-launch) | 5 items |
| Open Questions | 17 questions |

---

*Document compiled from full Stitch design session, May 2026*
*To be read alongside: Master Brief v1.1 and Terminology Glossary (to be created)*

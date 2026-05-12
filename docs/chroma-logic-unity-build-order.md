# Chroma-Logic Gallery — Unity Scene Build Order
**Version:** 1.0 | **Status:** Development Reference
**Purpose:** Prioritised build sequence mapping every Stitch-designed screen to a Unity scene or prefab, ordered by dependency. Never build a UI screen before its underlying system exists.

---

## The Golden Rule
```
Logic first → Data second → UI last
Never build a screen before its underlying system is tested and working.
```

---

## PHASE 0 — Project Foundation
*No scenes yet. Pure setup.*

| Task | Type | Notes |
|---|---|---|
| Android build target | Editor Setting | File → Build Settings → Android → Switch Platform |
| Bundle ID | Player Settings | `com.yourname.chromalogic` |
| URP asset configuration | Settings | Adjust quality tiers for mobile — disable SSAO, shadows on low tier |
| TextMesh Pro import | Package | Window → Package Manager → TMP Essential Resources |
| DOTween install | Package | Import from Asset Store, run setup panel |
| Addressables install | Package | Window → Package Manager → Addressables |
| Unity Gaming Services | Package | Window → Package Manager → UGS |
| Google Mobile Ads | Package | Import from Google's Unity plugin page |
| Input System | Package | Enable new Input System, restart editor |
| Colour palette material | Asset | Create a global colour palette ScriptableObject with all hex values |
| Font import | Asset | Import Cormorant Garamond, DM Sans, Hanken Grotesk as TMP font assets |

**Exit criteria:** Project builds to Android APK with a blank scene. No errors.

---

## PHASE 1 — Core Logic Systems
*No UI. Pure C# logic tested in editor only.*

### 1.1 Sudoku Engine
**File:** `Assets/_Project/Scripts/Core/SudokuSolver.cs`

The entire game depends on this. Build and test it completely before touching anything else.

```
Requirements:
- Generate valid 9x9 Sudoku puzzles
- Support four difficulty tiers by pre-filled cell count:
  Sunlit (38-45), Overcast (28-35), Moonlit (22-27), Void (17-21)
- Validate placement in real time (row + column + box)
- Dual-axis: validate both Shape AND Colour independently
- Return solution for hint system (The Insight)
- Expose: IsValidPlacement(row, col, shape, colour)
- Expose: GeneratePuzzle(difficulty)
- Expose: GetHint() → returns one correct cell
```

**Test in:** Unity Test Runner (Edit Mode tests)
Write unit tests for every public method before moving on.

---

### 1.2 Grid Data Model
**File:** `Assets/_Project/Scripts/Core/GridData.cs`

```
Requirements:
- 9x9 Cell array
- Each Cell holds: ShapeType, ColourType, IsLocked, IsRevealed, IsSolved
- ShapeType enum: Infinity, Star, Hexagon, Crescent, Diamond, Plus, Heart, Ring, Triangle
- ColourType enum: Crimson, Amber, Jade, Cobalt, Violet, Teal, Rose, Slate, Gold
- Error count tracking (max 3 before Safety Net trigger)
- Completion detection per row, column, box, and full grid
- Events: OnBoxComplete, OnRowComplete, OnColumnComplete, OnGridComplete
```

---

### 1.3 Meditation Grid Engine
**File:** `Assets/_Project/Scripts/Core/MeditationSolver.cs`

```
Requirements:
- 4x4 Sudoku variant
- Uses subset of 4 shapes and 4 colours (define fixed subset — see Open Question Q3)
- Same solver architecture as SudokuSolver, different parameters
- Confirm: same solver with parameters OR separate implementation
```

---

### 1.4 Jigsaw Engine
**File:** `Assets/_Project/Scripts/Core/JigsawManager.cs`

```
Requirements:
- Piece count variants: 12, 24, 48, 96
- Piece generation from source image (slice into irregular interlocking shapes)
- Piece state: position, rotation, isPlaced
- Snap detection with variable radius (generous on 12/24, tighter on 48/96)
- Rotation: long-press + drag
- Completion detection
- Events: OnPieceSnapped, OnJigsawComplete
```

---

### 1.5 Player Data Manager
**File:** `Assets/_Project/Scripts/Managers/PlayerDataManager.cs`

```
Requirements:
- Curator rank (Neophyte → Arch-Curator of the Void)
- Logic Points (int)
- Gems Collected (int)
- Revelation time (float, seconds → displayed as hours)
- Gallery XP (int)
- Streak data (last played date, current streak int)
- Completed vessel IDs (List<string>)
- Unlocked pack IDs (List<string>)
- Active atmosphere setting
- Accessibility settings (symbol-only, high contrast, haptic levels)
- Persist via Unity Gaming Services Cloud Save
- Local fallback via PlayerPrefs for offline
```

---

### 1.6 Progression Manager
**File:** `Assets/_Project/Scripts/Managers/ProgressionManager.cs`

```
Requirements:
- Rank promotion logic (Logic Points thresholds per tier)
- Designation award logic (check conditions on session end)
- Gallery XP → reward unlock mapping
- Pack completion detection → Grand Masterpiece unlock
- Events: OnRankPromoted, OnDesignationEarned, OnPackCompleted
```

**Exit criteria for Phase 1:** All systems pass unit tests. No Unity scenes created yet.

---

## PHASE 2 — Core Scene Infrastructure
*First actual Unity scenes.*

### 2.1 Bootstrap Scene
**Scene:** `Assets/_Project/Scenes/Bootstrap.unity`

```
Purpose: First scene loaded. Initialises all managers, checks save data,
         routes to correct scene (Onboarding if new, Dashboard if returning).

GameObjects:
- GameManager (singleton, DontDestroyOnLoad)
- PlayerDataManager
- ProgressionManager
- AudioManager
- AddressablesLoader
- UGS Initialiser

Exit criteria: Routes correctly to Onboarding vs Dashboard based on save state.
```

---

### 2.2 Persistent UI Canvas
**Prefab:** `Assets/_Project/Prefabs/UI/PersistentCanvas.prefab`

```
Purpose: Navigation bar and global overlay elements that persist across scenes.

Components:
- Bottom navigation bar (Gallery / Solve / Meditate / Archive)
- Global loading overlay
- Toast notification system
- Accessibility button (top corner, always visible)

Note: DontDestroyOnLoad. All scenes reference this prefab.
```

---

## PHASE 3 — Onboarding Flow
*Four screens. Implement in sequence — each depends on the previous.*

### 3.1 Onboarding Scene
**Scene:** `Assets/_Project/Scenes/Onboarding.unity`

Build order within this scene:

| Screen | Stitch Reference | Key Components |
|---|---|---|
| **The First Vessel** | Onboarding screen 1 | Animated Infinity gem, "BEGIN REVEAL" CTA, EST. 2024 footer |
| **Chromatic Harmony** | Onboarding screen 2 | 2×2 tile demo, "Logic dictates the flow. Color reveals the path." |
| **The Quiet Gallery** | Onboarding screen 3 | Simplified 6×6 grid, Visual Flash demo, hidden image tease |
| **Curator's Welcome** | Onboarding screen 4 | Illustrated Curator portrait, Novice Curator rank, QUICK START / BROWSE GALLERY |

```
Notes:
- 6×6 grid only — not full 9×9
- Incorrect cells locked — no mistakes possible
- Visual Flash MUST trigger on first 3×3 box completion
- Full reveal animation plays on completion — no skip
- Routes to Dashboard on QUICK START
- Routes to Gallery on BROWSE GALLERY
- Never shown again after completion — save flag in PlayerDataManager
```

---

## PHASE 4 — Sudoku Gameplay Scene
*The most complex scene. Core revenue driver.*

### 4.1 Solve Scene
**Scene:** `Assets/_Project/Scenes/Solve.unity`

Build order within this scene:

**Step 1 — Grid renderer**
```
- 9×9 grid of Cell prefabs
- Each cell renders ShapeType + ColourType as 3D gemstone tile
- Empty cells show subtle placeholder glow
- Locked (pre-filled) cells visually distinct from player-placed cells
```

**Step 2 — Tile tray**
```
- Bottom tray showing available tiles
- Drag-and-drop placement (primary)
- Tap-to-select + tap-to-place (accessibility fallback)
- Currently selected tile highlighted
```

**Step 3 — Feedback systems**
```
- Amber wobble + low haptic pulse on error
- Safety Net dots (3 dots, top right) — one dims per error
- Glass "clink" SFX + 12ms vibration on valid placement
- Ascending pentatonic chime on row/column/box completion
```

**Step 4 — Visual Flash mechanic**
```
- On 3×3 box completion: tiles turn frosted-transparent for 2.5 seconds
- On row/column completion: tiles illuminate underlying image briefly
- Image loads progressively — only fully loaded at 80% completion
- Requires: Addressables image loaded for current vessel
```

**Step 5 — Safety Net integration**
```
- After 3rd error: show Safety Net rewarded ad prompt
- "Watch to restore your focus"
- On ad complete: reset error count to 0
- Max once per puzzle session
```

**Step 6 — Hint system (The Insight)**
```
- Hint button always visible
- On tap: show rewarded ad prompt
- On ad complete: reveal one correct cell
- Never auto-placed — player must confirm placement
```

**Step 7 — Header**
```
- Clarity level display (e.g. "Clarity — Sunlit")
- Safety Net dots
- Pack name
- Back button (with confirmation if puzzle in progress)
```

**Exit criteria:** Full puzzle completable from start to finish with all feedback systems working.

---

## PHASE 5 — Phase B: The Reveal
*Triggered from Solve scene on grid completion.*

### 5.1 Reveal Sequence
**Component:** `Assets/_Project/Scripts/Gameplay/RevealSequence.cs`

```
Sequence (all timed, no skips):
1. 1.5s crystallise pause — grid vibrates gently (DOTween shake)
2. Tile shatter — physics particle burst, shards retain gem colour
3. Image bloom — top-to-bottom reveal over 2.5 seconds (DOTween)
4. Cinematic ambient swell plays (reserved for this moment only)
5. Reveal screen UI appears:
   - Image full screen
   - Vessel name + pack name
   - Completion date + Clarity level
   - "SHARE MOMENT" button
   - "NEXT SOLVE →" button
   - Souvenir rewarded ad prompt (once per image)

Post-reveal:
- Save completed vessel to PlayerDataManager
- Award Logic Points
- Check for rank promotion
- Check for pack completion → Grand Masterpiece unlock
- Check for designation awards
```

---

## PHASE 6 — Gallery & Dashboard

### 6.1 Gallery Dashboard Scene
**Scene:** `Assets/_Project/Scenes/Gallery.unity`

| Component | Stitch Reference | Notes |
|---|---|---|
| Featured Vessel card | Main Gallery Dashboard | Shows active vessel, % revealed, Fragment count, CONTINUE SEQUENCE |
| Active Sequences scroll | Main Gallery Dashboard | Horizontal scroll, partially visible second card |
| Curator's Ledger | Main Gallery Dashboard | Logic Points, Gems Collected, Rank |
| Daily Canvas card | Daily Canvas screen | Blurred image hint, streak dot calendar |

---

### 6.2 Archive Scene
**Scene:** `Assets/_Project/Scenes/Archive.unity`

```
Filter tabs: All Vessels / Gemstones / Constructs
Each card shows: vessel image, pack name, completion date, Logic descriptor, favourite star
Tap card → routes to Jigsaw mode for that vessel
```

---

## PHASE 7 — Jigsaw Mode

### 7.1 Jigsaw Scene
**Scene:** `Assets/_Project/Scenes/Jigsaw.unity`

Build order:

**Step 1 — Difficulty selection screen**
```
Four cards: Meditative (12) / Focused (24) / Challenging (48) / Grand Master (96)
Grand Master locked until Gallery XP Level 10
Tap → difficulty confirmed → scatter animation begins
```

**Step 2 — Scatter animation**
```
48-piece: physics cascade scatter (DOTween + Rigidbody2D)
96-piece: slow drift fade-in
Both: 2-second full-image flash before scatter
Skippable after 2 seconds via tap-hold
```

**Step 3 — Jigsaw board**
```
Full scatter board — all pieces visible simultaneously
Two-finger pinch-zoom
Long-press + drag for piece rotation
Snap on release if within radius
Generous snap radius on 12/24, tighter on 48/96
```

**Step 4 — Living Image reveal**
```
On completion: jigsaw assembles fully → Living Image animation triggers
Animation loops 3-6 seconds, seamless
No UI announcement — undisclosed feature
Share button appears after 3 seconds
```

---

## PHASE 8 — Curator Profile & Progression

### 8.1 Profile Scene
**Scene:** `Assets/_Project/Scenes/Profile.unity`

| Component | Stitch Reference | Notes |
|---|---|---|
| Curator portrait | Curator Profile screen | Illustrated, not photorealistic |
| Grandmaster rank badge | Curator Profile screen | Dynamic — reflects current rank |
| Logic Points + Gems + Revelation | Curator's Ledger | Lifetime stats |
| Achievement Gallery | Curator Profile screen | 4 badges + VIEW ARCHIVE |
| Earned Designations | Designations screen | List with earn conditions |
| Path of Prestige | Prestige screen | Vertical scroll, illuminated past tiers |

---

## PHASE 9 — Daily Meditation

### 9.1 Meditation Scene
**Scene:** `Assets/_Project/Scenes/Meditation.unity`

```
Landing screen: Curator's Sanctuary, vessel name, "BEGIN MEDITATION"
Active screen: 4×4 grid, golden smoke background texture
Copy: "The pattern follows the breath. Inhale on placement, exhale on revelation."
Step counter: STEP 04 / 12
On completion: standard reveal sequence (smaller scale)
Streak update on completion
```

---

## PHASE 10 — Settings & Accessibility

### 10.1 Aesthetic Calibration Scene
**Scene:** `Assets/_Project/Scenes/Settings.unity`

```
Screen name: "Aesthetic Calibration"
Sections:
- Preferences: Haptic Feedback toggle, PULSE toggle, Curator Insights toggle
- Aesthetic Settings: Active Atmosphere selector, MODIFY PALETTE
- Component Tuning: Glass Opacity slider (82%), Symbol Glow Intensity (45%), Refraction Depth (34px)
- Symbol-Only Mode toggle with live 3×3 preview of all 9 symbols
- Account & Legal: Privacy Policy, Terms of Service, LOG OUT

Note: Accessible from first session via persistent button — not buried.
```

---

## PHASE 11 — Monetisation Integration

| Feature | Implementation Order | Notes |
|---|---|---|
| Rewarded Ads (Safety Net) | After Phase 4 complete | Test with Google test ad unit IDs first |
| Rewarded Ads (Insight) | After Phase 4 complete | Same ad manager |
| Rewarded Ads (Souvenir) | After Phase 5 complete | Triggered from Reveal screen |
| Interstitial (Quick-Flip) | After Phase 6 complete | Between levels only — implement suppression rules carefully |
| Banner (Gallery) | After Phase 6 complete | Bottom rail, Gallery scene only |
| Zen Pass IAP | After all ads working | One-time purchase, suppresses all ads |
| Pack Unlock IAP | After Addressables working | $1.99 per pack |

---

## PHASE 12 — Gemstone Collection & Premium Tiles

### 12.1 Gemstone Collection Screen
**Scene/Panel:** Accessible from Profile or Settings

```
Default set: Quiet Luxury (gold outline, 9 tiles)
Premium sets: Botanical, Architectural, Celestial (Zen Pass unlock)
Preview tiles at 2× size minimum
"ACTIVE SET" badge on current selection
```

---

## PHASE 13 — Curator Showcase (v1.5)

*Defer until social backend on Elysium is built.*

```
Curator Showcase: The Resonant Elite
Weighted algorithm: consistency + engagement + collection + recency + reciprocity
Tier-scoped random selection (top 3 per tier, randomised display)
Resonate / Observe actions (private count)
```

---

## PHASE 14 — Shared Ledger (v1.5)

*Defer until social backend on Elysium is built.*

```
Community feed
Reflections (define: user-generated vs AI-assisted — resolve Open Question Q7)
Invite to Solve with Recipient Aura (Celestial / Nebula / Ether)
Export Essence (resolve Open Question Q8)
```

---

## Analytics — Instrument Throughout

Add these events at the point each system is built, not retrofitted at the end:

```csharp
// Phase 4
Analytics.puzzle_started
Analytics.puzzle_abandoned

// Phase 5
Analytics.puzzle_completed
Analytics.phase_b_reveal_watched (duration)

// Phase 6
Analytics.gallery_opened
Analytics.daily_canvas_claimed
Analytics.streak_broken

// Phase 7
Analytics.jigsaw_started
Analytics.jigsaw_completed

// Phase 9
Analytics.meditation_started
Analytics.meditation_completed

// Monetisation
Analytics.ad_shown
Analytics.ad_completed
Analytics.ad_skipped
Analytics.iap_prompt_shown
Analytics.iap_completed

// Social
Analytics.share_tapped
Analytics.share_completed

// Progression
Analytics.rank_promoted
```

---

## Build Milestone Checkpoints

| Milestone | Phases Complete | What It Proves |
|---|---|---|
| **Alpha 1** | 0–4 | Sudoku fully playable, no UI polish |
| **Alpha 2** | 0–6 | Full core loop: Solve → Reveal → Gallery |
| **Beta 1** | 0–10 | All screens built, monetisation integrated |
| **Beta 2** | 0–12 | Full content pipeline, Addressables live from Elysium |
| **RC1** | 0–12 + analytics | Release candidate, ready for internal testing |
| **v1.0 Launch** | 0–12 | Google Play submission |
| **v1.5** | 0–14 | Social layer live |

---

*Document owner: Jason Mercer*
*Last updated: May 2026*
*Read alongside: Master Brief v1.1, Design Session Debrief, Terminology Glossary*
*Stitch design session screens are the visual reference for all UI phases.*

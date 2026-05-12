# CLAUDE.md — Chroma-Logic Gallery
# Read this file at the start of every session. Do not drift from these conventions.

---

## Project Identity
- **Name:** Chroma-Logic Gallery
- **Type:** Mobile puzzle game — Android (Google Play), iOS Phase 2
- **Owner:** Jason Mercer
- **Repository:** GitHub (EUT-Lab organisation)
- **Status:** Active development

---

## The Game — One Paragraph
A premium mobile puzzle game where players solve a 9x9 Sudoku grid using nine gemstone shapes and nine colours simultaneously. Beneath the grid, a hidden high-resolution masterpiece image is revealed progressively as rows, columns, and boxes are completed. On full completion the grid shatters and the image blooms. The revealed image is then playable as a jigsaw puzzle. The game world is called The Gallery. Players are Curators. The tone is calm, prestigious, anxiety-free.

---

## Stack — Never Deviate From This
Engine:         Unity 6 (6000.x LTS)
Pipeline:       Universal Render Pipeline (URP)
Language:       C# — strict conventions (see below)
Scripting:      IL2CPP backend
UI:             UI Toolkit + TextMesh Pro
Animation:      DOTween Pro
Input:          Unity Input System (new — never legacy)
Assets OTA:     Unity Addressables
Remote URL:     https://chromalogic.aegisnet.org.uk/bundles/
Audio:          Unity Audio Mixer
Ads:            Google Mobile Ads Unity Plugin
IAP:            Unity IAP
Backend v1.0:   Unity Gaming Services (UGS)
Backend v1.5:   Node.js + PostgreSQL on Elysium VPS
Build:          Unity Cloud Build → APK/AAB
Source:         GitHub
Platform:       Android 26+ (ARM64)
Target:         60fps stable, Snapdragon 6 series minimum

---

## Infrastructure Rule
Build it     → Laptop (Unity 6, Windows)
Serve it     → Elysium (/mnt/sanctum/chromalogic/bundles/)
Store source → GitHub
Ship it      → Unity Cloud Build

Nothing Unity-related ever touches Elysium directly. Elysium purely serves built asset bundles.

---

## Core Enums — Never Change These Names

ShapeType:    Infinity, Star, Hexagon, Crescent, Diamond, Plus, Heart, Ring, Triangle
ColourType:   Crimson, Amber, Jade, Cobalt, Violet, Teal, Rose, Slate, Gold
ClarityLevel: Sunlit (38-45 cells), Overcast (28-35), Moonlit (22-27), Void (17-21)
CuratorRank:  Neophyte, Scholar, Architect, Master, Grandmaster, ArchCuratorOfTheVoid
JigsawDiff:   Meditative (12), Focused (24), Challenging (48), GrandMaster (96)

---

## Terminology — Always Use in Code and Comments
The Gallery       → game world (never "the game")
Vessel            → single puzzle unit
Curator           → the player (never "player" in UI strings)
Logic Points      → primary progression currency
Revelation        → time metric (never "hours played")
Visual Flash      → mechanic revealing image on completion
The Reveal        → Phase B full image bloom
Safety Net        → three-error system
The Insight       → hint mechanic
Living Image      → hidden animated jigsaw completion reward
Clarity Level     → difficulty (never "difficulty" in UI strings)

---

## UI Colours
BackgroundBase:  #0D0F1A
AccentGold:      #C8A97E
SuccessMint:     #7ECBA1
AlertAmber:      #D4A853 — NEVER use red for errors

---

## Key Design Rules — Never Violate
1. No timers on any default game mode
2. Never use red for error states — always amber wobble
3. Safety Net = max once per puzzle session
4. Interstitial ads = never on first session, never during Meditation
5. Zen Pass suppresses ALL interstitial and banner ads — check flag before every ad call
6. Living Image = undisclosed feature, zero UI hint
7. Curator portrait = illustrated only, never photorealistic
8. Resonance counts = private, never displayed publicly
9. Difficulty language = always "Clarity — Sunlit/Overcast/Moonlit/Void"

---

## Scene Build Order
Phase 0  → Project setup and packages
Phase 1  → Core C# logic (no UI)
Phase 2  → Bootstrap scene + PersistentCanvas
Phase 3  → Onboarding (4 screens, 6x6 tutorial)
Phase 4  → Solve scene (full Sudoku)
Phase 5  → Reveal sequence
Phase 6  → Gallery Dashboard + Archive
Phase 7  → Jigsaw mode
Phase 8  → Curator Profile + Path of Prestige
Phase 9  → Daily Meditation (4x4 grid)
Phase 10 → Aesthetic Calibration (Settings)
Phase 11 → Monetisation integration
Phase 12 → Gemstone Collection screen
Phase 13 → Curator Showcase (v1.5 — defer)
Phase 14 → Shared Ledger (v1.5 — defer)

---

## Open Questions — Do Not Implement Until Resolved
Q1: Infinity tile orientation — locked or rotatable on Moonlit/Void?
Q2: Meditation 4x4 — which 4 shapes and 4 colours?
Q3: PULSE toggle — exact behaviour?
Q4: Curator Insights — what content?
Q5: Constructs archive filter — what category?
Q6: Reflections — user-generated or AI-generated?
Q7: Export Essence — exact output format?
Q8: Teardrop geometry — cosmetic skin or new puzzle shape?
Q9: Logic descriptors (Purity/Symmetry) — flavour or computed?

---

## What NOT To Do
- Never use legacy Unity Input System
- Never use UnityEvents for game logic (use C# events)
- Never load Addressable assets synchronously
- Never hardcode colour values
- Never use "player", "difficulty", "hours played" in UI strings
- Never show red in error states
- Never implement Phases 13-14 before v1.5
- Never modify the nine ShapeType or ColourType enum values

---

## Reference Documents (in /docs folder)
chroma-logic-gallery-brief.md
chroma-logic-gallery-debrief.md
chroma-logic-terminology-glossary.md
chroma-logic-unity-build-order.md

---

Last updated: May 2026 | Owner: Jason Mercer
This file is the single source of truth for Claude Code sessions on this project.
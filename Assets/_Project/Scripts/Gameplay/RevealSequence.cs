using System;
using System.Collections.Generic;
using ChromaLogic.Core;
using ChromaLogic.Managers;
using ChromaLogic.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.Gameplay
{
    /// <summary>
    /// Phase 5 Reveal Sequence — played when <see cref="GridData.OnGridComplete"/> fires.
    /// <para>
    /// Sequence: 1.5s crystallise vibrate → tile shatter with colour bursts → 2.5s image bloom
    /// → result screen fade-in. Triggered externally via <see cref="Begin"/>; never auto-starts.
    /// </para>
    /// <para>
    /// Public fields (<see cref="VesselName"/>, <see cref="PackName"/>, <see cref="VesselId"/>,
    /// <see cref="PackId"/>, <see cref="PackVesselIds"/>, <see cref="RevealSprite"/>,
    /// <see cref="VesselClarity"/>) must be set by <see cref="SolveController"/> before calling
    /// <see cref="Begin"/>.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class RevealSequence : MonoBehaviour
    {
        // ── Public data fields ────────────────────────────────────────────

        public string       VesselName    = "Untitled Vessel";
        public string       PackName      = "The Collection";
        public string       VesselId      = string.Empty;
        public string       PackId        = string.Empty;
        public List<string> PackVesselIds = new();

        [Tooltip("Assign the Vessel's reveal art in the Inspector or load it via Addressables before calling Begin().")]
        public Sprite RevealSprite;

        public ClarityLevel VesselClarity;

        // ── Events ────────────────────────────────────────────────────────

        /// <summary>Fires when the Curator taps Share Moment.</summary>
        public event Action OnShareRequested;

        /// <summary>Fires when the Curator taps Download as wallpaper (Phase 11 ad hook).</summary>
        public event Action OnSouvenirRequested;

        /// <summary>Fires after the result screen has appeared and PostRevealData has been called.</summary>
        public event Action OnSequenceComplete;

        // ── USS class constants ───────────────────────────────────────────

        private const string ClassShard      = "cell-shard";
        private const string ClassShardPfx   = "cell-shard--";

        // ── Timing constants ──────────────────────────────────────────────

        private const float CrystalliseDuration = 1.5f;
        private const float ShatterSpread        = 0.8f;
        private const float ShatterCellDuration  = 0.2f;
        private const float BloomDuration        = 2.5f;
        private const float ResultFadeDuration   = 0.4f;

        // ── Private state ─────────────────────────────────────────────────

        private UIDocument _document;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Begins the full reveal sequence. Must not be called from Start — triggered
        /// externally by <see cref="SolveController"/> only.
        /// </summary>
        /// <param name="gridRenderer">Used to read per-cell VisualElements during shatter.</param>
        /// <param name="gridData">Used to read per-cell colour for particle bursts.</param>
        /// <param name="solveRoot">Root VisualElement of the Solve UIDocument.</param>
        public void Begin(GridRenderer gridRenderer, GridData gridData, VisualElement solveRoot)
        {
            if (_document == null)
            {
                Debug.LogError("[RevealSequence] UIDocument missing — cannot run sequence.");
                return;
            }

            var revealRoot = _document.rootVisualElement.Q("reveal-root");
            if (revealRoot == null)
            {
                Debug.LogError("[RevealSequence] reveal-root element not found in UIDocument.");
                return;
            }

            var overlayParticles = revealRoot.Q("overlay-particles");
            var solveGrid        = solveRoot.Q("solve-grid");

            StepCrystallise(solveGrid, () =>
                StepShatter(gridRenderer, gridData, overlayParticles, () =>
                    StepBloom(revealRoot, () =>
                        StepResult(revealRoot))));
        }

        // ── Sequence steps ────────────────────────────────────────────────

        private static void StepCrystallise(VisualElement solveGrid, Action onComplete)
        {
            DOVirtual.Float(0f, 1f, CrystalliseDuration, t =>
            {
                float offset = Mathf.Sin(t * Mathf.PI * 18f) * 2.5f;
                solveGrid.style.translate = new StyleTranslate(
                    new Translate(new Length(offset, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel)));
            })
            .OnComplete(() =>
            {
                solveGrid.style.translate = StyleKeyword.Null;
                onComplete?.Invoke();
            });
        }

        private static void StepShatter(
            GridRenderer   gridRenderer,
            GridData       gridData,
            VisualElement  overlayParticles,
            Action         onComplete)
        {
            const int GridSize = 9;

            for (int r = 0; r < GridSize; r++)
            for (int c = 0; c < GridSize; c++)
            {
                var cell  = gridRenderer.GetCellElement(r, c);
                var cData = gridData.Cells[r, c];
                float delay = (r * GridSize + c) / 80f * ShatterSpread;

                DOVirtual.Float(1f, 0f, ShatterCellDuration, v =>
                    cell.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f))))
                .SetDelay(delay)
                .OnStart(() =>
                {
                    if (cData.Colour.HasValue && overlayParticles != null)
                        SpawnCellBurst(overlayParticles, cell, cData.Colour.Value);
                });
            }

            // Last cell finishes at ShatterSpread + ShatterCellDuration after this step starts.
            DOVirtual.DelayedCall(ShatterSpread + ShatterCellDuration, () => onComplete?.Invoke());
        }

        private void StepBloom(VisualElement revealRoot, Action onComplete)
        {
            AudioManager.Instance?.PlayRevealSwell();

            var wrapper = revealRoot.Q("reveal-image-wrapper");
            var image   = revealRoot.Q("reveal-image");

            if (image != null && RevealSprite != null)
                image.style.backgroundImage = new StyleBackground(RevealSprite);

            if (wrapper == null)
            {
                onComplete?.Invoke();
                return;
            }

            // Wipe height from 0 to the full root height.
            float targetHeight = revealRoot.resolvedStyle.height;
            if (targetHeight <= 0f) targetHeight = Screen.height;

            DOVirtual.Float(0f, targetHeight, BloomDuration,
                h => wrapper.style.height = h)
            .OnComplete(() => onComplete?.Invoke());
        }

        private void StepResult(VisualElement revealRoot)
        {
            var screen = revealRoot.Q("reveal-screen");
            if (screen == null) return;

            // Populate labels.
            var vesselLabel = screen.Q<Label>("vessel-name-label");
            if (vesselLabel != null) vesselLabel.text = VesselName;

            var packLabel = screen.Q<Label>("pack-name-label");
            if (packLabel != null) packLabel.text = PackName;

            var dateLabel = screen.Q<Label>("completion-date-label");
            if (dateLabel != null) dateLabel.text = DateTime.Today.ToString("d MMMM yyyy");

            var clarityLabel = screen.Q<Label>("clarity-achieved-label");
            if (clarityLabel != null) clarityLabel.text = VesselClarity.ToString();

            // Wire buttons.
            screen.Q<Button>("btn-share")
                ?.RegisterCallback<ClickEvent>(_ => OnShareRequested?.Invoke());
            screen.Q<Button>("btn-next-solve")
                ?.RegisterCallback<ClickEvent>(_ => GameManager.Instance?.LoadDashboard());
            screen.Q<Button>("btn-souvenir")
                ?.RegisterCallback<ClickEvent>(_ => OnSouvenirRequested?.Invoke());

            // Fade in.
            DOVirtual.Float(0f, 1f, ResultFadeDuration,
                v => screen.style.opacity = v)
            .OnComplete(() =>
            {
                PostRevealData();
                OnSequenceComplete?.Invoke();
            });
        }

        // ── Post-reveal data ──────────────────────────────────────────────

        private void PostRevealData()
        {
            PlayerDataManager.Instance?.CompleteVessel(VesselId);

            if (!string.IsNullOrEmpty(PackId) && PackVesselIds?.Count > 0)
                ProgressionManager.Instance?.CheckPackCompletion(PackId, PackVesselIds);

            ProgressionManager.Instance?.CheckAllDesignations();
        }

        // ── Particle burst helper ─────────────────────────────────────────

        private static void SpawnCellBurst(
            VisualElement overlayParticles,
            VisualElement sourceCell,
            ColourType    colour)
        {
            string colourSuffix = TutorialGridData.AllColourSuffix[(int)colour];

            // Four shards fly out at 90° increments.
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                float dx    = Mathf.Cos(angle) * 40f;
                float dy    = Mathf.Sin(angle) * 40f;

                var shard = new VisualElement();
                shard.AddToClassList(ClassShard);
                shard.AddToClassList(ClassShardPfx + colourSuffix);

                // worldBound gives screen-space coords — correct for cross-panel positioning.
                Vector2 center   = sourceCell.worldBound.center;
                float startLeft  = center.x - 4f;
                float startTop   = center.y - 4f;

                shard.style.left = startLeft;
                shard.style.top  = startTop;

                overlayParticles.Add(shard);

                DOVirtual.Float(0f, 1f, 0.3f, t =>
                {
                    shard.style.left    = startLeft + dx * t;
                    shard.style.top     = startTop  + dy * t;
                    shard.style.opacity = 1f - t;
                })
                .OnComplete(() => shard.RemoveFromHierarchy());
            }
        }
    }
}

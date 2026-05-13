using System;
using ChromaLogic.Core;
using ChromaLogic.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Phase 6 Gallery Dashboard — the Curator's primary home screen.
    /// <para>
    /// Loaded by <see cref="GameManager.LoadDashboard"/>. Displays four sections:
    /// the Featured Vessel card, Active Sequences, Curator's Ledger, and Daily Canvas.
    /// All data is sourced from <see cref="PlayerDataManager"/>.
    /// </para>
    /// <para>
    /// The nav bar remains visible throughout this scene;
    /// <see cref="PersistentCanvasController"/> is not hidden.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GalleryController : MonoBehaviour
    {
        // ── PlayerPrefs key ───────────────────────────────────────────────

        private const string KeyPendingClarity = "CL_PendingClarity";

        // ── UXML element name constants ───────────────────────────────────

        // Header
        private const string NameRankLabel             = "header-rank-label";
        // Featured Vessel
        private const string NameFeaturedName          = "featured-vessel-name";
        private const string NameFeaturedMeta          = "featured-vessel-meta";
        private const string NameBtnContinue           = "btn-continue-sequence";
        // Active Sequences
        private const string NameActiveSequencesScroll = "active-sequences-scroll";
        private const string NameActiveEmpty           = "active-sequences-empty";
        // Curator's Ledger
        private const string NameLogicPoints           = "ledger-logic-points";
        private const string NameGemsCollected         = "ledger-gems-collected";
        private const string NameLedgerRank            = "ledger-rank";
        // Daily Canvas
        private const string NameDailyDate             = "daily-date-label";
        private const string NameStreakDots            = "streak-dots";
        private const string NameBtnSolveDaily         = "btn-solve-daily";

        // ── USS class constants ───────────────────────────────────────────

        private const string ClassStreakDot         = "streak-dot";
        private const string ClassStreakDotFilled   = "streak-dot--filled";
        private const string ClassHidden            = "hidden";
        private const string ClassChip              = "active-sequence-chip";
        private const string ClassChipLabel         = "active-sequence-chip-label";

        // ── Private state ─────────────────────────────────────────────────

        private UIDocument      _document;

        private Label           _rankLabel;
        private Label           _featuredName;
        private Label           _featuredMeta;
        private Label           _activeEmpty;
        private ScrollView      _activeScroll;
        private Label           _logicPoints;
        private Label           _gemsCollected;
        private Label           _ledgerRank;
        private Label           _dailyDate;
        private VisualElement   _streakDots;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                Debug.LogError("[GalleryController] UIDocument component missing.");
                return;
            }
            BindElements();
        }

        private void Start()
        {
            var root = _document.rootVisualElement;

            root.Q<Button>(NameBtnContinue)
                ?.RegisterCallback<ClickEvent>(_ => HandleContinueSequence());
            root.Q<Button>(NameBtnSolveDaily)
                ?.RegisterCallback<ClickEvent>(_ => GameManager.Instance?.LoadMeditation());

            PopulateFeaturedVessel();
            PopulateActiveSequences();
            PopulateCuratorLedger();
            PopulateDailyCanvas();
        }

        private void OnEnable()
        {
            var pdm = PlayerDataManager.Instance;
            if (pdm == null) return;
            pdm.OnRankPromoted  += HandleRankPromoted;
            pdm.OnStreakUpdated += HandleStreakUpdated;
        }

        private void OnDisable()
        {
            var pdm = PlayerDataManager.Instance;
            if (pdm == null) return;
            pdm.OnRankPromoted  -= HandleRankPromoted;
            pdm.OnStreakUpdated -= HandleStreakUpdated;
        }

        // ── Section populators ────────────────────────────────────────────

        private void PopulateFeaturedVessel()
        {
            // Placeholder until vessel metadata service lands in a later phase.
            if (_featuredName != null) _featuredName.text = "The Gilded Meridian";
            if (_featuredMeta != null) _featuredMeta.text = "0% revealed — 0 Fragments";
        }

        private void PopulateActiveSequences()
        {
            var pdm = PlayerDataManager.Instance;
            var ids = pdm?.CompletedVesselIds;

            if (ids == null || ids.Count == 0)
            {
                _activeEmpty?.RemoveFromClassList(ClassHidden);
                _activeScroll?.AddToClassList(ClassHidden);
                return;
            }

            _activeEmpty?.AddToClassList(ClassHidden);
            _activeScroll?.RemoveFromClassList(ClassHidden);

            if (_activeScroll == null) return;

            _activeScroll.contentContainer.Clear();
            foreach (string id in ids)
            {
                var chip  = new VisualElement();
                chip.AddToClassList(ClassChip);
                var label = new Label(id);
                label.AddToClassList(ClassChipLabel);
                chip.Add(label);
                _activeScroll.contentContainer.Add(chip);
            }
        }

        private void PopulateCuratorLedger()
        {
            var pdm = PlayerDataManager.Instance;
            if (pdm == null) return;

            if (_logicPoints  != null) _logicPoints.text  = $"{pdm.LogicPoints:N0} Logic Points";
            if (_gemsCollected != null) _gemsCollected.text = $"{pdm.GemsCollected} Gems";
            if (_ledgerRank    != null) _ledgerRank.text    = pdm.Rank.ToString();
            if (_rankLabel     != null) _rankLabel.text     = pdm.Rank.ToString();
        }

        private void PopulateDailyCanvas()
        {
            var pdm = PlayerDataManager.Instance;
            if (_dailyDate != null)
                _dailyDate.text = DateTime.Today.ToString("dddd, d MMMM");

            if (_streakDots == null) return;
            _streakDots.Clear();

            int activeDots = 0;
            if (pdm != null)
            {
                int daysAgo = (DateTime.Today - pdm.LastPlayedDate.Date).Days;
                activeDots  = (daysAgo <= 1) ? Mathf.Min(pdm.CurrentStreak, 7) : 0;
            }

            for (int i = 0; i < 7; i++)
            {
                var dot = new VisualElement();
                dot.AddToClassList(ClassStreakDot);
                if (i >= 7 - activeDots) dot.AddToClassList(ClassStreakDotFilled);
                _streakDots.Add(dot);
            }
        }

        // ── Button handlers ───────────────────────────────────────────────

        private static void HandleContinueSequence()
        {
            PlayerPrefs.SetInt(KeyPendingClarity, (int)ClarityLevel.Sunlit);
            GameManager.Instance?.LoadSolve();
        }

        // ── PlayerDataManager event handlers ──────────────────────────────

        private void HandleRankPromoted(CuratorRank newRank)
        {
            if (_ledgerRank != null) _ledgerRank.text = newRank.ToString();
            if (_rankLabel  != null) _rankLabel.text  = newRank.ToString();
            PersistentCanvasController.Instance?.ShowToast($"Rank achieved — {newRank}.", 4f);
        }

        private void HandleStreakUpdated(int newStreak)
        {
            PopulateDailyCanvas();
        }

        // ── Element binding ───────────────────────────────────────────────

        private void BindElements()
        {
            var root         = _document.rootVisualElement;
            _rankLabel       = root.Q<Label>(NameRankLabel);
            _featuredName    = root.Q<Label>(NameFeaturedName);
            _featuredMeta    = root.Q<Label>(NameFeaturedMeta);
            _activeEmpty     = root.Q<Label>(NameActiveEmpty);
            _activeScroll    = root.Q<ScrollView>(NameActiveSequencesScroll);
            _logicPoints     = root.Q<Label>(NameLogicPoints);
            _gemsCollected   = root.Q<Label>(NameGemsCollected);
            _ledgerRank      = root.Q<Label>(NameLedgerRank);
            _dailyDate       = root.Q<Label>(NameDailyDate);
            _streakDots      = root.Q(NameStreakDots);
        }
    }
}

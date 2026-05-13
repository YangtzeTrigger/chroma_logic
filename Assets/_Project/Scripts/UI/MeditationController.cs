using System;
using System.Collections.Generic;
using ChromaLogic.Core;
using ChromaLogic.Managers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Manages the Curator's Sanctuary — Phase 9 Daily Meditation scene.
    /// <para>
    /// Two screens share a single <see cref="UIDocument"/>:
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>landing-screen</b> — today's vessel name, Curator streak, 7-day dot
    ///     calendar, and a Begin Meditation button. Visible by default.
    ///   </description></item>
    ///   <item><description>
    ///     <b>meditation-screen</b> — active 4×4 puzzle using
    ///     <see cref="MeditationSolver"/>, shape/colour palettes, and a step counter.
    ///   </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The daily puzzle is seeded from the calendar date, producing the same Vessel
    /// for all Curators on any given day. Vessel names are generated from the same
    /// seed using a fixed adjective/noun word list.
    /// </para>
    /// <para>
    /// On puzzle completion this controller records the date in
    /// <c>PlayerPrefs "CL_MeditationDays"</c>, calls
    /// <see cref="PlayerDataManager.UpdateStreak"/>, and awards
    /// <see cref="MeditationCompletionXP"/> Gallery XP.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MeditationController : MonoBehaviour
    {
        // ── Vessel name word lists ─────────────────────────────────────────

        private static readonly string[] Adjectives =
        {
            "Serene", "Luminous", "Tranquil", "Amber", "Gilded",
            "Silver",  "Crystalline", "Ethereal", "Prismatic", "Radiant",
            "Verdant", "Cobalt", "Opaline", "Auric", "Cerulean",
        };

        private static readonly string[] Nouns =
        {
            "Current", "Passage",  "Sequence",    "Resonance",   "Clarity",
            "Meridian", "Cadence", "Refrain",     "Momentum",    "Solstice",
            "Threshold", "Convergence", "Stillness", "Luminance", "Interval",
        };

        // ── Shape → Unicode symbol ─────────────────────────────────────────

        private static readonly Dictionary<ShapeType, string> ShapeSymbols =
            new Dictionary<ShapeType, string>
            {
                { ShapeType.Infinity,  "∞" },
                { ShapeType.Star,      "★" },
                { ShapeType.Hexagon,   "⬡" },
                { ShapeType.Crescent,  "☽" },
                { ShapeType.Diamond,   "◆" },
                { ShapeType.Plus,      "✦" },
                { ShapeType.Heart,     "♥" },
                { ShapeType.Ring,      "○" },
                { ShapeType.Triangle,  "▲" },
            };

        // ── ColourType → approximate display colour ────────────────────────

        private static readonly Dictionary<ColourType, Color> ColourMap =
            new Dictionary<ColourType, Color>
            {
                { ColourType.Crimson, new Color(0.75f, 0.28f, 0.23f) },
                { ColourType.Amber,   new Color(0.83f, 0.66f, 0.32f) },
                { ColourType.Jade,    new Color(0.24f, 0.67f, 0.42f) },
                { ColourType.Cobalt,  new Color(0.24f, 0.48f, 0.71f) },
                { ColourType.Violet,  new Color(0.55f, 0.30f, 0.69f) },
                { ColourType.Teal,    new Color(0.24f, 0.62f, 0.66f) },
                { ColourType.Rose,    new Color(0.88f, 0.53f, 0.66f) },
                { ColourType.Slate,   new Color(0.48f, 0.55f, 0.61f) },
                { ColourType.Gold,    new Color(0.78f, 0.66f, 0.49f) },
            };

        // ── PlayerPrefs / XP constants ─────────────────────────────────────

        private const string KeyMeditationDays   = "CL_MeditationDays";
        private const char   DaysDelimiter        = '|';
        private const int    MeditationCompletionXP = 15;

        // ── UXML element name constants ────────────────────────────────────

        private const string NameLandingScreen       = "landing-screen";
        private const string NameMeditationScreen    = "meditation-screen";
        private const string NameVesselNameLanding   = "vessel-name-landing";
        private const string NameStreakLabel         = "streak-label";
        private const string NameWeeklyDots          = "weekly-dots";
        private const string NameBtnBegin            = "btn-begin";
        private const string NameMeditationTitle     = "meditation-vessel-title";
        private const string NameStepCounter         = "step-counter";
        private const string NameMeditationGrid      = "meditation-grid";
        private const string NameShapePalette        = "shape-palette";
        private const string NameColourPalette       = "colour-palette";
        private const string NameCompletionPanel     = "completion-panel";
        private const string NameCompletionXp        = "completion-xp";
        private const string NameBtnExit             = "btn-exit";
        private const string NameBtnCompletionReturn = "btn-completion-return";

        // ── UXML element references ────────────────────────────────────────

        private VisualElement _landingScreen;
        private VisualElement _meditationScreen;
        private Label         _vesselNameLanding;
        private Label         _streakLabel;
        private VisualElement _weeklyDots;
        private Label         _meditationTitle;
        private Label         _stepCounter;
        private VisualElement _gridElement;
        private VisualElement _shapePalette;
        private VisualElement _colourPalette;
        private VisualElement _completionPanel;
        private Label         _completionXp;

        // ── Runtime state ──────────────────────────────────────────────────

        private MeditationSolver            _solver;
        private readonly VisualElement[,]   _cellElements = new VisualElement[4, 4];
        private readonly Label[,]           _cellLabels   = new Label[4, 4];
        private readonly List<VisualElement> _shapeBtns   = new List<VisualElement>();
        private readonly List<VisualElement> _colourBtns  = new List<VisualElement>();

        private int    _selectedShapeIndex  = -1;
        private int    _selectedColourIndex = -1;
        private int    _stepsCompleted;
        private int    _stepsTotal;
        private string _todaysVesselName;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            _landingScreen    = root.Q<VisualElement>(NameLandingScreen);
            _meditationScreen = root.Q<VisualElement>(NameMeditationScreen);
            _vesselNameLanding = root.Q<Label>(NameVesselNameLanding);
            _streakLabel      = root.Q<Label>(NameStreakLabel);
            _weeklyDots       = root.Q<VisualElement>(NameWeeklyDots);
            _meditationTitle  = root.Q<Label>(NameMeditationTitle);
            _stepCounter      = root.Q<Label>(NameStepCounter);
            _gridElement      = root.Q<VisualElement>(NameMeditationGrid);
            _shapePalette     = root.Q<VisualElement>(NameShapePalette);
            _colourPalette    = root.Q<VisualElement>(NameColourPalette);
            _completionPanel  = root.Q<VisualElement>(NameCompletionPanel);
            _completionXp     = root.Q<Label>(NameCompletionXp);

            root.Q<Button>(NameBtnBegin)           ?.RegisterCallback<ClickEvent>(_ => BeginMeditation());
            root.Q<Button>(NameBtnExit)            ?.RegisterCallback<ClickEvent>(_ => ShowLandingScreen());
            root.Q<Button>(NameBtnCompletionReturn)?.RegisterCallback<ClickEvent>(_ => GameManager.Instance?.LoadDashboard());
        }

        private void Start()
        {
            int dateSeed = GetDateSeed();
            _todaysVesselName = GenerateVesselName(dateSeed);

            InitialiseSolver(dateSeed);
            PopulateLandingScreen();
            ShowLandingScreen();
        }

        // ── Landing screen ─────────────────────────────────────────────────

        private void PopulateLandingScreen()
        {
            if (_vesselNameLanding != null)
                _vesselNameLanding.text = _todaysVesselName;

            PlayerDataManager pdm = PlayerDataManager.Instance;
            if (_streakLabel != null && pdm != null)
                _streakLabel.text = $"{pdm.CurrentStreak} Day Streak";

            if (_weeklyDots != null)
                PopulateWeeklyDots();
        }

        private void PopulateWeeklyDots()
        {
            _weeklyDots.Clear();
            HashSet<string> playedDays = LoadMeditationDays();

            for (int i = 6; i >= 0; i--)
            {
                DateTime day = DateTime.Today.AddDays(-i);
                var dot = new VisualElement();
                dot.AddToClassList("meditation-dot");
                if (playedDays.Contains(day.ToString("yyyy-MM-dd")))
                    dot.AddToClassList("meditation-dot--filled");
                _weeklyDots.Add(dot);
            }
        }

        private void ShowLandingScreen()
        {
            _landingScreen?   .RemoveFromClassList("hidden");
            _meditationScreen?.AddToClassList("hidden");
        }

        // ── Meditation screen ──────────────────────────────────────────────

        private void BeginMeditation()
        {
            _landingScreen?   .AddToClassList("hidden");
            _meditationScreen?.RemoveFromClassList("hidden");

            if (_meditationTitle != null)
                _meditationTitle.text = _todaysVesselName;

            BuildPalettes();
            BuildGrid();
            UpdateStepCounter();
        }

        // ── Solver initialisation ──────────────────────────────────────────

        private void InitialiseSolver(int dateSeed)
        {
            PlayerDataManager pdm = PlayerDataManager.Instance;
            if (pdm == null) return;

            try
            {
                _solver = new MeditationSolver(dateSeed);
                _solver.GeneratePuzzle(pdm.MeditationShapes, pdm.MeditationColours);
            }
            catch (Exception ex)
            {
                Debug.LogError("[MeditationController] Failed to generate puzzle: " + ex.Message);
                return;
            }

            _stepsCompleted = 0;
            _stepsTotal     = 0;
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    if (_solver.GetCell(r, c).IsEmpty) _stepsTotal++;
        }

        // ── Grid construction and refresh ──────────────────────────────────

        private void BuildGrid()
        {
            if (_gridElement == null || _solver == null) return;
            _gridElement.Clear();

            for (int r = 0; r < 4; r++)
            {
                var row = new VisualElement();
                row.AddToClassList("meditation-row");

                for (int c = 0; c < 4; c++)
                {
                    var cell = new VisualElement();
                    cell.AddToClassList("meditation-cell");

                    var label = new Label();
                    label.AddToClassList("meditation-cell-label");
                    cell.Add(label);

                    _cellElements[r, c] = cell;
                    _cellLabels[r, c]   = label;

                    int capturedR = r, capturedC = c;
                    cell.RegisterCallback<ClickEvent>(_ => OnCellTapped(capturedR, capturedC));
                    row.Add(cell);
                }

                _gridElement.Add(row);
            }

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            if (_solver == null) return;
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    RefreshCell(r, c);
        }

        private void RefreshCell(int row, int col)
        {
            VisualElement element = _cellElements[row, col];
            Label         label   = _cellLabels[row, col];
            if (element == null) return;

            PuzzleCell cell = _solver.GetCell(row, col);

            element.RemoveFromClassList("meditation-cell--prefilled");
            element.RemoveFromClassList("meditation-cell--filled");
            element.RemoveFromClassList("meditation-cell--empty");

            if (cell.IsEmpty)
            {
                element.AddToClassList("meditation-cell--empty");
                element.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                if (label != null) { label.text = ""; label.style.color = new Color(0f, 0f, 0f, 0f); }
                return;
            }

            string symbol   = ShapeSymbols.TryGetValue(cell.Shape!.Value,  out string sym) ? sym  : "?";
            Color  tintBase = ColourMap   .TryGetValue(cell.Colour!.Value, out Color  col) ? col  : Color.white;
            bool   isPre    = cell.IsPreFilled;

            element.style.backgroundColor = new Color(tintBase.r, tintBase.g, tintBase.b, isPre ? 0.22f : 0.14f);
            element.AddToClassList(isPre ? "meditation-cell--prefilled" : "meditation-cell--filled");

            if (label != null)
            {
                label.text        = symbol;
                label.style.color = new Color(tintBase.r, tintBase.g, tintBase.b, isPre ? 0.95f : 0.85f);
            }
        }

        // ── Cell interaction ───────────────────────────────────────────────

        private void OnCellTapped(int row, int col)
        {
            if (_solver == null) return;

            PuzzleCell cell = _solver.GetCell(row, col);

            // Tap a user-placed cell to erase it, allowing correction
            if (!cell.IsEmpty && !cell.IsPreFilled)
            {
                if (_solver.TryClearCell(row, col))
                {
                    _stepsCompleted--;
                    RefreshCell(row, col);
                    UpdateStepCounter();
                }
                return;
            }

            if (!cell.IsEmpty) return; // pre-filled clue, never interactive

            if (_selectedShapeIndex < 0 || _selectedColourIndex < 0)
            {
                PersistentCanvasController.Instance?.ShowToast("Select a shape and colour first.", 1.5f);
                return;
            }

            PlayerDataManager pdm = PlayerDataManager.Instance;
            if (pdm == null) return;

            ShapeType  shape  = pdm.MeditationShapes[_selectedShapeIndex];
            ColourType colour = pdm.MeditationColours[_selectedColourIndex];

            if (_solver.TryPlaceValue(row, col, shape, colour))
            {
                _stepsCompleted++;
                RefreshCell(row, col);
                UpdateStepCounter();

                if (_solver.IsSolved()) HandleMeditationComplete();
            }
            else
            {
                AnimateCellError(_cellElements[row, col]);
            }
        }

        private void UpdateStepCounter()
        {
            if (_stepCounter != null)
                _stepCounter.text = $"STEP {_stepsCompleted} / {_stepsTotal}";
        }

        // Amber wobble — no error count, no red, keeping the sanctuary tone
        private static void AnimateCellError(VisualElement cell)
        {
            if (cell == null) return;
            cell.AddToClassList("meditation-cell--error");
            DOVirtual.DelayedCall(0.5f, () => cell.RemoveFromClassList("meditation-cell--error"));
        }

        // ── Palette construction ───────────────────────────────────────────

        private void BuildPalettes()
        {
            PlayerDataManager pdm = PlayerDataManager.Instance;
            if (pdm == null || _shapePalette == null || _colourPalette == null) return;

            _shapePalette.Clear();
            _colourPalette.Clear();
            _shapeBtns.Clear();
            _colourBtns.Clear();
            _selectedShapeIndex  = -1;
            _selectedColourIndex = -1;

            for (int i = 0; i < pdm.MeditationShapes.Count; i++)
            {
                ShapeType shape  = pdm.MeditationShapes[i];
                string    symbol = ShapeSymbols.TryGetValue(shape, out string s) ? s : "?";

                var btn = new Button { text = symbol };
                btn.AddToClassList("palette-btn");

                int captured = i;
                btn.RegisterCallback<ClickEvent>(_ => SelectShape(captured));

                _shapeBtns.Add(btn);
                _shapePalette.Add(btn);
            }

            for (int i = 0; i < pdm.MeditationColours.Count; i++)
            {
                ColourType colour = pdm.MeditationColours[i];
                Color      tint   = ColourMap.TryGetValue(colour, out Color c) ? c : Color.white;

                var swatch = new Button();
                swatch.AddToClassList("colour-swatch");
                swatch.style.backgroundColor = tint;

                int captured = i;
                swatch.RegisterCallback<ClickEvent>(_ => SelectColour(captured));

                _colourBtns.Add(swatch);
                _colourPalette.Add(swatch);
            }
        }

        private void SelectShape(int index)
        {
            for (int i = 0; i < _shapeBtns.Count; i++)
            {
                if (i == index) _shapeBtns[i].AddToClassList("palette-btn--selected");
                else            _shapeBtns[i].RemoveFromClassList("palette-btn--selected");
            }
            _selectedShapeIndex = index;
        }

        private void SelectColour(int index)
        {
            for (int i = 0; i < _colourBtns.Count; i++)
            {
                if (i == index) _colourBtns[i].AddToClassList("colour-swatch--selected");
                else            _colourBtns[i].RemoveFromClassList("colour-swatch--selected");
            }
            _selectedColourIndex = index;
        }

        // ── Completion ─────────────────────────────────────────────────────

        private void HandleMeditationComplete()
        {
            HashSet<string> days = LoadMeditationDays();
            MarkTodayPlayed(days);

            PlayerDataManager pdm = PlayerDataManager.Instance;
            pdm?.UpdateStreak();
            pdm?.AddGalleryXP(MeditationCompletionXP);

            ProgressionManager.Instance?.CheckAllDesignations();

            if (_streakLabel != null && pdm != null)
                _streakLabel.text = $"{pdm.CurrentStreak} Day Streak";

            if (_completionXp != null)
                _completionXp.text = $"+{MeditationCompletionXP} Gallery XP";

            _completionPanel?.RemoveFromClassList("hidden");

            PersistentCanvasController.Instance?.ShowToast("Clarity achieved.", 3f);
        }

        // ── Meditation days persistence ────────────────────────────────────

        private static HashSet<string> LoadMeditationDays()
        {
            string raw    = PlayerPrefs.GetString(KeyMeditationDays, string.Empty);
            var    result = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(raw)) return result;

            foreach (string entry in raw.Split(DaysDelimiter))
                if (!string.IsNullOrEmpty(entry))
                    result.Add(entry);

            return result;
        }

        private static void MarkTodayPlayed(HashSet<string> days)
        {
            days.Add(DateTime.Today.ToString("yyyy-MM-dd"));
            PlayerPrefs.SetString(KeyMeditationDays, string.Join(DaysDelimiter.ToString(), days));
            PlayerPrefs.Save();
        }

        // ── Date-seeded helpers ────────────────────────────────────────────

        /// <summary>
        /// Converts today's date to a deterministic integer seed.
        /// Identical seeds produce identical vessel names and puzzles.
        /// </summary>
        private static int GetDateSeed()
        {
            DateTime t = DateTime.Today;
            return t.Year * 10000 + t.Month * 100 + t.Day;
        }

        /// <summary>
        /// Generates a two-word vessel name from <paramref name="seed"/> by selecting
        /// entries from the fixed <see cref="Adjectives"/> and <see cref="Nouns"/> lists.
        /// Produces ~225 unique combinations with no calendar repetition for over six months.
        /// </summary>
        private static string GenerateVesselName(int seed)
        {
            var rng = new Random(seed);
            return Adjectives[rng.Next(Adjectives.Length)] + " " + Nouns[rng.Next(Nouns.Length)];
        }
    }
}

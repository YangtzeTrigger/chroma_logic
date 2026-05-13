using ChromaLogic.Core;
using ChromaLogic.Gameplay;
using ChromaLogic.Managers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Orchestrates the Phase 4 Solve scene: owns <see cref="GridData"/>,
    /// connects <see cref="GridRenderer"/> and <see cref="TileTrayController"/>,
    /// and bridges all C# events from the logic layer to the UI.
    /// <para>
    /// Clarity Level is read from <c>PlayerPrefs</c> key <c>CL_PendingClarity</c>
    /// (set by the Gallery scene in Phase 6) and falls back to <see cref="ClarityLevel.Sunlit"/>.
    /// The <c>Inspector</c> field <see cref="_defaultClarity"/> overrides the fallback
    /// during development.
    /// </para>
    /// <para>
    /// The nav bar remains visible during the Solve scene (unlike Onboarding);
    /// <see cref="PersistentCanvasController"/> nav methods are not called here.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(GridRenderer))]
    [RequireComponent(typeof(TileTrayController))]
    public sealed class SolveController : MonoBehaviour
    {
        // ── PlayerPrefs key (Phase 6 Gallery writes this before LoadSolve) ─

        private const string KeyPendingClarity = "CL_PendingClarity";

        // ── UXML element name constants ───────────────────────────────────

        private const string NameClarityLabel   = "clarity-label";
        private const string NameErrorDot0      = "error-dot-0";
        private const string NameErrorDot1      = "error-dot-1";
        private const string NameErrorDot2      = "error-dot-2";
        private const string NameBtnInsight     = "btn-insight";
        private const string NameSolveGrid      = "solve-grid";
        private const string NamePaletteShapes  = "palette-shapes";
        private const string NamePaletteColours = "palette-colours";
        private const string NameBtnClear       = "btn-clear";

        // ── USS class constants ───────────────────────────────────────────

        private const string ClassErrorActive = "error-dot--active";

        // ── Inspector ─────────────────────────────────────────────────────

        [SerializeField]
        private ClarityLevel _defaultClarity = ClarityLevel.Sunlit;

        [SerializeField]
        private RevealSequence _revealSequence;

        // ── Private state ─────────────────────────────────────────────────

        private UIDocument          _document;
        private GridRenderer        _gridRenderer;
        private TileTrayController  _trayController;

        private SudokuSolver        _solver;
        private GridData            _gridData;

        private Label           _clarityLabel;
        private VisualElement[] _errorDots;
        private Button          _btnInsight;

        private ClarityLevel    _activeClarity;

        private int _selectedRow = -1;
        private int _selectedCol = -1;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            _document       = GetComponent<UIDocument>();
            _gridRenderer   = GetComponent<GridRenderer>();
            _trayController = GetComponent<TileTrayController>();

            if (_document == null)
            {
                Debug.LogError("[SolveController] UIDocument component missing.");
                return;
            }

            BindElements();
        }

        private void Start()
        {
            var root = _document.rootVisualElement;
            _activeClarity = ReadPendingClarity();

            // Generate puzzle.
            _solver = new SudokuSolver();
            _solver.GeneratePuzzle(_activeClarity);
            _gridData = new GridData(_solver);

            // Subscribe to GridData events before rendering so no event is missed.
            SubscribeGridDataEvents();

            // Initialise child components.
            _gridRenderer.Initialise(root.Q(NameSolveGrid), _gridData);
            _trayController.Initialise(
                root.Q(NamePaletteShapes),
                root.Q(NamePaletteColours),
                root.Q<Button>(NameBtnClear),
                _btnInsight);

            // Subscribe to component events.
            _gridRenderer.OnCellTapped         += HandleCellTapped;
            _trayController.OnTilePicked        += HandleTilePicked;
            _trayController.OnClearRequested    += HandleClearRequested;
            _trayController.OnInsightRequested  += HandleInsightRequested;

            UpdateClarityLabel(_activeClarity);
            UpdateErrorDots(0);
        }

        private void OnDestroy()
        {
            if (_gridData != null)
            {
                _gridData.OnRowComplete       -= _gridRenderer.FlashRow;
                _gridData.OnColumnComplete    -= _gridRenderer.FlashColumn;
                _gridData.OnBoxComplete       -= _gridRenderer.FlashBox;
                _gridData.OnGridComplete      -= HandleGridComplete;
                _gridData.OnErrorCountChanged -= HandleErrorCountChanged;
                _gridData.OnSafetyNetTriggered -= HandleSafetyNetTriggered;
            }

            if (_gridRenderer != null)   _gridRenderer.OnCellTapped      -= HandleCellTapped;
            if (_trayController != null)
            {
                _trayController.OnTilePicked        -= HandleTilePicked;
                _trayController.OnClearRequested    -= HandleClearRequested;
                _trayController.OnInsightRequested  -= HandleInsightRequested;
            }
        }

        // ── Event subscriptions ───────────────────────────────────────────

        private void SubscribeGridDataEvents()
        {
            _gridData.OnRowComplete       += _gridRenderer.FlashRow;
            _gridData.OnColumnComplete    += _gridRenderer.FlashColumn;
            _gridData.OnBoxComplete       += _gridRenderer.FlashBox;
            _gridData.OnGridComplete      += HandleGridComplete;
            _gridData.OnErrorCountChanged += HandleErrorCountChanged;
            _gridData.OnSafetyNetTriggered += HandleSafetyNetTriggered;
        }

        // ── Interaction handlers ──────────────────────────────────────────

        private void HandleCellTapped(int row, int col)
        {
            var cell = _gridData.Cells[row, col];
            if (cell.IsLocked) return;

            _selectedRow = row;
            _selectedCol = col;
            _gridRenderer.SelectCell(row, col);
            _trayController.Reset();

            if (!cell.IsEmpty)
                _trayController.ShowClearButton();
            else
                _trayController.HideClearButton();
        }

        private void HandleTilePicked(ShapeType shape, ColourType colour)
        {
            if (_selectedRow < 0) return;

            int r = _selectedRow, c = _selectedCol;
            PlaceResult result = _gridData.TryPlaceValue(r, c, shape, colour);

            if (result == PlaceResult.Correct)
            {
                _gridRenderer.SetCellCorrect(r, c, shape, colour);
                _gridRenderer.DeselectAll();
                _trayController.HideClearButton();
                _selectedRow = -1;
                _selectedCol = -1;
            }
            else if (result == PlaceResult.Incorrect)
            {
                _gridRenderer.SetCellWrong(r, c);
            }
            // PlaceResult.Invalid: cell is locked or already filled — should not reach here
            // through normal flow since we guard in HandleCellTapped.
        }

        private void HandleClearRequested()
        {
            if (_selectedRow < 0) return;
            if (_gridData.TryClearCell(_selectedRow, _selectedCol))
            {
                _gridRenderer.ClearCell(_selectedRow, _selectedCol);
                _trayController.HideClearButton();
                // Cell remains selected (empty) so Curator can immediately re-place.
            }
        }

        private void HandleInsightRequested()
        {
            HintCell? hint = _solver.GetHint();
            if (hint == null) return;

            int r = hint.Value.Row;
            int c = hint.Value.Col;

            _selectedRow = r;
            _selectedCol = c;
            _gridRenderer.SelectCell(r, c);
            _trayController.HideClearButton();
            _trayController.PreSelectTile(hint.Value.Shape, hint.Value.Colour);
        }

        // ── GridData event handlers ───────────────────────────────────────

        private void HandleGridComplete()
        {
            _gridRenderer.DeselectAll();
            _trayController.Reset();
            _trayController.HideClearButton();
            _selectedRow = -1;
            _selectedCol = -1;

            if (_revealSequence != null)
            {
                _revealSequence.VesselClarity = _activeClarity;
                _revealSequence.Begin(_gridRenderer, _gridData, _document.rootVisualElement);
            }
            else
            {
                PersistentCanvasController.Instance?.ShowToast("Vessel complete.", 3f);
                DOVirtual.DelayedCall(3.5f, () => GameManager.Instance?.LoadDashboard());
            }
        }

        private void HandleErrorCountChanged(int newCount)
        {
            UpdateErrorDots(newCount);
        }

        private void HandleSafetyNetTriggered()
        {
            PersistentCanvasController.Instance?.ShowToast(
                "Safety Net — take care with remaining placements.", 3f);
        }

        // ── UI helpers ────────────────────────────────────────────────────

        private void BindElements()
        {
            var root = _document.rootVisualElement;
            _clarityLabel = root.Q<Label>(NameClarityLabel);
            _btnInsight   = root.Q<Button>(NameBtnInsight);
            _errorDots    = new VisualElement[]
            {
                root.Q(NameErrorDot0),
                root.Q(NameErrorDot1),
                root.Q(NameErrorDot2),
            };
        }

        private void UpdateClarityLabel(ClarityLevel clarity)
        {
            if (_clarityLabel != null)
                _clarityLabel.text = clarity.ToString();
        }

        private void UpdateErrorDots(int errorCount)
        {
            for (int i = 0; i < _errorDots.Length; i++)
            {
                if (i < errorCount) _errorDots[i]?.AddToClassList(ClassErrorActive);
                else                _errorDots[i]?.RemoveFromClassList(ClassErrorActive);
            }
        }

        private ClarityLevel ReadPendingClarity()
        {
            int raw = PlayerPrefs.GetInt(KeyPendingClarity, (int)_defaultClarity);
            PlayerPrefs.DeleteKey(KeyPendingClarity);
            return (ClarityLevel)raw;
        }
    }
}

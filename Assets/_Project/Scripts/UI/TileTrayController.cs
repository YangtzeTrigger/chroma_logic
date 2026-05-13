using System;
using ChromaLogic.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Manages the shape palette and colour swatch tray at the bottom of the Solve scene.
    /// <para>
    /// Two-step selection: the Curator first taps a shape, then a colour (or in either
    /// order). When both are set <see cref="OnTilePicked"/> fires and the pending state
    /// resets automatically. Wrong placements that return from
    /// <see cref="SolveController"/> also clear pending state via <see cref="Reset"/>.
    /// </para>
    /// <para>
    /// The Clear button is hidden by default; <see cref="ShowClearButton"/> /
    /// <see cref="HideClearButton"/> toggle it based on whether the currently
    /// selected cell holds a Curator-placed value.
    /// </para>
    /// <para>
    /// <see cref="PreSelectTile"/> is called by The Insight mechanic to pre-highlight
    /// the correct shape and colour for a hinted cell.
    /// </para>
    /// </summary>
    public sealed class TileTrayController : MonoBehaviour
    {
        // ── Events ─────────────────────────────────────────────────────────

        /// <summary>Fires when the Curator has selected both a shape and a colour.</summary>
        public event Action<ShapeType, ColourType> OnTilePicked;

        /// <summary>Fires when the Curator taps the Clear button.</summary>
        public event Action OnClearRequested;

        /// <summary>Fires when the Curator taps the Insight button.</summary>
        public event Action OnInsightRequested;

        // ── USS class constants ─────────────────────────────────────────────

        private const string ClassShapeBtn     = "solve-shape-btn";
        private const string ClassShapeSel     = "solve-shape-btn--selected";
        private const string ClassColourBtn    = "solve-colour-btn";
        private const string ClassColourSel    = "solve-colour-btn--selected";
        private const string ClassHidden       = "hidden";

        private const int PaletteSize = 9;   // all nine shapes / colours

        // ── State ───────────────────────────────────────────────────────────

        private Button[]    _shapeBtns;
        private Button[]    _colourBtns;
        private Button      _btnClear;

        private int _pendingShape  = -1;
        private int _pendingColour = -1;

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Builds the shape and colour palettes inside the supplied container elements
        /// and wires the Clear and Insight buttons. Must be called once before any
        /// interaction can occur.
        /// </summary>
        public void Initialise(
            VisualElement paletteShapes,
            VisualElement paletteColours,
            Button        btnClear,
            Button        btnInsight)
        {
            _btnClear = btnClear;

            BuildShapePalette(paletteShapes);
            BuildColourPalette(paletteColours);

            btnClear? .RegisterCallback<ClickEvent>(_ => OnClearRequested?.Invoke());
            btnInsight?.RegisterCallback<ClickEvent>(_ => OnInsightRequested?.Invoke());
        }

        /// <summary>
        /// Pre-highlights <paramref name="shape"/> and <paramref name="colour"/> in the
        /// palette without committing. Used by The Insight mechanic so the Curator only
        /// needs to confirm the pre-selected tile.
        /// </summary>
        public void PreSelectTile(ShapeType shape, ColourType colour)
        {
            _pendingShape  = (int)shape;
            _pendingColour = (int)colour;
            HighlightShapeBtn(_pendingShape);
            HighlightColourBtn(_pendingColour);
        }

        /// <summary>Clears all pending selections and palette highlights.</summary>
        public void Reset()
        {
            _pendingShape  = -1;
            _pendingColour = -1;
            ClearHighlights();
        }

        /// <summary>Shows the Clear button (call when a non-locked, filled cell is selected).</summary>
        public void ShowClearButton() => _btnClear?.RemoveFromClassList(ClassHidden);

        /// <summary>Hides the Clear button (call when an empty or locked cell is selected).</summary>
        public void HideClearButton() => _btnClear?.AddToClassList(ClassHidden);

        // ── Construction ────────────────────────────────────────────────────

        private void BuildShapePalette(VisualElement root)
        {
            _shapeBtns = new Button[PaletteSize];
            for (int i = 0; i < PaletteSize; i++)
            {
                var btn = new Button { text = TutorialGridData.AllShapeSymbols[i] };
                btn.AddToClassList(ClassShapeBtn);
                int idx = i;
                btn.RegisterCallback<ClickEvent>(_ => OnShapePicked(idx));
                _shapeBtns[i] = btn;
                root.Add(btn);
            }
        }

        private void BuildColourPalette(VisualElement root)
        {
            _colourBtns = new Button[PaletteSize];
            for (int i = 0; i < PaletteSize; i++)
            {
                var btn = new Button();
                btn.AddToClassList(ClassColourBtn);
                btn.AddToClassList("solve-colour-btn--" + TutorialGridData.AllColourSuffix[i]);
                int idx = i;
                btn.RegisterCallback<ClickEvent>(_ => OnColourPicked(idx));
                _colourBtns[i] = btn;
                root.Add(btn);
            }
        }

        // ── Interaction ─────────────────────────────────────────────────────

        private void OnShapePicked(int idx)
        {
            _pendingShape = idx;
            HighlightShapeBtn(idx);
            TryCommit();
        }

        private void OnColourPicked(int idx)
        {
            _pendingColour = idx;
            HighlightColourBtn(idx);
            TryCommit();
        }

        private void TryCommit()
        {
            if (_pendingShape < 0 || _pendingColour < 0) return;

            var shape  = (ShapeType)_pendingShape;
            var colour = (ColourType)_pendingColour;

            Reset();
            OnTilePicked?.Invoke(shape, colour);
        }

        // ── Highlight helpers ────────────────────────────────────────────────

        private void ClearHighlights()
        {
            foreach (var btn in _shapeBtns)  btn?.RemoveFromClassList(ClassShapeSel);
            foreach (var btn in _colourBtns) btn?.RemoveFromClassList(ClassColourSel);
        }

        private void HighlightShapeBtn(int idx)
        {
            for (int i = 0; i < _shapeBtns.Length; i++)
            {
                if (i == idx) _shapeBtns[i].AddToClassList(ClassShapeSel);
                else          _shapeBtns[i].RemoveFromClassList(ClassShapeSel);
            }
        }

        private void HighlightColourBtn(int idx)
        {
            for (int i = 0; i < _colourBtns.Length; i++)
            {
                if (i == idx) _colourBtns[i].AddToClassList(ClassColourSel);
                else          _colourBtns[i].RemoveFromClassList(ClassColourSel);
            }
        }
    }
}

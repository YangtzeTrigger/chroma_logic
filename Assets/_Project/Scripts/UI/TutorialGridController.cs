using System;
using ChromaLogic.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Manages the interactive 6×6 tutorial Vessel on Phase 3 onboarding screen 2.
    /// <para>
    /// Call <see cref="Initialise"/> once, passing the three container VisualElements
    /// found by <see cref="OnboardingController"/>. The controller builds grid cells and
    /// shape/colour palette buttons procedurally, then handles all tap interaction.
    /// </para>
    /// <para>
    /// When the Curator correctly places all empty cells, <see cref="OnTutorialComplete"/>
    /// fires. Wrong placements trigger an amber border flash — no penalty, no counter.
    /// </para>
    /// </summary>
    public sealed class TutorialGridController : MonoBehaviour
    {
        // ── Event ─────────────────────────────────────────────────────────

        /// <summary>Fires once when every empty cell has been correctly placed.</summary>
        public event Action OnTutorialComplete;

        // ── USS class constants ────────────────────────────────────────────

        private const string ClassCell              = "tutorial-cell";
        private const string ClassPrefilled         = "tutorial-cell--prefilled";
        private const string ClassEmpty             = "tutorial-cell--empty";
        private const string ClassSelected          = "tutorial-cell--selected";
        private const string ClassCorrect           = "tutorial-cell--correct";
        private const string ClassWrong             = "tutorial-cell--wrong";
        private const string ClassShapeLabel        = "cell-shape-label";
        private const string ClassPaletteShape      = "palette-shape-btn";
        private const string ClassPaletteShapeSel   = "palette-shape-btn--selected";
        private const string ClassPaletteColour     = "palette-colour-btn";
        private const string ClassPaletteColourSel  = "palette-colour-btn--selected";

        // ── State ──────────────────────────────────────────────────────────

        private VisualElement[,] _cells;
        private int[]  _shapeState;   // -1 = empty, 0-5 = placed index
        private int[]  _colourState;  // -1 = empty, 0-5 = placed index

        private int _selectedRow = -1;
        private int _selectedCol = -1;
        private int _pendingShape  = -1;
        private int _pendingColour = -1;

        private Button[] _shapeBtns;
        private Button[] _colourBtns;

        private int _correctCount;

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Builds the grid and palettes inside the supplied container elements.
        /// Must be called before any interaction can occur.
        /// </summary>
        public void Initialise(VisualElement gridRoot, VisualElement paletteShapes, VisualElement paletteColours)
        {
            int sz = TutorialGridData.Size;
            _cells       = new VisualElement[sz, sz];
            _shapeState  = new int[sz * sz];
            _colourState = new int[sz * sz];

            for (int r = 0; r < sz; r++)
            for (int c = 0; c < sz; c++)
            {
                int idx = r * sz + c;
                bool pre = TutorialGridData.IsPreFilled(r, c);
                _shapeState[idx]  = pre ? TutorialGridData.GetShapeIndex(r, c)  : -1;
                _colourState[idx] = pre ? TutorialGridData.GetColourIndex(r, c) : -1;
            }

            BuildGrid(gridRoot);
            BuildShapePalette(paletteShapes);
            BuildColourPalette(paletteColours);
        }

        // ── Grid construction ─────────────────────────────────────────────

        private void BuildGrid(VisualElement root)
        {
            int sz = TutorialGridData.Size;
            for (int r = 0; r < sz; r++)
            for (int c = 0; c < sz; c++)
            {
                var cell = new VisualElement();
                cell.AddToClassList(ClassCell);

                if (TutorialGridData.IsPreFilled(r, c))
                {
                    cell.AddToClassList(ClassPrefilled);
                    int si = TutorialGridData.GetShapeIndex(r, c);
                    int ci = TutorialGridData.GetColourIndex(r, c);
                    ApplyCellVisual(cell, si, ci);
                }
                else
                {
                    cell.AddToClassList(ClassEmpty);
                    int row = r, col = c;
                    cell.RegisterCallback<ClickEvent>(_ => OnCellTapped(row, col));
                }

                _cells[r, c] = cell;
                root.Add(cell);
            }
        }

        private void ApplyCellVisual(VisualElement cell, int shapeIdx, int colourIdx)
        {
            // Remove stale colour classes.
            foreach (string suf in TutorialGridData.ColourSuffix)
                cell.RemoveFromClassList("tutorial-cell--" + suf);

            if (shapeIdx < 0 || colourIdx < 0) return;

            cell.AddToClassList("tutorial-cell--" + TutorialGridData.ColourSuffix[colourIdx]);

            // Replace shape label.
            var old = cell.Q<Label>(className: ClassShapeLabel);
            old?.RemoveFromHierarchy();

            var lbl = new Label(TutorialGridData.ShapeSymbols[shapeIdx]);
            lbl.AddToClassList(ClassShapeLabel);
            cell.Add(lbl);
        }

        // ── Palette construction ──────────────────────────────────────────

        private void BuildShapePalette(VisualElement root)
        {
            int sz = TutorialGridData.Size;
            _shapeBtns = new Button[sz];
            for (int i = 0; i < sz; i++)
            {
                var btn = new Button { text = TutorialGridData.ShapeSymbols[i] };
                btn.AddToClassList(ClassPaletteShape);
                int idx = i;
                btn.RegisterCallback<ClickEvent>(_ => OnShapePicked(idx));
                _shapeBtns[i] = btn;
                root.Add(btn);
            }
        }

        private void BuildColourPalette(VisualElement root)
        {
            int sz = TutorialGridData.Size;
            _colourBtns = new Button[sz];
            for (int i = 0; i < sz; i++)
            {
                var btn = new Button();
                btn.AddToClassList(ClassPaletteColour);
                btn.AddToClassList("palette-colour-btn--" + TutorialGridData.ColourSuffix[i]);
                int idx = i;
                btn.RegisterCallback<ClickEvent>(_ => OnColourPicked(idx));
                _colourBtns[i] = btn;
                root.Add(btn);
            }
        }

        // ── Interaction ───────────────────────────────────────────────────

        private void OnCellTapped(int row, int col)
        {
            // Deselect previous cell.
            if (_selectedRow >= 0)
                _cells[_selectedRow, _selectedCol].RemoveFromClassList(ClassSelected);

            _selectedRow   = row;
            _selectedCol   = col;
            _pendingShape  = -1;
            _pendingColour = -1;

            _cells[row, col].AddToClassList(ClassSelected);
            ClearPaletteHighlights();
        }

        private void OnShapePicked(int idx)
        {
            if (_selectedRow < 0) return;
            _pendingShape = idx;
            HighlightShapeBtn(idx);
            TryCommit();
        }

        private void OnColourPicked(int idx)
        {
            if (_selectedRow < 0) return;
            _pendingColour = idx;
            HighlightColourBtn(idx);
            TryCommit();
        }

        private void TryCommit()
        {
            if (_pendingShape < 0 || _pendingColour < 0) return;

            int r = _selectedRow, c = _selectedCol;
            bool correct = _pendingShape  == TutorialGridData.GetShapeIndex(r, c) &&
                           _pendingColour == TutorialGridData.GetColourIndex(r, c);

            var cell = _cells[r, c];

            if (correct)
            {
                int sz  = TutorialGridData.Size;
                int idx = r * sz + c;
                _shapeState[idx]  = _pendingShape;
                _colourState[idx] = _pendingColour;

                cell.RemoveFromClassList(ClassSelected);
                cell.RemoveFromClassList(ClassEmpty);
                cell.AddToClassList(ClassCorrect);
                ApplyCellVisual(cell, _pendingShape, _pendingColour);
                AnimateSuccess(cell);

                _selectedRow   = -1;
                _selectedCol   = -1;
                _pendingShape  = -1;
                _pendingColour = -1;
                ClearPaletteHighlights();

                _correctCount++;
                if (_correctCount >= TutorialGridData.EmptyCellCount)
                    OnTutorialComplete?.Invoke();
            }
            else
            {
                AnimateWrong(cell);
                _pendingShape  = -1;
                _pendingColour = -1;
                ClearPaletteHighlights();
            }
        }

        // ── Animations ────────────────────────────────────────────────────

        private static void AnimateSuccess(VisualElement cell)
        {
            DOVirtual.Float(1f, 1.12f, 0.1f,
                v => cell.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f))))
            .OnComplete(() =>
                DOVirtual.Float(1.12f, 1f, 0.15f,
                    v => cell.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f)))));
        }

        private static void AnimateWrong(VisualElement cell)
        {
            cell.AddToClassList(ClassWrong);
            DOVirtual.DelayedCall(0.45f, () => cell.RemoveFromClassList(ClassWrong));
        }

        // ── Palette highlight helpers ─────────────────────────────────────

        private void ClearPaletteHighlights()
        {
            foreach (var btn in _shapeBtns)  btn.RemoveFromClassList(ClassPaletteShapeSel);
            foreach (var btn in _colourBtns) btn.RemoveFromClassList(ClassPaletteColourSel);
        }

        private void HighlightShapeBtn(int idx)
        {
            for (int i = 0; i < _shapeBtns.Length; i++)
            {
                if (i == idx) _shapeBtns[i].AddToClassList(ClassPaletteShapeSel);
                else          _shapeBtns[i].RemoveFromClassList(ClassPaletteShapeSel);
            }
        }

        private void HighlightColourBtn(int idx)
        {
            for (int i = 0; i < _colourBtns.Length; i++)
            {
                if (i == idx) _colourBtns[i].AddToClassList(ClassPaletteColourSel);
                else          _colourBtns[i].RemoveFromClassList(ClassPaletteColourSel);
            }
        }
    }
}

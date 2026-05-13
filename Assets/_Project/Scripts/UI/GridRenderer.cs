using System;
using ChromaLogic.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Builds and owns the 9×9 Vessel grid as UI Toolkit <see cref="VisualElement"/> cells.
    /// <para>
    /// Call <see cref="Initialise"/> once — it creates all 81 cells, applies locked-clue
    /// visuals, and assigns box-border classes. Subsequent state changes go through
    /// <see cref="SetCellCorrect"/>, <see cref="SetCellWrong"/>, and <see cref="ClearCell"/>.
    /// Row/column/box completion is signalled by <see cref="FlashRow"/>,
    /// <see cref="FlashColumn"/>, and <see cref="FlashBox"/>.
    /// </para>
    /// <para>
    /// <see cref="OnCellTapped"/> fires whenever the Curator taps a non-locked cell;
    /// <see cref="SolveController"/> listens and routes the event to
    /// <see cref="TileTrayController"/>.
    /// </para>
    /// </summary>
    public sealed class GridRenderer : MonoBehaviour
    {
        // ── Event ─────────────────────────────────────────────────────────

        /// <summary>Fires when the Curator taps a non-locked cell.</summary>
        public event Action<int, int> OnCellTapped;

        // ── USS class constants ────────────────────────────────────────────

        private const string ClassCell        = "solve-cell";
        private const string ClassLocked      = "solve-cell--locked";
        private const string ClassEmpty       = "solve-cell--empty";
        private const string ClassSelected    = "solve-cell--selected";
        private const string ClassCorrect     = "solve-cell--correct";
        private const string ClassWrong       = "solve-cell--wrong";
        private const string ClassFlash       = "solve-cell--flash";
        private const string ClassBoxLeft     = "solve-cell--box-border-left";
        private const string ClassBoxTop      = "solve-cell--box-border-top";
        private const string ClassShapeLabel  = "solve-cell-label";

        private const int GridSize = 9;

        // ── State ──────────────────────────────────────────────────────────

        private VisualElement[,] _cells;
        private int _selectedRow = -1;
        private int _selectedCol = -1;

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Builds all 81 cells inside <paramref name="solveGrid"/> and populates locked
        /// clue cells from <paramref name="gridData"/>. Must be called once before any
        /// other method.
        /// </summary>
        public void Initialise(VisualElement solveGrid, GridData gridData)
        {
            _cells = new VisualElement[GridSize, GridSize];

            for (int r = 0; r < GridSize; r++)
            for (int c = 0; c < GridSize; c++)
            {
                var cell = new VisualElement();
                cell.AddToClassList(ClassCell);

                // Box-boundary accent borders.
                if (c % 3 == 0) cell.AddToClassList(ClassBoxLeft);
                if (r % 3 == 0) cell.AddToClassList(ClassBoxTop);

                var cellData = gridData.Cells[r, c];
                if (cellData.IsLocked)
                {
                    cell.AddToClassList(ClassLocked);
                    ApplyVisual(cell, cellData.Shape!.Value, cellData.Colour!.Value);
                }
                else
                {
                    cell.AddToClassList(ClassEmpty);
                    int row = r, col = c;
                    cell.RegisterCallback<ClickEvent>(_ => OnCellTapped?.Invoke(row, col));
                }

                _cells[r, c] = cell;
                solveGrid.Add(cell);
            }
        }

        /// <summary>Highlights <paramref name="row"/>,<paramref name="col"/> as selected.
        /// Deselects the previously selected cell.</summary>
        public void SelectCell(int row, int col)
        {
            DeselectAll();
            _selectedRow = row;
            _selectedCol = col;
            _cells[row, col].AddToClassList(ClassSelected);
        }

        /// <summary>Removes the selection highlight from all cells.</summary>
        public void DeselectAll()
        {
            if (_selectedRow >= 0)
                _cells[_selectedRow, _selectedCol].RemoveFromClassList(ClassSelected);
            _selectedRow = -1;
            _selectedCol = -1;
        }

        /// <summary>
        /// Commits a correct placement: applies the colour background and shape symbol,
        /// removes the empty/selected classes, and plays a scale-pop animation.
        /// </summary>
        public void SetCellCorrect(int row, int col, ShapeType shape, ColourType colour)
        {
            var cell = _cells[row, col];
            cell.RemoveFromClassList(ClassSelected);
            cell.RemoveFromClassList(ClassEmpty);
            cell.AddToClassList(ClassCorrect);
            ApplyVisual(cell, shape, colour);
            AnimateSuccess(cell);
        }

        /// <summary>
        /// Flashes an amber wrong-attempt indicator on the cell without changing its
        /// contents (wrong placements are not written to the board).
        /// </summary>
        public void SetCellWrong(int row, int col)
        {
            var cell = _cells[row, col];
            cell.AddToClassList(ClassWrong);
            DOVirtual.DelayedCall(0.45f, () => cell.RemoveFromClassList(ClassWrong));
        }

        /// <summary>
        /// Removes a Curator-placed value from a cell and restores the empty visual state.
        /// </summary>
        public void ClearCell(int row, int col)
        {
            var cell = _cells[row, col];
            cell.RemoveFromClassList(ClassCorrect);
            cell.RemoveFromClassList(ClassSelected);

            foreach (string suf in TutorialGridData.AllColourSuffix)
                cell.RemoveFromClassList("solve-cell--" + suf);

            var lbl = cell.Q<Label>(className: ClassShapeLabel);
            lbl?.RemoveFromHierarchy();

            cell.AddToClassList(ClassEmpty);
        }

        /// <summary>Plays the gold structure-completion flash on all cells in <paramref name="row"/>.</summary>
        public void FlashRow(int row)
        {
            for (int c = 0; c < GridSize; c++) FlashCell(_cells[row, c]);
        }

        /// <summary>Plays the gold structure-completion flash on all cells in <paramref name="col"/>.</summary>
        public void FlashColumn(int col)
        {
            for (int r = 0; r < GridSize; r++) FlashCell(_cells[r, col]);
        }

        /// <summary>Plays the gold structure-completion flash on all 9 cells in
        /// the 3×3 box identified by <paramref name="boxIndex"/> (0–8, row-major).</summary>
        public void FlashBox(int boxIndex)
        {
            int startRow = (boxIndex / 3) * 3;
            int startCol = (boxIndex % 3) * 3;
            for (int r = startRow; r < startRow + 3; r++)
            for (int c = startCol; c < startCol + 3; c++)
                FlashCell(_cells[r, c]);
        }

        // ── Private helpers ────────────────────────────────────────────────

        private void ApplyVisual(VisualElement cell, ShapeType shape, ColourType colour)
        {
            foreach (string suf in TutorialGridData.AllColourSuffix)
                cell.RemoveFromClassList("solve-cell--" + suf);

            cell.AddToClassList("solve-cell--" + TutorialGridData.AllColourSuffix[(int)colour]);

            var old = cell.Q<Label>(className: ClassShapeLabel);
            old?.RemoveFromHierarchy();

            var lbl = new Label(TutorialGridData.AllShapeSymbols[(int)shape]);
            lbl.AddToClassList(ClassShapeLabel);
            cell.Add(lbl);
        }

        private static void FlashCell(VisualElement cell)
        {
            cell.AddToClassList(ClassFlash);
            DOVirtual.DelayedCall(0.65f, () => cell.RemoveFromClassList(ClassFlash));
        }

        private static void AnimateSuccess(VisualElement cell)
        {
            DOVirtual.Float(1f, 1.1f, 0.09f,
                v => cell.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f))))
            .OnComplete(() =>
                DOVirtual.Float(1.1f, 1f, 0.14f,
                    v => cell.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f)))));
        }
    }
}

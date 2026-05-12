using System;

namespace ChromaLogic.Core
{
    /// <summary>
    /// Outcome of a <see cref="GridData.TryPlaceValue"/> call.
    /// Lets callers distinguish a structural error (locked/out-of-bounds) from a
    /// wrong answer from the Curator, without relying on exceptions or out-parameters.
    /// </summary>
    public enum PlaceResult
    {
        /// <summary>Cell is locked, already filled, or coordinates are out of bounds.</summary>
        Invalid,

        /// <summary>
        /// Value matches the solution on both axes. Written to the board.
        /// Completion events fire if the move finishes a row, column, or box.
        /// </summary>
        Correct,

        /// <summary>
        /// Value does not match the solution. <em>Not</em> written to the board.
        /// Error count incremented; <see cref="GridData.OnSafetyNetTriggered"/> fires
        /// on the third error (once only per Vessel).
        /// </summary>
        Incorrect
    }

    /// <summary>
    /// Represents the live state of one cell in the 9×9 Vessel board.
    /// Shape and colour are written together — a cell is always either fully filled
    /// or fully empty.
    /// </summary>
    public sealed class Cell
    {
        /// <summary>Current shape value, or <c>null</c> if the cell is empty.</summary>
        public ShapeType? Shape { get; internal set; }

        /// <summary>Current colour value, or <c>null</c> if the cell is empty.</summary>
        public ColourType? Colour { get; internal set; }

        /// <summary>
        /// <c>true</c> when this cell was pre-filled as a clue by the puzzle generator.
        /// Locked cells cannot be modified or cleared by the Curator.
        /// </summary>
        public bool IsLocked { get; internal set; }

        /// <summary>
        /// <c>true</c> once the row, column, or 3×3 box that contains this cell has
        /// completed and its Visual Flash has triggered. Set externally by the Reveal
        /// system via <see cref="GridData.MarkRowRevealed"/>,
        /// <see cref="GridData.MarkColumnRevealed"/>, or
        /// <see cref="GridData.MarkBoxRevealed"/>.
        /// </summary>
        public bool IsRevealed { get; internal set; }

        /// <summary><c>true</c> when neither axis carries a value.</summary>
        public bool IsEmpty => !Shape.HasValue;

        internal Cell() { }
    }

    /// <summary>
    /// Live game-board layer for a Chroma-Logic Gallery Vessel.
    /// <para>
    /// Wraps the 9×9 <see cref="Cell"/> grid, routes Curator moves against the
    /// solution held by <see cref="SudokuSolver"/>, tracks the Safety Net error
    /// budget, and publishes C# events whenever a row, column, 3×3 box, or the
    /// full grid reaches completion.
    /// </para>
    /// <para>
    /// This is a pure C# logic class with no <c>MonoBehaviour</c> dependency.
    /// </para>
    /// </summary>
    public sealed class GridData
    {
        // ── Constants ─────────────────────────────────────────────────────
        private const int GridSize  = 9;
        private const int BoxSize   = 3;

        /// <summary>Number of incorrect placements that triggers the Safety Net.</summary>
        public const int MaxErrors = 3;

        // ── State ─────────────────────────────────────────────────────────
        private readonly SudokuSolver _solver;

        // Completion deduplication — prevents an event firing more than once
        // for a structure that was already complete before the current move.
        private readonly bool[] _rowDone = new bool[GridSize];
        private readonly bool[] _colDone = new bool[GridSize];
        private readonly bool[] _boxDone = new bool[GridSize];

        private bool _safetyNetUsed;
        private bool _gridComplete;

        // ── Public surface ────────────────────────────────────────────────

        /// <summary>The 9×9 cell grid. Read cell state directly; modify only through GridData methods.</summary>
        public Cell[,] Cells { get; }

        /// <summary>
        /// Number of incorrect placements made this session (0–<see cref="MaxErrors"/>).
        /// Incremented by <see cref="TryPlaceValue"/> on a wrong answer.
        /// </summary>
        public int ErrorCount { get; private set; }

        // ── C# Events ────────────────────────────────────────────────────

        /// <summary>
        /// Fires once when every cell in <paramref name="row"/> is correctly filled.
        /// Subscribe to trigger the row's Visual Flash.
        /// </summary>
        public event Action<int> OnRowComplete;

        /// <summary>
        /// Fires once when every cell in <paramref name="col"/> is correctly filled.
        /// Subscribe to trigger the column's Visual Flash.
        /// </summary>
        public event Action<int> OnColumnComplete;

        /// <summary>
        /// Fires once when every cell in the 3×3 box identified by
        /// <paramref name="boxIndex"/> (0–8, row-major) is correctly filled.
        /// Subscribe to trigger the box's Visual Flash.
        /// Box origin: row = <c>(boxIndex / 3) * 3</c>, col = <c>(boxIndex % 3) * 3</c>.
        /// </summary>
        public event Action<int> OnBoxComplete;

        /// <summary>
        /// Fires once when all 81 cells are correctly filled.
        /// Subscribe to begin The Reveal sequence.
        /// </summary>
        public event Action OnGridComplete;

        /// <summary>
        /// Fires each time <see cref="ErrorCount"/> changes.
        /// The new count is passed as the argument.
        /// </summary>
        public event Action<int> OnErrorCountChanged;

        /// <summary>
        /// Fires exactly once per Vessel session when <see cref="ErrorCount"/>
        /// reaches <see cref="MaxErrors"/>. Further errors do not re-fire this event.
        /// Per design rules the Safety Net may only trigger once per puzzle session.
        /// </summary>
        public event Action OnSafetyNetTriggered;

        // ── Constructor ───────────────────────────────────────────────────

        /// <summary>
        /// Initialises the board from a fully generated <see cref="SudokuSolver"/>.
        /// Copies the current puzzle state (pre-filled clues) into the <see cref="Cells"/>
        /// grid and locks those cells against Curator edits.
        /// </summary>
        /// <param name="solver">
        /// A <see cref="SudokuSolver"/> on which <c>GeneratePuzzle</c> has already
        /// been called.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="solver"/> is <c>null</c>.
        /// </exception>
        public GridData(SudokuSolver solver)
        {
            _solver = solver ?? throw new ArgumentNullException(nameof(solver));

            Cells = new Cell[GridSize, GridSize];
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    PuzzleCell snapshot = solver.GetCell(r, c);
                    Cells[r, c] = new Cell
                    {
                        Shape      = snapshot.Shape,
                        Colour     = snapshot.Colour,
                        IsLocked   = snapshot.IsPreFilled,
                        IsRevealed = false
                    };
                }
            }

            // Guards against the edge case where pre-filled clues already complete a structure,
            // preventing those completion events from re-firing on the first Curator placement.
            SeedCompletionState();
        }

        // ── Curator interaction ───────────────────────────────────────────

        /// <summary>
        /// Attempts to place <paramref name="shape"/> and <paramref name="colour"/>
        /// at (<paramref name="row"/>, <paramref name="col"/>).
        /// <list type="bullet">
        ///   <item><description>
        ///     <see cref="PlaceResult.Invalid"/> — cell is locked, already filled,
        ///     or coordinates are out of bounds. Board is unchanged.
        ///   </description></item>
        ///   <item><description>
        ///     <see cref="PlaceResult.Correct"/> — values match the solution on both
        ///     axes. Written to the board. Completion events fire as appropriate.
        ///   </description></item>
        ///   <item><description>
        ///     <see cref="PlaceResult.Incorrect"/> — values do not match the solution.
        ///     Not written to the board. <see cref="ErrorCount"/> is incremented and
        ///     <see cref="OnErrorCountChanged"/> fires. If this is the third error,
        ///     <see cref="OnSafetyNetTriggered"/> also fires (once only).
        ///   </description></item>
        /// </list>
        /// </summary>
        /// <param name="row">Zero-based row index (0–8).</param>
        /// <param name="col">Zero-based column index (0–8).</param>
        /// <param name="shape">Shape value to place.</param>
        /// <param name="colour">Colour value to place.</param>
        public PlaceResult TryPlaceValue(int row, int col, ShapeType shape, ColourType colour)
        {
            if (!IsInBounds(row, col))            return PlaceResult.Invalid;
            if (Cells[row, col].IsLocked)         return PlaceResult.Invalid;
            if (!Cells[row, col].IsEmpty)         return PlaceResult.Invalid;

            (ShapeType solShape, ColourType solColour) = _solver.GetSolutionCell(row, col);

            if (shape != solShape || colour != solColour)
            {
                ErrorCount++;
                OnErrorCountChanged?.Invoke(ErrorCount);

                if (ErrorCount >= MaxErrors && !_safetyNetUsed)
                {
                    _safetyNetUsed = true;
                    OnSafetyNetTriggered?.Invoke();
                }

                return PlaceResult.Incorrect;
            }

            Cells[row, col].Shape  = shape;
            Cells[row, col].Colour = colour;

            CheckCompletions(row, col);
            return PlaceResult.Correct;
        }

        /// <summary>
        /// Removes a Curator-placed value from (<paramref name="row"/>, <paramref name="col"/>).
        /// Locked cells and already-empty cells cannot be cleared.
        /// Error count is not affected.
        /// </summary>
        /// <param name="row">Zero-based row index (0–8).</param>
        /// <param name="col">Zero-based column index (0–8).</param>
        /// <returns>
        /// <c>true</c> if the cell was cleared; <c>false</c> if it is locked, empty,
        /// or out of bounds.
        /// </returns>
        public bool TryClearCell(int row, int col)
        {
            if (!IsInBounds(row, col))    return false;
            if (Cells[row, col].IsLocked) return false;
            if (Cells[row, col].IsEmpty)  return false;

            Cells[row, col].Shape  = null;
            Cells[row, col].Colour = null;
            return true;
        }

        // ── Reveal marking ────────────────────────────────────────────────

        /// <summary>
        /// Marks every cell in <paramref name="row"/> as <see cref="Cell.IsRevealed"/>.
        /// Called by the Visual Flash system after <see cref="OnRowComplete"/> fires.
        /// </summary>
        /// <param name="row">Zero-based row index (0–8).</param>
        public void MarkRowRevealed(int row)
        {
            if (!IsInBounds(row, 0)) return;
            for (int c = 0; c < GridSize; c++)
                Cells[row, c].IsRevealed = true;
        }

        /// <summary>
        /// Marks every cell in <paramref name="col"/> as <see cref="Cell.IsRevealed"/>.
        /// Called by the Visual Flash system after <see cref="OnColumnComplete"/> fires.
        /// </summary>
        /// <param name="col">Zero-based column index (0–8).</param>
        public void MarkColumnRevealed(int col)
        {
            if (!IsInBounds(0, col)) return;
            for (int r = 0; r < GridSize; r++)
                Cells[r, col].IsRevealed = true;
        }

        /// <summary>
        /// Marks every cell in the 3×3 box identified by <paramref name="boxIndex"/>
        /// as <see cref="Cell.IsRevealed"/>.
        /// Called by the Visual Flash system after <see cref="OnBoxComplete"/> fires.
        /// Box index is row-major (0 = top-left, 8 = bottom-right).
        /// </summary>
        /// <param name="boxIndex">Box index in the range 0–8.</param>
        public void MarkBoxRevealed(int boxIndex)
        {
            if ((uint)boxIndex >= GridSize) return;
            int startRow = (boxIndex / BoxSize) * BoxSize;
            int startCol = (boxIndex % BoxSize) * BoxSize;
            for (int r = startRow; r < startRow + BoxSize; r++)
                for (int c = startCol; c < startCol + BoxSize; c++)
                    Cells[r, c].IsRevealed = true;
        }

        // ── Completion queries ────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when all 9 cells in <paramref name="row"/> are filled.
        /// Because <see cref="TryPlaceValue"/> only writes correct values, filled
        /// implies correct.
        /// </summary>
        /// <param name="row">Zero-based row index (0–8).</param>
        public bool IsRowComplete(int row)
        {
            if (!IsInBounds(row, 0)) return false;
            for (int c = 0; c < GridSize; c++)
                if (Cells[row, c].IsEmpty) return false;
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> when all 9 cells in <paramref name="col"/> are filled.
        /// </summary>
        /// <param name="col">Zero-based column index (0–8).</param>
        public bool IsColumnComplete(int col)
        {
            if (!IsInBounds(0, col)) return false;
            for (int r = 0; r < GridSize; r++)
                if (Cells[r, col].IsEmpty) return false;
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> when all 9 cells in the 3×3 box identified by
        /// <paramref name="boxIndex"/> are filled.
        /// </summary>
        /// <param name="boxIndex">Box index in the range 0–8 (row-major).</param>
        public bool IsBoxComplete(int boxIndex)
        {
            if ((uint)boxIndex >= GridSize) return false;
            int startRow = (boxIndex / BoxSize) * BoxSize;
            int startCol = (boxIndex % BoxSize) * BoxSize;
            for (int r = startRow; r < startRow + BoxSize; r++)
                for (int c = startCol; c < startCol + BoxSize; c++)
                    if (Cells[r, c].IsEmpty) return false;
            return true;
        }

        // ── Private helpers ───────────────────────────────────────────────

        // Called once from the constructor to mark any structures that are already
        // fully filled by clue cells so their events don't double-fire later.
        private void SeedCompletionState()
        {
            for (int i = 0; i < GridSize; i++)
            {
                if (IsRowComplete(i))    _rowDone[i] = true;
                if (IsColumnComplete(i)) _colDone[i] = true;
                if (IsBoxComplete(i))    _boxDone[i] = true;
            }
        }

        // Runs after every correct placement: fires completion events for the
        // row, column, and box affected by (row, col), then checks the full grid.
        private void CheckCompletions(int row, int col)
        {
            if (!_rowDone[row] && IsRowComplete(row))
            {
                _rowDone[row] = true;
                OnRowComplete?.Invoke(row);
            }

            if (!_colDone[col] && IsColumnComplete(col))
            {
                _colDone[col] = true;
                OnColumnComplete?.Invoke(col);
            }

            int boxIndex = (row / BoxSize) * BoxSize + (col / BoxSize);
            if (!_boxDone[boxIndex] && IsBoxComplete(boxIndex))
            {
                _boxDone[boxIndex] = true;
                OnBoxComplete?.Invoke(boxIndex);
            }

            if (!_gridComplete && IsFullGridComplete())
            {
                _gridComplete = true;
                OnGridComplete?.Invoke();
            }
        }

        // Write path only places correct values, so filled implies correct.
        private bool IsFullGridComplete()
        {
            for (int i = 0; i < GridSize; i++)
                if (!_rowDone[i]) return false;
            return true;
        }

        private static bool IsInBounds(int row, int col) =>
            (uint)row < GridSize && (uint)col < GridSize;
    }
}

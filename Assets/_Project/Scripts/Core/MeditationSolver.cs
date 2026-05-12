using System;
using System.Collections.Generic;
using ChromaLogic.Managers;

namespace ChromaLogic.Core
{
    /// <summary>
    /// Pure C# 4×4 dual-axis Sudoku engine for the Daily Meditation mode (Phase 9).
    /// <para>
    /// Operates on the Curator's four chosen <see cref="ShapeType"/> values and four
    /// chosen <see cref="ColourType"/> values stored in <see cref="PlayerDataManager"/>.
    /// Both sets are captured at <see cref="GeneratePuzzle"/> call time, so changes made
    /// in Aesthetic Calibration (Phase 10) take effect on the next generated Vessel.
    /// </para>
    /// <para>
    /// The grid is 4×4 with four 2×2 boxes. Each axis independently satisfies standard
    /// Sudoku constraints — every value appears exactly once per row, column, and box.
    /// A unique solution is guaranteed for both axes.
    /// </para>
    /// <para>
    /// Single difficulty tier: 6–8 pre-filled clue cells out of 16, delivering a calm
    /// Meditative experience without timers or frustration.
    /// This is a pure C# logic class with no <c>MonoBehaviour</c> dependency.
    /// </para>
    /// </summary>
    public sealed class MeditationSolver
    {
        // ── Grid constants ─────────────────────────────────────────────────
        private const int GridSize = 4;
        private const int BoxSize  = 2;

        // ── Difficulty constants ───────────────────────────────────────────
        private const int PrefillMin = 6;
        private const int PrefillMax = 8;

        // ── Generation retry limit ─────────────────────────────────────────
        private const int MaxGenerationRetries = 64;

        // ── State ──────────────────────────────────────────────────────────
        private readonly Random _rng;

        // Captured from PlayerDataManager at GeneratePuzzle() call time.
        private ShapeType[]  _shapeValues  = Array.Empty<ShapeType>();
        private ColourType[] _colourValues = Array.Empty<ColourType>();

        private readonly ShapeType[,]   _solutionShapes  = new ShapeType[GridSize, GridSize];
        private readonly ColourType[,]  _solutionColours = new ColourType[GridSize, GridSize];
        private readonly ShapeType?[,]  _currentShapes   = new ShapeType?[GridSize, GridSize];
        private readonly ColourType?[,] _currentColours  = new ColourType?[GridSize, GridSize];
        private readonly bool[,]        _isPreFilled     = new bool[GridSize, GridSize];

        private bool _puzzleReady;

        // ── Constructors ───────────────────────────────────────────────────

        /// <summary>Creates a new <see cref="MeditationSolver"/> seeded from the system tick count.</summary>
        public MeditationSolver() : this(Environment.TickCount) { }

        /// <summary>Creates a new <see cref="MeditationSolver"/> with a deterministic seed.</summary>
        /// <param name="seed">RNG seed. Identical seeds with identical <see cref="PlayerDataManager"/>
        /// configuration produce identical puzzles.</param>
        public MeditationSolver(int seed) => _rng = new Random(seed);

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Generates a new 4×4 Meditation Vessel. Reads the Curator's chosen shapes and
        /// colours from <see cref="PlayerDataManager.MeditationShapes"/> and
        /// <see cref="PlayerDataManager.MeditationColours"/> at call time.
        /// The resulting puzzle has a unique solution and 6–8 pre-filled clue cells.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="PlayerDataManager.Instance"/> is not yet initialised, or if
        /// puzzle generation fails after <c>MaxGenerationRetries</c> attempts.
        /// </exception>
        public void GeneratePuzzle()
        {
            PlayerDataManager pdm = PlayerDataManager.Instance
                ?? throw new InvalidOperationException(
                    "PlayerDataManager.Instance is null. " +
                    "Ensure PlayerDataManager is initialised before calling GeneratePuzzle.");

            _shapeValues  = pdm.MeditationShapes.ToArray();
            _colourValues = pdm.MeditationColours.ToArray();

            int prefillCount = _rng.Next(PrefillMin, PrefillMax + 1);

            for (int attempt = 0; attempt < MaxGenerationRetries; attempt++)
            {
                int[,] shapeGrid  = BuildFilledGrid();
                int[,] colourGrid = BuildFilledGrid();

                // Store the complete solution before carving clues in-place.
                StoreSolution(shapeGrid, colourGrid);

                if (!TryApplyPuzzleClues(shapeGrid, colourGrid, prefillCount)) continue;

                ApplyClueState(shapeGrid, colourGrid);
                _puzzleReady = true;
                return;
            }

            throw new InvalidOperationException(
                $"MeditationSolver failed to generate a valid puzzle after {MaxGenerationRetries} attempts.");
        }

        /// <summary>
        /// Returns a snapshot of the cell at (<paramref name="row"/>, <paramref name="col"/>)
        /// in the current working puzzle state.
        /// </summary>
        /// <param name="row">Zero-based row index (0–3).</param>
        /// <param name="col">Zero-based column index (0–3).</param>
        /// <returns>
        /// A <see cref="PuzzleCell"/> reflecting the current value and lock state.
        /// Empty cells return a default <see cref="PuzzleCell"/> where
        /// <see cref="PuzzleCell.IsEmpty"/> is <c>true</c>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="GeneratePuzzle"/> has not been called.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when row or col is outside 0–3.
        /// </exception>
        public PuzzleCell GetCell(int row, int col)
        {
            EnsurePuzzleReady();
            if (!IsInBounds(row, col))
                throw new ArgumentOutOfRangeException(
                    $"({row},{col}) is out of bounds for a {GridSize}×{GridSize} grid.");

            ShapeType? shape = _currentShapes[row, col];
            if (!shape.HasValue) return default;
            return new PuzzleCell(shape.Value, _currentColours[row, col]!.Value, _isPreFilled[row, col]);
        }

        /// <summary>
        /// Returns the solution shape and colour for the cell at
        /// (<paramref name="row"/>, <paramref name="col"/>).
        /// </summary>
        /// <param name="row">Zero-based row index (0–3).</param>
        /// <param name="col">Zero-based column index (0–3).</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="GeneratePuzzle"/> has not been called.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when row or col is outside 0–3.
        /// </exception>
        public (ShapeType Shape, ColourType Colour) GetSolutionCell(int row, int col)
        {
            EnsurePuzzleReady();
            if (!IsInBounds(row, col))
                throw new ArgumentOutOfRangeException(
                    $"({row},{col}) is out of bounds for a {GridSize}×{GridSize} grid.");
            return (_solutionShapes[row, col], _solutionColours[row, col]);
        }

        /// <summary>
        /// Attempts to place <paramref name="shape"/> and <paramref name="colour"/> at
        /// (<paramref name="row"/>, <paramref name="col"/>). Writes to the board only
        /// when both axes match the solution and the cell is empty and unlocked.
        /// </summary>
        /// <param name="row">Zero-based row index (0–3).</param>
        /// <param name="col">Zero-based column index (0–3).</param>
        /// <param name="shape">Shape value to place.</param>
        /// <param name="colour">Colour value to place.</param>
        /// <returns>
        /// <c>true</c> if the values matched the solution and were written to the board;
        /// <c>false</c> if the cell is locked, already filled, out of bounds, or either
        /// axis does not match the solution.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="GeneratePuzzle"/> has not been called.
        /// </exception>
        public bool TryPlaceValue(int row, int col, ShapeType shape, ColourType colour)
        {
            EnsurePuzzleReady();
            if (!IsInBounds(row, col))             return false;
            if (_isPreFilled[row, col])            return false;
            if (_currentShapes[row, col].HasValue) return false;

            if (shape  != _solutionShapes[row, col])  return false;
            if (colour != _solutionColours[row, col]) return false;

            _currentShapes[row, col]  = shape;
            _currentColours[row, col] = colour;
            return true;
        }

        /// <summary>
        /// Removes the Curator-placed value from (<paramref name="row"/>, <paramref name="col"/>).
        /// Locked cells and already-empty cells cannot be cleared.
        /// </summary>
        /// <param name="row">Zero-based row index (0–3).</param>
        /// <param name="col">Zero-based column index (0–3).</param>
        /// <returns>
        /// <c>true</c> if the cell was cleared; <c>false</c> if it is locked, empty,
        /// or out of bounds.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="GeneratePuzzle"/> has not been called.
        /// </exception>
        public bool TryClearCell(int row, int col)
        {
            EnsurePuzzleReady();
            if (!IsInBounds(row, col))              return false;
            if (_isPreFilled[row, col])             return false;
            if (!_currentShapes[row, col].HasValue) return false;

            _currentShapes[row, col]  = null;
            _currentColours[row, col] = null;
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> if placing <paramref name="shape"/> and
        /// <paramref name="colour"/> at the given cell would satisfy row, column, and
        /// 2×2 box constraints against the current working state.
        /// Does not validate against the solution — intended for UI highlighting only.
        /// </summary>
        /// <param name="row">Zero-based row index (0–3).</param>
        /// <param name="col">Zero-based column index (0–3).</param>
        /// <param name="shape">Shape value to test.</param>
        /// <param name="colour">Colour value to test.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="GeneratePuzzle"/> has not been called.
        /// </exception>
        public bool IsValidPlacement(int row, int col, ShapeType shape, ColourType colour)
        {
            EnsurePuzzleReady();
            if (!IsInBounds(row, col))             return false;
            if (_isPreFilled[row, col])            return false;
            if (_currentShapes[row, col].HasValue) return false;

            return IsShapeValidInState(row, col, shape) &&
                   IsColourValidInState(row, col, colour);
        }

        /// <summary>
        /// Returns a <see cref="HintCell"/> pointing to one unfilled cell and its
        /// correct solution values, or <c>null</c> when all 16 cells are filled.
        /// Powers The Insight mechanic for Meditation Vessels.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="GeneratePuzzle"/> has not been called.
        /// </exception>
        public HintCell? GetHint()
        {
            EnsurePuzzleReady();
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    if (!_currentShapes[r, c].HasValue)
                        return new HintCell(r, c, _solutionShapes[r, c], _solutionColours[r, c]);
            return null;
        }

        /// <summary>Returns <c>true</c> when all 16 cells have been correctly filled.</summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="GeneratePuzzle"/> has not been called.
        /// </exception>
        public bool IsSolved()
        {
            EnsurePuzzleReady();
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    if (!_currentShapes[r, c].HasValue) return false;
            return true;
        }

        // ── Private helpers ────────────────────────────────────────────────

        // Builds a complete, valid 4×4 grid using backtracking with Fisher-Yates shuffle.
        // Values 0–3 are indices into _shapeValues / _colourValues.
        private int[,] BuildFilledGrid()
        {
            var grid = new int[GridSize, GridSize];
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    grid[r, c] = -1;
            BacktrackFill(grid);
            return grid;
        }

        private bool BacktrackFill(int[,] grid)
        {
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (grid[r, c] != -1) continue;

                    int[] order = ShuffledIndices();
                    foreach (int val in order)
                    {
                        if (!IsIndexValidInGrid(grid, r, c, val)) continue;
                        grid[r, c] = val;
                        if (BacktrackFill(grid)) return true;
                        grid[r, c] = -1;
                    }
                    return false;
                }
            }
            return true;
        }

        // Carves cells from both grids in a shuffled order, restoring any removal that
        // would break the unique-solution property on either axis.
        // Returns true when exactly (GridSize*GridSize - prefillCount) cells have been removed.
        private bool TryApplyPuzzleClues(int[,] shapeGrid, int[,] colourGrid, int prefillCount)
        {
            int cellsToRemove = GridSize * GridSize - prefillCount;

            var positions = new List<(int r, int c)>(GridSize * GridSize);
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    positions.Add((r, c));
            Shuffle(positions);

            int removed = 0;
            foreach ((int r, int c) in positions)
            {
                if (removed >= cellsToRemove) break;

                int sv = shapeGrid[r, c];
                int cv = colourGrid[r, c];
                shapeGrid[r, c]  = -1;
                colourGrid[r, c] = -1;

                if (CountSolutions(shapeGrid) == 1 && CountSolutions(colourGrid) == 1)
                {
                    removed++;
                }
                else
                {
                    shapeGrid[r, c]  = sv;
                    colourGrid[r, c] = cv;
                }
            }

            return removed >= cellsToRemove;
        }

        // Stores fully-filled grids as the canonical solution. Must be called before
        // TryApplyPuzzleClues modifies the grids in-place.
        private void StoreSolution(int[,] shapeGrid, int[,] colourGrid)
        {
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                {
                    _solutionShapes[r, c]  = _shapeValues[shapeGrid[r, c]];
                    _solutionColours[r, c] = _colourValues[colourGrid[r, c]];
                }
        }

        // Populates working state from the carved clue grids.
        // Cells with value != -1 are locked pre-filled clues; -1 cells start empty.
        private void ApplyClueState(int[,] shapeGrid, int[,] colourGrid)
        {
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    bool filled           = shapeGrid[r, c] != -1;
                    _isPreFilled[r, c]    = filled;
                    _currentShapes[r, c]  = filled ? _shapeValues[shapeGrid[r, c]]   : (ShapeType?)null;
                    _currentColours[r, c] = filled ? _colourValues[colourGrid[r, c]] : (ColourType?)null;
                }
            }
        }

        // Counts solutions for a partial grid up to maxCount, operating on a clone
        // so the caller's state is never modified.
        private int CountSolutions(int[,] grid, int maxCount = 2)
        {
            int[,] copy = (int[,])grid.Clone();
            int count = 0;
            CountSolutionsRecursive(copy, ref count, maxCount);
            return count;
        }

        private void CountSolutionsRecursive(int[,] grid, ref int count, int maxCount)
        {
            if (count >= maxCount) return;

            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (grid[r, c] != -1) continue;

                    for (int val = 0; val < GridSize; val++)
                    {
                        if (!IsIndexValidInGrid(grid, r, c, val)) continue;
                        grid[r, c] = val;
                        CountSolutionsRecursive(grid, ref count, maxCount);
                        grid[r, c] = -1;
                        if (count >= maxCount) return;
                    }
                    return;
                }
            }
            count++;
        }

        // Returns true when val (0–3) can legally occupy (row, col) in grid given
        // current row, column, and 2×2 box constraints. -1 represents an empty cell.
        private static bool IsIndexValidInGrid(int[,] grid, int row, int col, int val)
        {
            for (int c = 0; c < GridSize; c++)
                if (c != col && grid[row, c] == val) return false;

            for (int r = 0; r < GridSize; r++)
                if (r != row && grid[r, col] == val) return false;

            int startRow = (row / BoxSize) * BoxSize;
            int startCol = (col / BoxSize) * BoxSize;
            for (int r = startRow; r < startRow + BoxSize; r++)
                for (int c = startCol; c < startCol + BoxSize; c++)
                    if ((r != row || c != col) && grid[r, c] == val) return false;

            return true;
        }

        // Returns true when shape can legally occupy (row, col) given the current
        // working state in _currentShapes.
        private bool IsShapeValidInState(int row, int col, ShapeType shape)
        {
            for (int c = 0; c < GridSize; c++)
                if (c != col && _currentShapes[row, c] == shape) return false;

            for (int r = 0; r < GridSize; r++)
                if (r != row && _currentShapes[r, col] == shape) return false;

            int startRow = (row / BoxSize) * BoxSize;
            int startCol = (col / BoxSize) * BoxSize;
            for (int r = startRow; r < startRow + BoxSize; r++)
                for (int c = startCol; c < startCol + BoxSize; c++)
                    if ((r != row || c != col) && _currentShapes[r, c] == shape) return false;

            return true;
        }

        private bool IsColourValidInState(int row, int col, ColourType colour)
        {
            for (int c = 0; c < GridSize; c++)
                if (c != col && _currentColours[row, c] == colour) return false;

            for (int r = 0; r < GridSize; r++)
                if (r != row && _currentColours[r, col] == colour) return false;

            int startRow = (row / BoxSize) * BoxSize;
            int startCol = (col / BoxSize) * BoxSize;
            for (int r = startRow; r < startRow + BoxSize; r++)
                for (int c = startCol; c < startCol + BoxSize; c++)
                    if ((r != row || c != col) && _currentColours[r, c] == colour) return false;

            return true;
        }

        // Returns a Fisher-Yates shuffled array of [0, GridSize).
        private int[] ShuffledIndices()
        {
            var indices = new int[GridSize];
            for (int i = 0; i < GridSize; i++) indices[i] = i;
            for (int i = GridSize - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            return indices;
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static bool IsInBounds(int row, int col) =>
            (uint)row < GridSize && (uint)col < GridSize;

        private void EnsurePuzzleReady()
        {
            if (!_puzzleReady)
                throw new InvalidOperationException(
                    "Call GeneratePuzzle() before using any other MeditationSolver method.");
        }
    }
}

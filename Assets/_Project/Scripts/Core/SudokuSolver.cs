// NOTE: ShapeType, ColourType, ClarityLevel, HintCell, and PuzzleCell are defined
// here for Phase 1 bootstrap. Move them to GameTypes.cs when additional core
// systems are added in Phase 1.

using System;
using System.Collections.Generic;

namespace ChromaLogic.Core
{
    /// <summary>Nine gemstone shapes that occupy the shape axis of every puzzle cell.</summary>
    public enum ShapeType
    {
        Infinity, Star, Hexagon, Crescent, Diamond, Plus, Heart, Ring, Triangle
    }

    /// <summary>Nine gemstone colours that occupy the colour axis of every puzzle cell.</summary>
    public enum ColourType
    {
        Crimson, Amber, Jade, Cobalt, Violet, Teal, Rose, Slate, Gold
    }

    /// <summary>
    /// Puzzle difficulty tier expressed as Clarity Level.
    /// Controls how many cells are pre-filled when a Vessel is generated.
    /// Never expose the word "difficulty" in UI strings — always use "Clarity Level".
    /// </summary>
    public enum ClarityLevel
    {
        /// <summary>38–45 cells pre-filled.</summary>
        Sunlit,
        /// <summary>28–35 cells pre-filled.</summary>
        Overcast,
        /// <summary>22–27 cells pre-filled.</summary>
        Moonlit,
        /// <summary>17–21 cells pre-filled.</summary>
        Void
    }

    /// <summary>
    /// The result returned by <see cref="SudokuSolver.GetHint"/>.
    /// Contains the grid coordinates and both correct axis values for one unfilled cell.
    /// This powers The Insight mechanic.
    /// </summary>
    public readonly struct HintCell
    {
        /// <summary>Zero-based row index (0–8).</summary>
        public readonly int Row;

        /// <summary>Zero-based column index (0–8).</summary>
        public readonly int Col;

        /// <summary>Correct shape for this cell per the solution.</summary>
        public readonly ShapeType Shape;

        /// <summary>Correct colour for this cell per the solution.</summary>
        public readonly ColourType Colour;

        internal HintCell(int row, int col, ShapeType shape, ColourType colour)
        {
            Row = row; Col = col; Shape = shape; Colour = colour;
        }
    }

    /// <summary>
    /// Snapshot of a single cell in the working grid.
    /// The default value represents an empty (unfilled) cell.
    /// </summary>
    public readonly struct PuzzleCell
    {
        /// <summary>Current shape value, or <c>null</c> if the cell is empty.</summary>
        public readonly ShapeType? Shape;

        /// <summary>Current colour value, or <c>null</c> if the cell is empty.</summary>
        public readonly ColourType? Colour;

        /// <summary>
        /// <c>true</c> if this cell was set by puzzle generation and may not be modified
        /// by the Curator.
        /// </summary>
        public readonly bool IsPreFilled;

        /// <summary><c>true</c> when neither axis has been filled.</summary>
        public bool IsEmpty => !Shape.HasValue;

        internal PuzzleCell(ShapeType shape, ColourType colour, bool isPreFilled)
        {
            Shape = shape; Colour = colour; IsPreFilled = isPreFilled;
        }
    }

    /// <summary>
    /// Generates and manages a dual-axis Sudoku Vessel.
    /// <para>
    /// Every cell carries two independent Sudoku constraints: one for <see cref="ShapeType"/>
    /// and one for <see cref="ColourType"/>. Each axis must satisfy standard 9×9 Sudoku
    /// rules (unique value per row, column, and 3×3 box) independently of the other.
    /// </para>
    /// <para>
    /// This is a pure C# logic class. It has no <c>MonoBehaviour</c> dependency and
    /// can be instantiated freely from any game system.
    /// </para>
    /// </summary>
    public sealed class SudokuSolver
    {
        // ── Grid dimensions ───────────────────────────────────────────────
        private const int GridSize = 9;
        private const int BoxSize  = 3;

        // ── Pre-fill ranges from the ClarityLevel definitions in CLAUDE.md ─
        private const int SunlitMin   = 38, SunlitMax   = 45;
        private const int OvercastMin = 28, OvercastMax = 35;
        private const int MoonlitMin  = 22, MoonlitMax  = 27;
        private const int VoidMin     = 17, VoidMax     = 21;

        // ── Retry cap for the uniqueness-constrained generation loop ──────
        private const int MaxGenerationRetries = 64;

        // Cached enum value arrays — order matches enum declaration order.
        private static readonly ShapeType[]  ShapeValues  = (ShapeType[]) Enum.GetValues(typeof(ShapeType));
        private static readonly ColourType[] ColourValues = (ColourType[])Enum.GetValues(typeof(ColourType));

        // ── Solution grids (ground truth, never shown directly to Curator) ─
        private readonly ShapeType[,]  _solutionShapes  = new ShapeType[GridSize,  GridSize];
        private readonly ColourType[,] _solutionColours = new ColourType[GridSize, GridSize];

        // ── Working state — null means the Curator has not filled the cell ─
        private readonly ShapeType?[,]  _currentShapes  = new ShapeType?[GridSize,  GridSize];
        private readonly ColourType?[,] _currentColours = new ColourType?[GridSize, GridSize];

        // ── Pre-filled cells are locked and cannot be overwritten ─────────
        private readonly bool[,] _isPreFilled = new bool[GridSize, GridSize];

        private readonly Random _rng;
        private bool _puzzleReady;

        // ── Constructors ──────────────────────────────────────────────────

        /// <summary>
        /// Initialises the solver with a seed derived from the system clock.
        /// Each call produces a different sequence of puzzles.
        /// </summary>
        public SudokuSolver() : this(Environment.TickCount) { }

        /// <summary>
        /// Initialises the solver with a fixed seed for reproducible puzzles.
        /// Identical seeds produce identical puzzle sequences.
        /// </summary>
        /// <param name="seed">Seed value for the internal random number generator.</param>
        public SudokuSolver(int seed) => _rng = new Random(seed);

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Generates a new dual-axis Sudoku Vessel at the requested Clarity Level.
        /// Builds two independent valid 9×9 Sudoku solutions (one per axis), removes
        /// cells to reach the pre-fill target for <paramref name="clarity"/>, then
        /// verifies that each axis sub-puzzle has a unique solution before committing.
        /// Retries internally up to <c>64</c> times if uniqueness cannot be guaranteed.
        /// </summary>
        /// <param name="clarity">
        /// Clarity Level that controls how many cells are pre-filled:
        /// Sunlit 38–45 · Overcast 28–35 · Moonlit 22–27 · Void 17–21.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a uniquely-solvable puzzle cannot be produced within the retry limit.
        /// </exception>
        public void GeneratePuzzle(ClarityLevel clarity)
        {
            int prefillCount = PrefillCountFor(clarity);

            for (int attempt = 0; attempt < MaxGenerationRetries; attempt++)
            {
                int[,] shapeGrid  = BuildFilledGrid();
                int[,] colourGrid = BuildFilledGrid();

                if (!TryApplyPuzzleClues(shapeGrid, colourGrid, prefillCount))
                    continue;

                StoreSolution(shapeGrid, colourGrid);
                _puzzleReady = true;
                return;
            }

            throw new InvalidOperationException(
                $"Failed to generate a unique {clarity} puzzle after {MaxGenerationRetries} attempts.");
        }

        /// <summary>
        /// Determines whether placing <paramref name="shape"/> and <paramref name="colour"/>
        /// at (<paramref name="row"/>, <paramref name="col"/>) is structurally valid
        /// against the current working state of the Vessel.
        /// <para>
        /// Both axes are evaluated independently: shape must not duplicate any shape
        /// in the same row, column, or 3×3 box; colour must satisfy the same constraint.
        /// A placement is invalid if the cell is pre-filled or already occupied.
        /// </para>
        /// </summary>
        /// <param name="row">Zero-based row index (0–8).</param>
        /// <param name="col">Zero-based column index (0–8).</param>
        /// <param name="shape">Shape value to test on the shape axis.</param>
        /// <param name="colour">Colour value to test on the colour axis.</param>
        /// <returns>
        /// <c>true</c> if both axes are conflict-free in the current board state;
        /// <c>false</c> if the cell is locked, already filled, out of bounds, or
        /// either axis conflicts with an existing value.
        /// </returns>
        public bool IsValidPlacement(int row, int col, ShapeType shape, ColourType colour)
        {
            EnsurePuzzleReady();
            if (!IsInBounds(row, col))          return false;
            if (_isPreFilled[row, col])          return false;
            if (_currentShapes[row, col].HasValue) return false;

            return IsShapeValidInState(row, col, shape)
                && IsColourValidInState(row, col, colour);
        }

        /// <summary>
        /// Places <paramref name="shape"/> and <paramref name="colour"/> into the
        /// working grid at (<paramref name="row"/>, <paramref name="col"/>) if the
        /// placement passes <see cref="IsValidPlacement"/>.
        /// Both axes are always written together — a cell is either fully filled or empty.
        /// </summary>
        /// <param name="row">Zero-based row index (0–8).</param>
        /// <param name="col">Zero-based column index (0–8).</param>
        /// <param name="shape">Shape value to place.</param>
        /// <param name="colour">Colour value to place.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if the placement is invalid.</returns>
        public bool TryPlaceValue(int row, int col, ShapeType shape, ColourType colour)
        {
            if (!IsValidPlacement(row, col, shape, colour)) return false;
            _currentShapes[row, col]  = shape;
            _currentColours[row, col] = colour;
            return true;
        }

        /// <summary>
        /// Removes a Curator-placed value from (<paramref name="row"/>, <paramref name="col"/>).
        /// Pre-filled cells and already-empty cells cannot be cleared.
        /// </summary>
        /// <param name="row">Zero-based row index (0–8).</param>
        /// <param name="col">Zero-based column index (0–8).</param>
        /// <returns><c>true</c> if the cell was cleared; <c>false</c> otherwise.</returns>
        public bool TryClearCell(int row, int col)
        {
            EnsurePuzzleReady();
            if (!IsInBounds(row, col))              return false;
            if (_isPreFilled[row, col])              return false;
            if (!_currentShapes[row, col].HasValue)  return false;

            _currentShapes[row, col]  = null;
            _currentColours[row, col] = null;
            return true;
        }

        /// <summary>
        /// Returns a snapshot of the current state of a single cell.
        /// The default <see cref="PuzzleCell"/> value (both axes <c>null</c>) represents an empty cell.
        /// </summary>
        /// <param name="row">Zero-based row index (0–8).</param>
        /// <param name="col">Zero-based column index (0–8).</param>
        /// <returns>
        /// A <see cref="PuzzleCell"/> with the current shape, colour, and pre-fill flag,
        /// or the default empty value if the cell has not been filled.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="row"/> or <paramref name="col"/> is outside 0–8.
        /// </exception>
        public PuzzleCell GetCell(int row, int col)
        {
            EnsurePuzzleReady();
            if (!IsInBounds(row, col))
                throw new ArgumentOutOfRangeException($"({row},{col}) is outside the 9×9 grid.");

            if (!_currentShapes[row, col].HasValue) return default;

            return new PuzzleCell(
                _currentShapes[row, col]!.Value,
                _currentColours[row, col]!.Value,
                _isPreFilled[row, col]);
        }

        /// <summary>
        /// Returns the correct shape and colour for a cell directly from the solution.
        /// Intended for use by the Visual Flash sequence and The Reveal — not for
        /// in-play validation (use <see cref="IsValidPlacement"/> for that).
        /// </summary>
        /// <param name="row">Zero-based row index (0–8).</param>
        /// <param name="col">Zero-based column index (0–8).</param>
        /// <returns>The solution <see cref="ShapeType"/> and <see cref="ColourType"/> for this cell.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="row"/> or <paramref name="col"/> is outside 0–8.
        /// </exception>
        public (ShapeType Shape, ColourType Colour) GetSolutionCell(int row, int col)
        {
            EnsurePuzzleReady();
            if (!IsInBounds(row, col))
                throw new ArgumentOutOfRangeException($"({row},{col}) is outside the 9×9 grid.");

            return (_solutionShapes[row, col], _solutionColours[row, col]);
        }

        /// <summary>
        /// Selects one unfilled cell at random and returns the correct values from the
        /// solution grid. This is the data source for The Insight mechanic.
        /// Returns <c>null</c> when every cell is already filled (puzzle complete).
        /// </summary>
        /// <returns>
        /// A <see cref="HintCell"/> describing a randomly chosen empty cell and its
        /// correct shape and colour, or <c>null</c> if the Vessel is fully filled.
        /// </returns>
        public HintCell? GetHint()
        {
            EnsurePuzzleReady();

            var candidates = new List<(int r, int c)>(GridSize * GridSize);
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    if (!_currentShapes[r, c].HasValue)
                        candidates.Add((r, c));

            if (candidates.Count == 0) return null;

            (int row, int col) = candidates[_rng.Next(candidates.Count)];
            return new HintCell(row, col, _solutionShapes[row, col], _solutionColours[row, col]);
        }

        /// <summary>
        /// Returns <c>true</c> when all 81 cells are filled and every cell matches
        /// the solution on both axes. Used to trigger the Visual Flash and Reveal sequence.
        /// </summary>
        public bool IsSolved()
        {
            EnsurePuzzleReady();
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                {
                    if (!_currentShapes[r, c].HasValue)                    return false;
                    if (_currentShapes[r, c]  != _solutionShapes[r, c])   return false;
                    if (_currentColours[r, c] != _solutionColours[r, c])  return false;
                }
            return true;
        }

        // ── Grid generation ───────────────────────────────────────────────

        // Fills a 9×9 integer grid (values 0–8) using randomised backtracking.
        // Each call produces a different valid Sudoku layout due to candidate shuffling.
        private int[,] BuildFilledGrid()
        {
            int[,] grid = new int[GridSize, GridSize];
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

                    foreach (int val in ShuffledIndices())
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

        // Removes cells from copies of the two filled grids to produce puzzle clues,
        // verifies both axis sub-puzzles are uniquely solvable, then commits the
        // resulting clue state to the working arrays.
        private bool TryApplyPuzzleClues(int[,] shapeGrid, int[,] colourGrid, int prefillCount)
        {
            int cellsToRemove = (GridSize * GridSize) - prefillCount;

            var positions = new List<(int r, int c)>(GridSize * GridSize);
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    positions.Add((r, c));
            Shuffle(positions);

            // Work on clones so the original full grids are preserved for StoreSolution.
            int[,] shapeClues  = (int[,])shapeGrid.Clone();
            int[,] colourClues = (int[,])colourGrid.Clone();

            for (int i = 0; i < cellsToRemove; i++)
            {
                (int r, int c) = positions[i];
                shapeClues[r, c]  = -1;
                colourClues[r, c] = -1;
            }

            if (CountSolutions(shapeClues)  != 1) return false;
            if (CountSolutions(colourClues) != 1) return false;

            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    bool filled = shapeClues[r, c] != -1;
                    _isPreFilled[r, c]   = filled;
                    _currentShapes[r, c]  = filled ? (ShapeType?)ShapeValues[shapeClues[r, c]]    : null;
                    _currentColours[r, c] = filled ? (ColourType?)ColourValues[colourClues[r, c]] : null;
                }
            }
            return true;
        }

        private void StoreSolution(int[,] shapeGrid, int[,] colourGrid)
        {
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                {
                    _solutionShapes[r, c]  = ShapeValues[shapeGrid[r, c]];
                    _solutionColours[r, c] = ColourValues[colourGrid[r, c]];
                }
        }

        // ── Uniqueness check ──────────────────────────────────────────────

        // Counts solutions in an integer grid, stopping at maxCount to stay fast.
        // Returns 1 for a uniquely-solvable puzzle, 0 for unsolvable, 2+ for ambiguous.
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
                    return; // First empty cell explored — ascend to caller
                }
            }
            count++; // No empty cells remain — valid complete solution
        }

        // ── Constraint checking ───────────────────────────────────────────

        private static bool IsIndexValidInGrid(int[,] grid, int row, int col, int val)
        {
            for (int c = 0; c < GridSize; c++)
                if (grid[row, c] == val) return false;

            for (int r = 0; r < GridSize; r++)
                if (grid[r, col] == val) return false;

            int br = (row / BoxSize) * BoxSize;
            int bc = (col / BoxSize) * BoxSize;
            for (int r = br; r < br + BoxSize; r++)
                for (int c = bc; c < bc + BoxSize; c++)
                    if (grid[r, c] == val) return false;

            return true;
        }

        // Checks the shape axis of the current working state.
        // The caller guarantees (row, col) is currently empty (null), so the cell
        // will never falsely conflict with itself during iteration.
        private bool IsShapeValidInState(int row, int col, ShapeType shape)
        {
            for (int c = 0; c < GridSize; c++)
                if (_currentShapes[row, c] == shape) return false;

            for (int r = 0; r < GridSize; r++)
                if (_currentShapes[r, col] == shape) return false;

            int br = (row / BoxSize) * BoxSize;
            int bc = (col / BoxSize) * BoxSize;
            for (int r = br; r < br + BoxSize; r++)
                for (int c = bc; c < bc + BoxSize; c++)
                    if (_currentShapes[r, c] == shape) return false;

            return true;
        }

        private bool IsColourValidInState(int row, int col, ColourType colour)
        {
            for (int c = 0; c < GridSize; c++)
                if (_currentColours[row, c] == colour) return false;

            for (int r = 0; r < GridSize; r++)
                if (_currentColours[r, col] == colour) return false;

            int br = (row / BoxSize) * BoxSize;
            int bc = (col / BoxSize) * BoxSize;
            for (int r = br; r < br + BoxSize; r++)
                for (int c = bc; c < bc + BoxSize; c++)
                    if (_currentColours[r, c] == colour) return false;

            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private int PrefillCountFor(ClarityLevel clarity) => clarity switch
        {
            ClarityLevel.Sunlit   => _rng.Next(SunlitMin,   SunlitMax   + 1),
            ClarityLevel.Overcast => _rng.Next(OvercastMin, OvercastMax + 1),
            ClarityLevel.Moonlit  => _rng.Next(MoonlitMin,  MoonlitMax  + 1),
            ClarityLevel.Void     => _rng.Next(VoidMin,     VoidMax     + 1),
            _ => throw new ArgumentOutOfRangeException(nameof(clarity), clarity, null)
        };

        // Returns a Fisher-Yates shuffled array of indices [0, GridSize).
        private int[] ShuffledIndices()
        {
            int[] idx = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
            for (int i = GridSize - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }
            return idx;
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
                    "GeneratePuzzle must be called before using the solver.");
        }
    }
}

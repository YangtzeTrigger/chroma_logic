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
}

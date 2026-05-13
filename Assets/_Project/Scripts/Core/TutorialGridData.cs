namespace ChromaLogic.Core
{
    /// <summary>
    /// Hard-coded data for the Phase 3 onboarding tutorial Vessel.
    /// A 6×6 dual-axis Sudoku using a subset of six shapes and six colours.
    /// Solution and pre-fill mask are fixed so the tutorial is always consistent.
    /// Pure static data — no MonoBehaviour dependency.
    /// </summary>
    public static class TutorialGridData
    {
        // ── Grid constants ────────────────────────────────────────────────

        public const int Size    = 6;
        public const int BoxRows = 2;   // 2-row × 3-col boxes
        public const int BoxCols = 3;

        // ── Tutorial palette (6 of 9 shapes, 6 of 9 colours) ─────────────

        public static readonly ShapeType[] TutorialShapes =
        {
            ShapeType.Star, ShapeType.Hexagon, ShapeType.Diamond,
            ShapeType.Heart, ShapeType.Ring, ShapeType.Triangle
        };

        public static readonly ColourType[] TutorialColours =
        {
            ColourType.Amber, ColourType.Jade, ColourType.Cobalt,
            ColourType.Violet, ColourType.Rose, ColourType.Gold
        };

        // Unicode placeholder symbols, one per shape (replaced by art in Phase 4+).
        public static readonly string[] ShapeSymbols =
            { "★", "⬡", "◆", "♥", "○", "▲" };

        // USS colour-suffix class name, one per colour (index must match TutorialColours).
        public static readonly string[] ColourSuffix =
            { "amber", "jade", "cobalt", "violet", "rose", "gold" };

        // ── Full-game palettes (all 9 values, index == enum int value) ────

        // Index matches ShapeType enum: Infinity=0, Star=1 … Triangle=8
        public static readonly string[] AllShapeSymbols =
            { "∞", "★", "⬡", "☾", "◆", "✚", "♥", "○", "▲" };

        // Index matches ColourType enum: Crimson=0, Amber=1 … Gold=8
        public static readonly string[] AllColourSuffix =
            { "crimson", "amber", "jade", "cobalt", "violet", "teal", "rose", "slate", "gold" };

        // ── Solution grids (indices into TutorialShapes / TutorialColours) ─

        // Each row and column contains 0–5 exactly once.
        // Boxes (2 rows × 3 cols) also each contain 0–5 exactly once.

        private static readonly int[,] ShapeSolution =
        {
            { 0, 1, 2, 3, 4, 5 },
            { 3, 4, 5, 0, 1, 2 },
            { 1, 0, 3, 2, 5, 4 },
            { 2, 5, 4, 1, 0, 3 },
            { 4, 2, 0, 5, 3, 1 },
            { 5, 3, 1, 4, 2, 0 },
        };

        private static readonly int[,] ColourSolution =
        {
            { 2, 3, 4, 5, 0, 1 },
            { 5, 0, 1, 2, 3, 4 },
            { 3, 4, 5, 0, 1, 2 },
            { 0, 1, 2, 3, 4, 5 },
            { 4, 5, 0, 1, 2, 3 },
            { 1, 2, 3, 4, 5, 0 },
        };

        // ── Pre-fill mask ─────────────────────────────────────────────────

        // true  = pre-filled (locked, shown at start)
        // false = empty (interactive — Curator must place these 6 cells)
        //
        // Empty cells and their correct values:
        //   (0,5) → Triangle,  Jade
        //   (1,1) → Ring,      Amber
        //   (2,3) → Diamond,   Amber
        //   (3,4) → Star,      Rose
        //   (4,0) → Ring,      Rose
        //   (5,2) → Hexagon,   Violet

        private static readonly bool[,] PreFilledMask =
        {
            { true,  true,  true,  true,  true,  false },
            { true,  false, true,  true,  true,  true  },
            { true,  true,  true,  false, true,  true  },
            { true,  true,  true,  true,  false, true  },
            { false, true,  true,  true,  true,  true  },
            { true,  true,  false, true,  true,  true  },
        };

        // ── Public accessors ──────────────────────────────────────────────

        public static int       GetShapeIndex (int row, int col) => ShapeSolution[row, col];
        public static int       GetColourIndex(int row, int col) => ColourSolution[row, col];
        public static ShapeType GetShape      (int row, int col) => TutorialShapes[ShapeSolution[row, col]];
        public static ColourType GetColour    (int row, int col) => TutorialColours[ColourSolution[row, col]];
        public static bool      IsPreFilled   (int row, int col) => PreFilledMask[row, col];

        // Total number of interactive cells the Curator must fill.
        public static int EmptyCellCount { get; } = CountEmpty();

        private static int CountEmpty()
        {
            int n = 0;
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (!PreFilledMask[r, c]) n++;
            return n;
        }
    }
}

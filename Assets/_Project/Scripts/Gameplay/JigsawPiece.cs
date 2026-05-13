using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.Gameplay
{
    /// <summary>
    /// Represents a single Jigsaw puzzle piece — its VisualElement, current transform,
    /// solved-position target, and snap state.
    /// <para>
    /// Create via the constructor; the resulting <see cref="Element"/> must be added to
    /// the board VisualElement by the caller. Call <see cref="ApplyTransform"/> after
    /// changing <see cref="Left"/>, <see cref="Top"/>, or <see cref="Angle"/> to push
    /// values to the UI. Call <see cref="TrySnap"/> on pointer-up to check placement.
    /// </para>
    /// </summary>
    public sealed class JigsawPiece
    {
        // ── Identity ──────────────────────────────────────────────────────

        /// <summary>Row-major zero-based index within the full piece grid.</summary>
        public int Index { get; }

        /// <summary>Row within the grid (0 = top).</summary>
        public int Row { get; }

        /// <summary>Column within the grid (0 = left).</summary>
        public int Col { get; }

        // ── Visual element ────────────────────────────────────────────────

        /// <summary>The VisualElement representing this piece. Add to the board container.</summary>
        public VisualElement Element { get; }

        // ── Current transform (board-space) ───────────────────────────────

        /// <summary>Horizontal position in board-space pixels (left edge).</summary>
        public float Left  { get; set; }

        /// <summary>Vertical position in board-space pixels (top edge).</summary>
        public float Top   { get; set; }

        /// <summary>Rotation in degrees (0 = solved orientation).</summary>
        public float Angle { get; set; }

        // ── Solved transform ──────────────────────────────────────────────

        /// <summary>Horizontal position when the piece is correctly placed.</summary>
        public float SolvedLeft  { get; }

        /// <summary>Vertical position when the piece is correctly placed.</summary>
        public float SolvedTop   { get; }

        // ── Dimensions ────────────────────────────────────────────────────

        /// <summary>Width of this piece in pixels.</summary>
        public float PieceWidth  { get; }

        /// <summary>Height of this piece in pixels.</summary>
        public float PieceHeight { get; }

        // ── State ─────────────────────────────────────────────────────────

        /// <summary><c>true</c> once the piece has been snapped to its correct position.</summary>
        public bool IsSnapped { get; private set; }

        // ── USS class ─────────────────────────────────────────────────────

        private const string ClassPiece        = "jigsaw-piece";
        private const string ClassPieceSnapped = "jigsaw-piece--snapped";

        // ── Constructor ───────────────────────────────────────────────────

        /// <summary>
        /// Creates the piece VisualElement and configures its background slice from
        /// <paramref name="sprite"/> using the CSS sprite-sheet technique:
        /// <c>background-size</c> is set to the full board dimensions;
        /// <c>background-position</c> offsets to this piece's row/column.
        /// </summary>
        /// <param name="index">Row-major piece index.</param>
        /// <param name="row">Row in the grid.</param>
        /// <param name="col">Column in the grid.</param>
        /// <param name="sprite">The full Vessel reveal image.</param>
        /// <param name="cols">Total grid columns.</param>
        /// <param name="rows">Total grid rows.</param>
        /// <param name="pieceW">Width of each piece in pixels.</param>
        /// <param name="pieceH">Height of each piece in pixels.</param>
        public JigsawPiece(int index, int row, int col,
                           Sprite sprite, int cols, int rows,
                           float pieceW, float pieceH)
        {
            Index        = index;
            Row          = row;
            Col          = col;
            PieceWidth   = pieceW;
            PieceHeight  = pieceH;
            SolvedLeft   = col * pieceW;
            SolvedTop    = row * pieceH;

            Element = new VisualElement();
            Element.AddToClassList(ClassPiece);
            Element.style.width    = pieceW;
            Element.style.height   = pieceH;
            Element.style.position = Position.Absolute;

            if (sprite != null)
            {
                float totalW = cols * pieceW;
                float totalH = rows * pieceH;

                Element.style.backgroundImage = new StyleBackground(sprite);
                Element.style.backgroundSize  = new StyleBackgroundSize(
                    new BackgroundSize(
                        new Length(totalW, LengthUnit.Pixel),
                        new Length(totalH, LengthUnit.Pixel)));
                Element.style.backgroundPositionX = new StyleBackgroundPosition(
                    new BackgroundPosition(
                        BackgroundPositionKeyword.Left,
                        new Length(-(col * pieceW), LengthUnit.Pixel)));
                Element.style.backgroundPositionY = new StyleBackgroundPosition(
                    new BackgroundPosition(
                        BackgroundPositionKeyword.Top,
                        new Length(-(row * pieceH), LengthUnit.Pixel)));
            }

            Left  = SolvedLeft;
            Top   = SolvedTop;
            Angle = 0f;
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Writes <see cref="Left"/>, <see cref="Top"/>, and <see cref="Angle"/>
        /// to <see cref="Element"/>'s inline style.
        /// </summary>
        public void ApplyTransform()
        {
            Element.style.left   = Left;
            Element.style.top    = Top;
            Element.style.rotate = new StyleRotate(
                new Rotate(new Angle(Angle, AngleUnit.Degree)));
        }

        /// <summary>
        /// Snaps this piece to its solved position if within
        /// <paramref name="snapRadius"/> pixels. Returns <c>true</c> on success.
        /// Once snapped the piece cannot be moved or snapped again.
        /// </summary>
        public bool TrySnap(float snapRadius)
        {
            if (IsSnapped) return false;

            float dx   = Left  - SolvedLeft;
            float dy   = Top   - SolvedTop;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            if (dist > snapRadius) return false;

            Left  = SolvedLeft;
            Top   = SolvedTop;
            Angle = 0f;
            IsSnapped = true;
            ApplyTransform();
            Element.AddToClassList(ClassPieceSnapped);
            return true;
        }
    }
}

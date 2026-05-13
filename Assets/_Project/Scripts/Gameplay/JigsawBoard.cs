#nullable enable
using System;
using System.Collections.Generic;
using ChromaLogic.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.Gameplay
{
    /// <summary>
    /// Manages the full collection of <see cref="JigsawPiece"/> objects within a board
    /// VisualElement, including zoom/pan transforms, scatter animation, hit-testing,
    /// and snap-to-solve detection.
    /// <para>
    /// Construct once after the board VisualElement's geometry is resolved (non-zero size).
    /// Call <see cref="ScatterPieces"/> immediately after construction to animate pieces
    /// into play positions. Subscribe to <see cref="OnBoardComplete"/> to receive the
    /// completion signal.
    /// </para>
    /// </summary>
    public sealed class JigsawBoard
    {
        // ── Events ────────────────────────────────────────────────────────

        /// <summary>Fires when all pieces have been snapped to their correct positions.</summary>
        public event Action OnBoardComplete;

        // ── Public state ──────────────────────────────────────────────────

        /// <summary>All pieces in row-major order.</summary>
        public List<JigsawPiece> Pieces      { get; }

        /// <summary>Number of pieces currently snapped.</summary>
        public int               SnappedCount { get; private set; }

        /// <summary>Total piece count for this difficulty.</summary>
        public int               TotalPieces  { get; }

        // ── Private state ─────────────────────────────────────────────────

        private readonly VisualElement    _boardEl;
        private readonly JigsawDifficulty _difficulty;
        private readonly float            _boardW;
        private readonly float            _boardH;
        private readonly float            _snapRadius;

        private float   _zoom      = 1f;
        private Vector2 _panOffset = Vector2.zero;

        // Zoom bounds
        private const float ZoomMin = 0.5f;
        private const float ZoomMax = 4.0f;

        // ── Constructor ───────────────────────────────────────────────────

        /// <summary>
        /// Creates all pieces and adds them to <paramref name="boardElement"/>.
        /// Pieces start at their solved positions; call <see cref="ScatterPieces"/>
        /// to animate them into play positions.
        /// </summary>
        /// <param name="boardElement">The absolute-positioned board VisualElement.</param>
        /// <param name="sprite">Full Vessel reveal image used for all piece backgrounds.</param>
        /// <param name="difficulty">Determines grid dimensions and snap radius.</param>
        /// <param name="boardW">Resolved width of the board container in pixels.</param>
        /// <param name="boardH">Resolved height of the board container in pixels.</param>
        public JigsawBoard(VisualElement boardElement, Sprite sprite,
                           JigsawDifficulty difficulty, float boardW, float boardH)
        {
            _boardEl    = boardElement;
            _difficulty = difficulty;
            _boardW     = boardW;
            _boardH     = boardH;
            _snapRadius = GetSnapRadius(difficulty);

            var (cols, rows) = GetGridDims(difficulty);
            float pieceW     = boardW  / cols;
            float pieceH     = boardH  / rows;
            TotalPieces      = cols * rows;
            Pieces           = new List<JigsawPiece>(TotalPieces);

            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                int index = r * cols + c;
                var piece = new JigsawPiece(index, r, c, sprite, cols, rows, pieceW, pieceH);
                Pieces.Add(piece);
                _boardEl.Add(piece.Element);
                piece.ApplyTransform();
            }
        }

        // ── Scatter ───────────────────────────────────────────────────────

        /// <summary>
        /// Animates pieces away from their solved positions into randomised play positions.
        /// <list type="bullet">
        ///   <item>Meditative/Focused: instant random scatter (no animation).</item>
        ///   <item>Challenging: staggered cascade, pieces fly in from random edges over 0–1.2s.</item>
        ///   <item>GrandMaster: slow drift fade-in, pieces materialise from near-solved positions over 0–2.0s.</item>
        /// </list>
        /// </summary>
        public void ScatterPieces()
        {
            switch (_difficulty)
            {
                case JigsawDifficulty.Challenging:
                    ScatterCascade();
                    break;
                case JigsawDifficulty.GrandMaster:
                    ScatterDrift();
                    break;
                default:
                    ScatterInstant();
                    break;
            }
        }

        // ── Zoom / pan ────────────────────────────────────────────────────

        /// <summary>
        /// Applies a multiplicative zoom delta centred on <paramref name="pivotScreen"/>,
        /// clamped between <see cref="ZoomMin"/> and <see cref="ZoomMax"/>.
        /// </summary>
        public void ApplyZoomDelta(float delta, Vector2 pivotScreen)
        {
            float newZoom = Mathf.Clamp(_zoom * delta, ZoomMin, ZoomMax);
            float ratio   = newZoom / _zoom;

            // Adjust pan so the pivot point stays fixed on screen.
            _panOffset = pivotScreen + ((_panOffset - pivotScreen) * ratio);
            _zoom      = newZoom;

            ApplyBoardTransform();
        }

        /// <summary>Pans the board by <paramref name="delta"/> pixels in screen space.</summary>
        public void ApplyPanDelta(Vector2 delta)
        {
            _panOffset += delta;
            ApplyBoardTransform();
        }

        // ── Hit testing ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the topmost un-snapped piece that contains
        /// <paramref name="boardPos"/> (in board-element-local space), or <c>null</c>.
        /// Iterates in reverse order so pieces rendered last (on top) win.
        /// </summary>
        public JigsawPiece? HitTest(Vector2 boardPos)
        {
            for (int i = Pieces.Count - 1; i >= 0; i--)
            {
                var p = Pieces[i];
                if (p.IsSnapped) continue;
                if (boardPos.x >= p.Left && boardPos.x <= p.Left + p.PieceWidth &&
                    boardPos.y >= p.Top  && boardPos.y <= p.Top  + p.PieceHeight)
                    return p;
            }
            return null;
        }

        /// <summary>
        /// Attempts to snap <paramref name="piece"/> to its solved position.
        /// Increments <see cref="SnappedCount"/> and fires <see cref="OnBoardComplete"/>
        /// when all pieces are snapped.
        /// </summary>
        /// <returns><c>true</c> if the piece snapped.</returns>
        public bool TrySnapPiece(JigsawPiece piece)
        {
            if (!piece.TrySnap(_snapRadius)) return false;

            SnappedCount++;
            if (SnappedCount >= TotalPieces)
                OnBoardComplete?.Invoke();

            return true;
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static (int cols, int rows) GetGridDims(JigsawDifficulty d) => d switch
        {
            JigsawDifficulty.Meditative  => (4, 3),
            JigsawDifficulty.Focused     => (6, 4),
            JigsawDifficulty.Challenging => (8, 6),
            _                            => (12, 8),
        };

        private static float GetSnapRadius(JigsawDifficulty d) => d switch
        {
            JigsawDifficulty.Meditative  => 40f,
            JigsawDifficulty.Focused     => 35f,
            JigsawDifficulty.Challenging => 20f,
            _                            => 12f,
        };

        private void ApplyBoardTransform()
        {
            _boardEl.style.scale = new StyleScale(
                new Scale(new Vector3(_zoom, _zoom, 1f)));
            _boardEl.style.translate = new StyleTranslate(
                new Translate(
                    new Length(_panOffset.x, LengthUnit.Pixel),
                    new Length(_panOffset.y, LengthUnit.Pixel)));
        }

        private void ScatterInstant()
        {
            float spreadX = _boardW * 0.3f;
            float spreadY = _boardH * 0.3f;

            foreach (var p in Pieces)
            {
                p.Left  = Mathf.Clamp(p.SolvedLeft  + UnityEngine.Random.Range(-spreadX, spreadX),
                                      0f, _boardW - p.PieceWidth);
                p.Top   = Mathf.Clamp(p.SolvedTop   + UnityEngine.Random.Range(-spreadY, spreadY),
                                      0f, _boardH - p.PieceHeight);
                p.Angle = UnityEngine.Random.Range(-15f, 15f);
                p.ApplyTransform();
            }
        }

        private void ScatterCascade()
        {
            int   count    = Pieces.Count;
            float maxDelay = 1.2f;

            foreach (var p in Pieces)
            {
                // Start piece at a random edge of the board.
                float startLeft = UnityEngine.Random.value < 0.5f
                    ? UnityEngine.Random.Range(-p.PieceWidth, 0f)
                    : UnityEngine.Random.Range(_boardW, _boardW + p.PieceWidth);
                float startTop = UnityEngine.Random.Range(0f, _boardH - p.PieceHeight);

                float targetLeft = Mathf.Clamp(
                    p.SolvedLeft + UnityEngine.Random.Range(-_boardW * 0.25f, _boardW * 0.25f),
                    0f, _boardW  - p.PieceWidth);
                float targetTop = Mathf.Clamp(
                    p.SolvedTop  + UnityEngine.Random.Range(-_boardH * 0.25f, _boardH * 0.25f),
                    0f, _boardH  - p.PieceHeight);
                float targetAngle = UnityEngine.Random.Range(-20f, 20f);

                float delay = (float)p.Index / (count - 1) * maxDelay;

                p.Left  = startLeft;
                p.Top   = startTop;
                p.Angle = 0f;
                p.ApplyTransform();

                var captured = p;
                DOVirtual.Float(0f, 1f, 0.25f, t =>
                {
                    captured.Left  = Mathf.Lerp(startLeft, targetLeft,  t);
                    captured.Top   = Mathf.Lerp(startTop,  targetTop,   t);
                    captured.Angle = Mathf.Lerp(0f,        targetAngle, t);
                    captured.ApplyTransform();
                }).SetDelay(delay);
            }
        }

        private void ScatterDrift()
        {
            int   count    = Pieces.Count;
            float maxDelay = 2.0f;

            foreach (var p in Pieces)
            {
                float startLeft  = p.SolvedLeft  + UnityEngine.Random.Range(-50f, 50f);
                float startTop   = p.SolvedTop   + UnityEngine.Random.Range(-50f, 50f);
                float targetLeft = Mathf.Clamp(
                    p.SolvedLeft + UnityEngine.Random.Range(-_boardW * 0.2f, _boardW * 0.2f),
                    0f, _boardW  - p.PieceWidth);
                float targetTop = Mathf.Clamp(
                    p.SolvedTop  + UnityEngine.Random.Range(-_boardH * 0.2f, _boardH * 0.2f),
                    0f, _boardH  - p.PieceHeight);
                float targetAngle = UnityEngine.Random.Range(-10f, 10f);

                float delay = (float)p.Index / (count - 1) * maxDelay;

                p.Left  = startLeft;
                p.Top   = startTop;
                p.Angle = targetAngle;
                p.Element.style.opacity = 0f;
                p.ApplyTransform();

                var captured = p;
                DOVirtual.Float(0f, 1f, 0.3f, t =>
                {
                    captured.Element.style.opacity = t;
                    captured.Left  = Mathf.Lerp(startLeft, targetLeft,  t);
                    captured.Top   = Mathf.Lerp(startTop,  targetTop,   t);
                    captured.ApplyTransform();
                }).SetDelay(delay);
            }
        }
    }
}

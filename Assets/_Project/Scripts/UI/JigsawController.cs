using System;
using ChromaLogic.Core;
using ChromaLogic.Gameplay;
using ChromaLogic.Managers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Phase 7 Jigsaw Controller — orchestrates the full jigsaw experience.
    /// <para>
    /// Scene flow: difficulty selection → 2-second image flash → scatter animation
    /// → interactive puzzle → completion (Living Image stub, XP award, result panel).
    /// </para>
    /// <para>
    /// Input: two-finger pinch-zoom and pan via <see cref="EnhancedTouchSupport"/>;
    /// single-finger drag to move pieces; long-press (0.5s hold) to enter rotation
    /// mode (horizontal drag rotates the selected piece). Mouse left-drag and
    /// right-drag are supported in the Editor.
    /// </para>
    /// <para>
    /// Assign <see cref="_testSprite"/> in the Inspector for development. In production
    /// the sprite will be loaded via Addressables using the vessel ID stored in
    /// <c>PlayerPrefs "CL_PendingJigsawVesselId"</c>.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class JigsawController : MonoBehaviour
    {
        // ── PlayerPrefs keys ──────────────────────────────────────────────

        private const string KeyPendingVesselId  = "CL_PendingJigsawVesselId";

        // ── Grand Master XP gate ──────────────────────────────────────────

        /// <summary>Cumulative Gallery XP required to unlock Grand Master difficulty (Level 10).</summary>
        private const int GrandMasterUnlockXP = 1000;

        // ── UXML element name constants ───────────────────────────────────

        // Difficulty screen
        private const string NameDifficultyScreen  = "difficulty-screen";
        private const string NameCardGrandmaster   = "card-grandmaster";
        private const string NameLockLabel         = "lock-label";
        private const string NameBtnMeditative     = "btn-meditative";
        private const string NameBtnFocused        = "btn-focused";
        private const string NameBtnChallenging    = "btn-challenging";
        private const string NameBtnGrandmaster    = "btn-grandmaster";
        // Game screen
        private const string NameGameScreen        = "game-screen";
        private const string NameBtnBack           = "btn-back";
        private const string NamePieceCounter      = "piece-counter";
        private const string NameFlashOverlay      = "flash-overlay";
        private const string NameFlashImage        = "flash-image";
        private const string NameBoardContainer    = "board-container";
        private const string NameJigsawBoard       = "jigsaw-board";
        private const string NameResultPanel       = "result-panel";
        private const string NameBtnShare          = "btn-share";
        private const string NameBtnExit           = "btn-exit";

        // ── USS class constants ───────────────────────────────────────────

        private const string ClassHidden       = "hidden";
        private const string ClassCardLocked   = "difficulty-card--locked";

        // ── Inspector ─────────────────────────────────────────────────────

        [SerializeField]
        [Tooltip("Development placeholder. Replace with Addressables load via vessel ID in production.")]
        private Sprite _testSprite;

        // ── Events ────────────────────────────────────────────────────────

        /// <summary>Fires when the Curator has assembled the complete Vessel.</summary>
        public event Action OnJigsawComplete;

        // ── Private state — scene ─────────────────────────────────────────

        private UIDocument    _document;
        private JigsawBoard   _board;
        private string        _vesselId;
        private JigsawDifficulty _selectedDifficulty;

        // ── Private state — elements ──────────────────────────────────────

        private VisualElement _difficultyScreen;
        private VisualElement _gameScreen;
        private VisualElement _flashOverlay;
        private VisualElement _flashImage;
        private VisualElement _boardContainer;
        private VisualElement _boardEl;
        private VisualElement _resultPanel;
        private Label         _pieceCounter;
        private Button        _btnGrandmaster;
        private VisualElement _cardGrandmaster;

        // ── Private state — input ─────────────────────────────────────────

        private JigsawPiece _dragPiece;
        private bool        _isRotating;
        private float       _touchHoldTime;
        private float       _rotationStartX;
        private float       _rotationStartAngle;
        private float       _prevPinchDist;
        private Vector2     _prevSinglePos;
        private bool        _wasTwoFinger;

        // ── Private state — flash ─────────────────────────────────────────

        private bool  _flashComplete;
        private float _flashElapsed;
        private bool  _boardPending;  // board init deferred until geometry resolves

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                Debug.LogError("[JigsawController] UIDocument component missing.");
                return;
            }
            BindElements();
            WireDifficultyButtons();
        }

        private void Start()
        {
            _vesselId = PlayerPrefs.GetString(KeyPendingVesselId, string.Empty);

            var pdm         = PlayerDataManager.Instance;
            bool gmLocked   = pdm == null || pdm.GalleryXP < GrandMasterUnlockXP;

            if (gmLocked)
            {
                _cardGrandmaster?.AddToClassList(ClassCardLocked);
                if (_btnGrandmaster != null) _btnGrandmaster.SetEnabled(false);
            }
            else
            {
                _cardGrandmaster?.RemoveFromClassList(ClassCardLocked);
                _document.rootVisualElement.Q<Label>(NameLockLabel)?.AddToClassList(ClassHidden);
            }

            ShowDifficultyScreen();
        }

        private void OnEnable()  => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();

        private void Update()
        {
            if (_board == null) return;

            var touches = Touch.activeTouches;

            if (!_flashComplete)
            {
                HandleFlashSkip(touches.Count);
                return;
            }

            if (touches.Count == 2)
            {
                HandlePinch(touches[0], touches[1]);
                _wasTwoFinger = true;
            }
            else if (touches.Count == 1 && !_wasTwoFinger)
            {
                HandleSingleTouch(touches[0]);
            }
            else if (touches.Count == 0)
            {
                _wasTwoFinger  = false;
                _prevPinchDist = 0f;
                _touchHoldTime = 0f;
            }

#if UNITY_EDITOR
            HandleMouseInput();
#endif
        }

        // ── Difficulty selection ──────────────────────────────────────────

        private void ShowDifficultyScreen()
        {
            _difficultyScreen?.RemoveFromClassList(ClassHidden);
            _gameScreen?.AddToClassList(ClassHidden);
        }

        private void SelectDifficulty(JigsawDifficulty d)
        {
            var pdm = PlayerDataManager.Instance;
            if (d == JigsawDifficulty.GrandMaster &&
                (pdm == null || pdm.GalleryXP < GrandMasterUnlockXP))
            {
                PersistentCanvasController.Instance?.ShowToast(
                    "Reach Gallery Level 10 to unlock Grand Master.", 3f);
                return;
            }

            _selectedDifficulty = d;
            _difficultyScreen?.AddToClassList(ClassHidden);
            _gameScreen?.RemoveFromClassList(ClassHidden);

            StartFlash();
        }

        private void WireDifficultyButtons()
        {
            var root = _document.rootVisualElement;
            root.Q<Button>(NameBtnMeditative)  ?.RegisterCallback<ClickEvent>(_ => SelectDifficulty(JigsawDifficulty.Meditative));
            root.Q<Button>(NameBtnFocused)     ?.RegisterCallback<ClickEvent>(_ => SelectDifficulty(JigsawDifficulty.Focused));
            root.Q<Button>(NameBtnChallenging) ?.RegisterCallback<ClickEvent>(_ => SelectDifficulty(JigsawDifficulty.Challenging));
            root.Q<Button>(NameBtnGrandmaster) ?.RegisterCallback<ClickEvent>(_ => SelectDifficulty(JigsawDifficulty.GrandMaster));
        }

        // ── Flash sequence ────────────────────────────────────────────────

        private void StartFlash()
        {
            _flashComplete = false;
            _flashElapsed  = 0f;

            if (_flashImage != null && _testSprite != null)
                _flashImage.style.backgroundImage = new StyleBackground(_testSprite);

            _flashOverlay?.RemoveFromClassList(ClassHidden);
            DOVirtual.DelayedCall(2f, () => { _flashComplete = true; EndFlash(); });
        }

        private void HandleFlashSkip(int touchCount)
        {
            if (touchCount < 1) { _flashElapsed = 0f; return; }
            _flashElapsed += Time.deltaTime;
            // Flash is mandatory for 2s; after that a sustained hold skips it.
            if (_flashElapsed >= 2f) { _flashComplete = true; EndFlash(); }
        }

        private void EndFlash()
        {
            _flashOverlay?.AddToClassList(ClassHidden);

            float w = _boardContainer?.resolvedStyle.width  ?? 0f;
            float h = _boardContainer?.resolvedStyle.height ?? 0f;

            if (w > 0f && h > 0f)
            {
                CreateBoard(w, h);
            }
            else
            {
                // Geometry not yet resolved — defer until layout pass completes.
                _boardPending = true;
                _boardContainer?.RegisterCallback<GeometryChangedEvent>(OnBoardGeometryReady);
            }
        }

        private void OnBoardGeometryReady(GeometryChangedEvent evt)
        {
            if (!_boardPending) return;
            _boardContainer?.UnregisterCallback<GeometryChangedEvent>(OnBoardGeometryReady);
            _boardPending = false;
            CreateBoard(evt.newRect.width, evt.newRect.height);
        }

        private void CreateBoard(float w, float h)
        {
            if (_boardEl == null) return;

            _board                   = new JigsawBoard(_boardEl, _testSprite, _selectedDifficulty, w, h);
            _board.OnBoardComplete   += HandleBoardComplete;

            UpdatePieceCounter();
            _board.ScatterPieces();
        }

        // ── Input — flash skip ────────────────────────────────────────────

        // (handled inside Update → HandleFlashSkip)

        // ── Input — pinch zoom ────────────────────────────────────────────

        private void HandlePinch(Touch a, Touch b)
        {
            Vector2 mid  = (a.screenPosition + b.screenPosition) * 0.5f;
            float   dist = Vector2.Distance(a.screenPosition, b.screenPosition);

            if (_prevPinchDist > 0f)
            {
                float delta = dist / _prevPinchDist;
                _board?.ApplyZoomDelta(delta, mid);
            }
            _prevPinchDist = dist;
            _dragPiece     = null;
        }

        // ── Input — single touch ──────────────────────────────────────────

        private void HandleSingleTouch(Touch t)
        {
            switch (t.phase)
            {
                case TouchPhase.Began:
                    _dragPiece      = _board?.HitTest(t.screenPosition);
                    _touchHoldTime  = 0f;
                    _isRotating     = false;
                    _prevSinglePos  = t.screenPosition;
                    if (_dragPiece != null)
                    {
                        _rotationStartX     = t.screenPosition.x;
                        _rotationStartAngle = _dragPiece.Angle;
                        BringToFront(_dragPiece);
                    }
                    break;

                case TouchPhase.Moved:
                    if (_dragPiece == null) break;

                    Vector2 delta = t.screenPosition - _prevSinglePos;
                    float   moved = delta.magnitude;

                    _touchHoldTime += Time.deltaTime;
                    if (!_isRotating && _touchHoldTime > 0.5f && moved < 8f)
                        _isRotating = true;

                    if (_isRotating)
                    {
                        float dx = t.screenPosition.x - _rotationStartX;
                        _dragPiece.Angle = _rotationStartAngle + dx * 0.5f;
                        _dragPiece.ApplyTransform();
                    }
                    else
                    {
                        _dragPiece.Left += delta.x;
                        _dragPiece.Top  -= delta.y;   // screen Y is inverted vs layout Y
                        _dragPiece.ApplyTransform();
                    }
                    _prevSinglePos = t.screenPosition;
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_dragPiece != null)
                    {
                        _board?.TrySnapPiece(_dragPiece);
                        UpdatePieceCounter();
                        _dragPiece = null;
                    }
                    _isRotating    = false;
                    _touchHoldTime = 0f;
                    _prevPinchDist = 0f;
                    break;
            }
            _prevSinglePos = t.screenPosition;
        }

        // ── Input — mouse (Editor) ────────────────────────────────────────

#if UNITY_EDITOR
        private bool    _mouseDragging;
        private bool    _mouseRotating;
        private Vector2 _mousePrev;
        private float   _mouseHoldTime;
        private float   _mouseRotStartX;
        private float   _mouseRotStartAngle;

        private void HandleMouseInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 pos = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                _dragPiece     = _board?.HitTest(pos);
                _mouseDragging = _dragPiece != null;
                _mouseRotating = false;
                _mouseHoldTime = 0f;
                _mousePrev     = pos;
                if (_dragPiece != null)
                {
                    _mouseRotStartX     = pos.x;
                    _mouseRotStartAngle = _dragPiece.Angle;
                    BringToFront(_dragPiece);
                }
            }
            else if (mouse.leftButton.isPressed && _dragPiece != null)
            {
                Vector2 d = pos - _mousePrev;
                _mouseHoldTime += Time.deltaTime;
                if (!_mouseRotating && _mouseHoldTime > 0.5f && d.magnitude < 8f)
                    _mouseRotating = true;

                if (_mouseRotating)
                {
                    _dragPiece.Angle = _mouseRotStartAngle + (pos.x - _mouseRotStartX) * 0.5f;
                    _dragPiece.ApplyTransform();
                }
                else
                {
                    _dragPiece.Left += d.x;
                    _dragPiece.Top  -= d.y;
                    _dragPiece.ApplyTransform();
                }
                _mousePrev = pos;
            }
            else if (mouse.leftButton.wasReleasedThisFrame && _dragPiece != null)
            {
                _board?.TrySnapPiece(_dragPiece);
                UpdatePieceCounter();
                _dragPiece     = null;
                _mouseDragging = false;
                _mouseRotating = false;
            }

            // Right-click drag: pan the board
            if (mouse.rightButton.wasPressedThisFrame)
                _mousePrev = pos;
            else if (mouse.rightButton.isPressed)
            {
                _board?.ApplyPanDelta(pos - _mousePrev);
                _mousePrev = pos;
            }

            // Scroll wheel: zoom
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                _board?.ApplyZoomDelta(1f + scroll * 0.1f, pos);
        }
#endif

        // ── Board completion ──────────────────────────────────────────────

        private void HandleBoardComplete()
        {
            Debug.Log("[JigsawController] Living Image would play here.");

            ProgressionManager.Instance?.AwardJigsawXP(_selectedDifficulty);
            PersistentCanvasController.Instance?.ShowToast("Vessel assembled.", 3f);
            OnJigsawComplete?.Invoke();

            DOVirtual.DelayedCall(3f, () => _resultPanel?.RemoveFromClassList(ClassHidden));
        }

        // ── UI helpers ────────────────────────────────────────────────────

        private void UpdatePieceCounter()
        {
            if (_board == null || _pieceCounter == null) return;
            _pieceCounter.text = $"{_board.SnappedCount} / {_board.TotalPieces}";
        }

        private static void BringToFront(JigsawPiece piece)
        {
            piece.Element.parent?.Add(piece.Element); // re-adding moves to last child (top)
        }

        // ── Element binding ───────────────────────────────────────────────

        private void BindElements()
        {
            var root = _document.rootVisualElement;

            _difficultyScreen = root.Q(NameDifficultyScreen);
            _cardGrandmaster  = root.Q(NameCardGrandmaster);
            _btnGrandmaster   = root.Q<Button>(NameBtnGrandmaster);
            _gameScreen       = root.Q(NameGameScreen);
            _flashOverlay     = root.Q(NameFlashOverlay);
            _flashImage       = root.Q(NameFlashImage);
            _boardContainer   = root.Q(NameBoardContainer);
            _boardEl          = root.Q(NameJigsawBoard);
            _resultPanel      = root.Q(NameResultPanel);
            _pieceCounter     = root.Q<Label>(NamePieceCounter);

            root.Q<Button>(NameBtnBack) ?.RegisterCallback<ClickEvent>(_ => GameManager.Instance?.LoadDashboard());
            root.Q<Button>(NameBtnExit) ?.RegisterCallback<ClickEvent>(_ => GameManager.Instance?.LoadDashboard());
            root.Q<Button>(NameBtnShare)?.RegisterCallback<ClickEvent>(_ =>
                Debug.Log("[JigsawController] Share — Phase 11."));
        }
    }
}

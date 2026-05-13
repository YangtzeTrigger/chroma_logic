using ChromaLogic.Core;
using ChromaLogic.Managers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Drives the four-screen Phase 3 onboarding flow.
    /// <para>
    /// Screens in order:
    /// <list type="number">
    ///   <item><term>The First Vessel</term><description>Welcome copy, no interaction.</description></item>
    ///   <item><term>Chromatic Harmony</term><description>Dual-axis concept illustration.</description></item>
    ///   <item><term>The Quiet Gallery</term><description>Interactive 6×6 tutorial Vessel.</description></item>
    ///   <item><term>Curator's Welcome</term><description>Rank reveal, Enter The Gallery CTA.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Requires a <see cref="UIDocument"/> component on the same GameObject pointing at
    /// <c>Onboarding.uxml</c>, and a sibling <see cref="TutorialGridController"/> component.
    /// Screen transitions are a cross-fade driven by DOTween.
    /// </para>
    /// <para>
    /// On <c>Start</c> the nav bar is hidden via <see cref="PersistentCanvasController"/>;
    /// it is restored when the Curator taps "Enter The Gallery" on screen 3.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(TutorialGridController))]
    public sealed class OnboardingController : MonoBehaviour
    {
        // ── UXML element name constants ───────────────────────────────────

        private const string NameScreen0    = "screen-0";
        private const string NameScreen1    = "screen-1";
        private const string NameScreen2    = "screen-2";
        private const string NameScreen3    = "screen-3";

        private const string NameBtn0       = "btn-0";
        private const string NameBtn1       = "btn-1";
        private const string NameBtn2       = "btn-2";
        private const string NameBtn3       = "btn-3";

        private const string NameRankLabel      = "rank-label";
        private const string NameGridRoot       = "tutorial-grid-root";
        private const string NamePaletteShapes  = "palette-shapes";
        private const string NamePaletteColours = "palette-colours";

        // ── USS class constants ───────────────────────────────────────────

        private const string ClassHidden = "hidden";

        // ── Transition timing ─────────────────────────────────────────────

        private const float FadeDuration = 0.25f;

        // ── Private state ─────────────────────────────────────────────────

        private UIDocument              _document;
        private TutorialGridController  _tutorialGrid;

        private VisualElement[] _screens;     // indices 0-3
        private Button[]        _continueBtns;

        private Label     _rankLabel;
        private int       _currentScreen;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            _document     = GetComponent<UIDocument>();
            _tutorialGrid = GetComponent<TutorialGridController>();

            if (_document == null)
            {
                Debug.LogError("[OnboardingController] UIDocument component missing.");
                return;
            }

            BindElements();
        }

        private void Start()
        {
            PersistentCanvasController.Instance?.HideNavBar();

            _tutorialGrid.Initialise(
                _document.rootVisualElement.Q(NameGridRoot),
                _document.rootVisualElement.Q(NamePaletteShapes),
                _document.rootVisualElement.Q(NamePaletteColours)
            );

            _tutorialGrid.OnTutorialComplete += OnTutorialSolved;

            PopulateRankLabel();
            ShowScreen(0);
        }

        private void OnDestroy()
        {
            if (_tutorialGrid != null)
                _tutorialGrid.OnTutorialComplete -= OnTutorialSolved;
        }

        // ── Screen management ─────────────────────────────────────────────

        private void ShowScreen(int index)
        {
            for (int i = 0; i < _screens.Length; i++)
            {
                if (i == index)
                {
                    _screens[i].RemoveFromClassList(ClassHidden);
                    _screens[i].style.opacity = 1f;
                }
                else
                {
                    _screens[i].AddToClassList(ClassHidden);
                }
            }
            _currentScreen = index;
        }

        private void AdvanceScreen()
        {
            if (_currentScreen >= _screens.Length - 1) return;

            VisualElement current = _screens[_currentScreen];
            VisualElement next    = _screens[_currentScreen + 1];
            int           nextIdx = _currentScreen + 1;

            next.style.opacity = 0f;
            next.RemoveFromClassList(ClassHidden);

            DOVirtual.Float(1f, 0f, FadeDuration, v => current.style.opacity = v)
                .OnComplete(() =>
                {
                    current.AddToClassList(ClassHidden);
                    _currentScreen = nextIdx;
                    DOVirtual.Float(0f, 1f, FadeDuration, v => next.style.opacity = v);
                });
        }

        // ── Callbacks ─────────────────────────────────────────────────────

        private void OnTutorialSolved()
        {
            _continueBtns[2]?.RemoveFromClassList(ClassHidden);
        }

        private void OnEnterGallery()
        {
            PersistentCanvasController.Instance?.ShowNavBar();
            GameManager.Instance?.LoadDashboard();
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void BindElements()
        {
            var root = _document.rootVisualElement;

            _screens = new VisualElement[]
            {
                root.Q(NameScreen0),
                root.Q(NameScreen1),
                root.Q(NameScreen2),
                root.Q(NameScreen3),
            };

            _continueBtns = new Button[]
            {
                root.Q<Button>(NameBtn0),
                root.Q<Button>(NameBtn1),
                root.Q<Button>(NameBtn2),
                root.Q<Button>(NameBtn3),
            };

            _rankLabel = root.Q<Label>(NameRankLabel);

            _continueBtns[0]?.RegisterCallback<ClickEvent>(_ => AdvanceScreen());
            _continueBtns[1]?.RegisterCallback<ClickEvent>(_ => AdvanceScreen());
            _continueBtns[2]?.RegisterCallback<ClickEvent>(_ => AdvanceScreen());
            _continueBtns[3]?.RegisterCallback<ClickEvent>(_ => OnEnterGallery());
        }

        private void PopulateRankLabel()
        {
            if (_rankLabel == null) return;
            var rank = PlayerDataManager.Instance != null
                ? PlayerDataManager.Instance.Rank
                : CuratorRank.Neophyte;
            _rankLabel.text = rank + " Curator";
        }
    }
}

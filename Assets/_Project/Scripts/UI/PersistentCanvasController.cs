using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using ChromaLogic.Managers;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Persistent singleton that owns the bottom navigation bar and shared UI chrome
    /// for the Chroma-Logic Gallery session.
    /// <para>
    /// Survives scene loads via <c>DontDestroyOnLoad</c>. Place this component on the
    /// same persistent Bootstrap GameObject as the manager singletons, alongside a
    /// <see cref="UIDocument"/> component that references the navigation bar UXML asset.
    /// </para>
    /// <para>
    /// UI elements are located by name at start-up via <c>rootVisualElement.Q&lt;&gt;</c>.
    /// Name strings are held in private constants — they must match the corresponding
    /// <c>name</c> attributes in the UXML. Active-tab state is managed through the USS
    /// class <c>tab--active</c>; overlay and toast visibility through the class
    /// <c>hidden</c>. C# never touches <c>style</c> directly.
    /// </para>
    /// <para>
    /// Scene routing is delegated entirely to <see cref="GameManager"/> — this controller
    /// contains no scene-name strings of its own; all four tab constants alias the
    /// <see cref="GameManager"/> scene constants.
    /// </para>
    /// </summary>
    public sealed class PersistentCanvasController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────

        /// <summary>The single active instance. <c>null</c> before the Bootstrap
        /// scene has loaded.</summary>
        public static PersistentCanvasController Instance { get; private set; }

        // ── Tab scene name constants ───────────────────────────────────────

        /// <summary>Scene name for the Gallery tab. Aliases <see cref="GameManager.SceneGallery"/>.</summary>
        public const string TabGallery  = GameManager.SceneGallery;

        /// <summary>Scene name for the Solve tab. Aliases <see cref="GameManager.SceneSolve"/>.</summary>
        public const string TabSolve    = GameManager.SceneSolve;

        /// <summary>Scene name for the Meditate tab. Aliases <see cref="GameManager.SceneMeditation"/>.</summary>
        public const string TabMeditate = GameManager.SceneMeditation;

        /// <summary>Scene name for the Archive tab. Aliases <see cref="GameManager.SceneArchive"/>.</summary>
        public const string TabArchive  = GameManager.SceneArchive;

        // ── UXML element name constants ───────────────────────────────────

        // Must match the `name` attribute of each element in the navigation bar UXML.
        private const string NameGalleryTab     = "tab-gallery";
        private const string NameSolveTab       = "tab-solve";
        private const string NameMeditateTab    = "tab-meditate";
        private const string NameArchiveTab     = "tab-archive";
        private const string NameLoadingOverlay = "loading-overlay";
        private const string NameToastRoot      = "toast-root";
        private const string NameToastLabel     = "toast-label";

        // ── USS class constants ───────────────────────────────────────────

        private const string ClassTabActive = "tab--active";
        private const string ClassHidden    = "hidden";

        // ── Private UI state ──────────────────────────────────────────────

        private UIDocument      _document;
        private Button          _galleryTab;
        private Button          _solveTab;
        private Button          _meditateTab;
        private Button          _archiveTab;
        private VisualElement   _loadingOverlay;
        private VisualElement   _toastRoot;
        private Label           _toastLabel;

        private Coroutine _toastCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                Debug.LogError("[PersistentCanvasController] UIDocument component missing. " +
                               "Add a UIDocument to this GameObject and assign the nav-bar UXML asset.");
                return;
            }

            BindElements();
            WireTabCallbacks();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Loads the scene identified by <paramref name="sceneName"/> via the matching
        /// <see cref="GameManager"/> routing method, then highlights the corresponding tab.
        /// <para>
        /// Does nothing and logs a warning if <paramref name="sceneName"/> does not match
        /// one of the four navigation tab constants.
        /// Does nothing if <see cref="GameManager.Instance"/> is <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="sceneName">
        /// One of <see cref="TabGallery"/>, <see cref="TabSolve"/>,
        /// <see cref="TabMeditate"/>, or <see cref="TabArchive"/>.
        /// </param>
        public void NavigateTo(string sceneName)
        {
            if (GameManager.Instance == null) return;

            switch (sceneName)
            {
                case TabGallery:  GameManager.Instance.LoadDashboard();  break;
                case TabSolve:    GameManager.Instance.LoadSolve();      break;
                case TabMeditate: GameManager.Instance.LoadMeditation(); break;
                case TabArchive:  GameManager.Instance.LoadArchive();    break;
                default:
                    Debug.LogWarning("[PersistentCanvasController] No navigation route for scene: " + sceneName);
                    return;
            }

            SetActiveTab(sceneName);
        }

        /// <summary>
        /// Updates tab highlight state so the tab matching <paramref name="sceneName"/>
        /// carries the <c>tab--active</c> USS class and all others do not.
        /// <para>
        /// Call this when the active scene changes externally (e.g. from the
        /// <see cref="GameManager"/> on launch) without going through
        /// <see cref="NavigateTo"/>.
        /// </para>
        /// </summary>
        /// <param name="sceneName">The scene name whose tab should appear active.</param>
        public void SetActiveTab(string sceneName)
        {
            SetTabState(_galleryTab,  sceneName == TabGallery);
            SetTabState(_solveTab,    sceneName == TabSolve);
            SetTabState(_meditateTab, sceneName == TabMeditate);
            SetTabState(_archiveTab,  sceneName == TabArchive);
        }

        /// <summary>
        /// Makes the full-screen loading overlay visible by removing the <c>hidden</c>
        /// USS class. Call before initiating a scene load when a visual transition is
        /// needed; pair with <see cref="HideLoadingOverlay"/> once the new scene is ready.
        /// </summary>
        public void ShowLoadingOverlay() => _loadingOverlay?.RemoveFromClassList(ClassHidden);

        /// <summary>
        /// Hides the full-screen loading overlay by adding the <c>hidden</c> USS class.
        /// </summary>
        public void HideLoadingOverlay() => _loadingOverlay?.AddToClassList(ClassHidden);

        /// <summary>
        /// Displays a brief, non-blocking toast notification for <paramref name="duration"/>
        /// seconds, then dismisses it automatically.
        /// <para>
        /// If a toast is already visible when this is called, it is cancelled immediately
        /// and replaced by the new message. Per the design tone, keep messages calm and
        /// concise — one short sentence.
        /// </para>
        /// </summary>
        /// <param name="message">The message to display in the toast.</param>
        /// <param name="duration">Seconds the toast remains visible. Default 2 seconds.</param>
        public void ShowToast(string message, float duration = 2f)
        {
            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(ToastRoutine(message, duration));
        }

        // ── Private helpers ───────────────────────────────────────────────

        private void BindElements()
        {
            VisualElement root = _document.rootVisualElement;
            _galleryTab     = root.Q<Button>(NameGalleryTab);
            _solveTab       = root.Q<Button>(NameSolveTab);
            _meditateTab    = root.Q<Button>(NameMeditateTab);
            _archiveTab     = root.Q<Button>(NameArchiveTab);
            _loadingOverlay = root.Q<VisualElement>(NameLoadingOverlay);
            _toastRoot      = root.Q<VisualElement>(NameToastRoot);
            _toastLabel     = root.Q<Label>(NameToastLabel);
        }

        private void WireTabCallbacks()
        {
            _galleryTab? .RegisterCallback<ClickEvent>(_ => NavigateTo(TabGallery));
            _solveTab?   .RegisterCallback<ClickEvent>(_ => NavigateTo(TabSolve));
            _meditateTab?.RegisterCallback<ClickEvent>(_ => NavigateTo(TabMeditate));
            _archiveTab? .RegisterCallback<ClickEvent>(_ => NavigateTo(TabArchive));
        }

        private static void SetTabState(Button tab, bool active)
        {
            if (tab == null) return;
            if (active) tab.AddToClassList(ClassTabActive);
            else        tab.RemoveFromClassList(ClassTabActive);
        }

        private IEnumerator ToastRoutine(string message, float duration)
        {
            if (_toastLabel != null) _toastLabel.text = message;
            _toastRoot?.RemoveFromClassList(ClassHidden);
            yield return new WaitForSeconds(duration);
            _toastRoot?.AddToClassList(ClassHidden);
            _toastCoroutine = null;
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChromaLogic.Managers
{
    /// <summary>
    /// Central orchestrator for the Chroma-Logic Gallery session lifecycle.
    /// <para>
    /// Owns all scene-routing logic and determines the correct entry point on each
    /// launch. Place this component on the same persistent Bootstrap GameObject as
    /// <see cref="PlayerDataManager"/> and <see cref="ProgressionManager"/>.
    /// </para>
    /// <para>
    /// Execution order is set to <c>+100</c> so this component's <c>Awake</c> runs
    /// after <see cref="PlayerDataManager"/> and <see cref="ProgressionManager"/> have
    /// initialised their singletons at the default order of <c>0</c>.
    /// </para>
    /// <para>
    /// Scene names are exposed as <c>public const string</c> fields so no other script
    /// ever hardcodes a scene name string.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class GameManager : MonoBehaviour
    {
        // ── Scene name constants ───────────────────────────────────────────

        /// <summary>Scene loaded for first-time Curators and tutorial flow (Phase 3).</summary>
        public const string SceneOnboarding = "Onboarding";

        /// <summary>Main Gallery Dashboard scene shown on every returning launch (Phase 6).</summary>
        public const string SceneGallery    = "Gallery";

        /// <summary>Full 9×9 Vessel solve scene (Phase 4).</summary>
        public const string SceneSolve      = "Solve";

        /// <summary>Daily Meditation 4×4 scene (Phase 9).</summary>
        public const string SceneMeditation = "Meditation";

        /// <summary>Curator Archive / Constructs scene (Phase 6).</summary>
        public const string SceneArchive    = "Archive";

        // ── Singleton ─────────────────────────────────────────────────────

        /// <summary>The single active instance. <c>null</c> before the Bootstrap
        /// scene has loaded.</summary>
        public static GameManager Instance { get; private set; }

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

            // Fail fast in the editor if the Bootstrap GameObject is misconfigured.
            if (PlayerDataManager.Instance == null)
                Debug.LogError("[GameManager] PlayerDataManager.Instance is null. " +
                               "Add PlayerDataManager to the same Bootstrap GameObject.");
            if (ProgressionManager.Instance == null)
                Debug.LogError("[GameManager] ProgressionManager.Instance is null. " +
                               "Add ProgressionManager to the same Bootstrap GameObject.");
        }

        private void Start()
        {
            // Check before UpdateStreak so the first-ever launch flag is still intact.
            bool isFirstLaunch = PlayerDataManager.Instance.LastPlayedDate == DateTime.MinValue;

            PlayerDataManager.Instance.UpdateStreak();

            if (isFirstLaunch)
                LoadOnboarding();
            else
                LoadDashboard();
        }

        // ── Scene routing ─────────────────────────────────────────────────

        /// <summary>
        /// Loads the Onboarding scene (<see cref="SceneOnboarding"/>).
        /// Called automatically on first launch; also callable from the UI to restart
        /// the tutorial.
        /// </summary>
        public void LoadOnboarding() => SceneManager.LoadScene(SceneOnboarding);

        /// <summary>
        /// Loads the main Gallery Dashboard scene (<see cref="SceneGallery"/>).
        /// This is the default destination for every returning launch.
        /// </summary>
        public void LoadDashboard() => SceneManager.LoadScene(SceneGallery);

        /// <summary>
        /// Loads the Vessel solve scene (<see cref="SceneSolve"/>).
        /// Call this when the Curator selects a Vessel to solve from the Gallery.
        /// </summary>
        public void LoadSolve() => SceneManager.LoadScene(SceneSolve);

        /// <summary>
        /// Loads the Daily Meditation scene (<see cref="SceneMeditation"/>).
        /// Call this when the Curator opens the Meditation Vessel for the day.
        /// </summary>
        public void LoadMeditation() => SceneManager.LoadScene(SceneMeditation);

        /// <summary>
        /// Loads the Curator Archive scene (<see cref="SceneArchive"/>).
        /// Call this when the Curator navigates to their Constructs collection.
        /// </summary>
        public void LoadArchive() => SceneManager.LoadScene(SceneArchive);
    }
}

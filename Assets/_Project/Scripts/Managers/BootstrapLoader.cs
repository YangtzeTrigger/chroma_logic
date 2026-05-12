using UnityEngine;

namespace ChromaLogic.Managers
{
    /// <summary>
    /// Ensures the manager singletons are initialised whenever any scene is entered,
    /// regardless of whether the Bootstrap scene was loaded first.
    /// <para>
    /// Add this component to every scene that may be opened directly in the Unity
    /// Editor during development (Onboarding, Gallery, Solve, etc.). In a production
    /// build the Bootstrap scene always loads first, so this component becomes a
    /// silent no-op — <see cref="GameManager.Instance"/> is already set before any
    /// other scene's <c>Awake</c> runs.
    /// </para>
    /// <para>
    /// When a scene is opened directly (e.g. for isolated testing), this component
    /// loads the <c>GameManager</c> prefab from <c>Resources/</c> and instantiates it.
    /// <see cref="GameManager.Awake"/> immediately assigns the singleton, calls
    /// <c>DontDestroyOnLoad</c>, and initialises <see cref="PlayerDataManager"/>,
    /// <see cref="ProgressionManager"/>, and <see cref="AudioManager"/>.
    /// </para>
    /// <para>
    /// If the prefab is missing, a descriptive error is logged with instructions to
    /// run the setup menu item.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class BootstrapLoader : MonoBehaviour
    {
        // ── Prefab path ────────────────────────────────────────────────────

        // Relative to any Resources/ folder in the project.
        // Resolved to Assets/_Project/Resources/GameManager.prefab by the
        // BootstrapSceneBuilder Editor script.
        private const string PrefabResourcePath = "GameManager";

        // ── Unity lifecycle ───────────────────────────────────────────────

        /// <summary>
        /// Checks whether <see cref="GameManager.Instance"/> is already set.
        /// If it is, the Bootstrap scene was loaded first and nothing needs to be done.
        /// If it is not, loads and instantiates the <c>GameManager</c> prefab from
        /// <c>Resources/</c> so that all manager singletons become available immediately.
        /// </summary>
        private void Awake()
        {
            if (GameManager.Instance != null) return;

            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError(
                    "[BootstrapLoader] GameManager prefab not found at Resources/" +
                    PrefabResourcePath + ". " +
                    "Run Chroma-Logic ▸ Setup ▸ Create Bootstrap Scene in the Unity menu.");
                return;
            }

            Instantiate(prefab);
        }
    }
}

using ChromaLogic.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChromaLogic.Editor
{
    /// <summary>
    /// One-time Editor utility that creates the Bootstrap scene and the GameManager
    /// prefab used by <see cref="BootstrapLoader"/> at runtime.
    /// <para>
    /// Run via <b>Chroma-Logic ▸ Setup ▸ Create Bootstrap Scene</b> after the project
    /// has compiled successfully. The command is safe to re-run — it prompts before
    /// overwriting existing assets.
    /// </para>
    /// <para>
    /// Outputs:
    /// <list type="bullet">
    ///   <item><description>
    ///     <c>Assets/_Project/Scenes/Bootstrap.unity</c> — the production entry-point
    ///     scene containing a standalone <c>GameManager</c> GameObject with all four
    ///     manager components attached.
    ///   </description></item>
    ///   <item><description>
    ///     <c>Assets/_Project/Resources/GameManager.prefab</c> — the prefab instantiated
    ///     by <see cref="BootstrapLoader"/> when any other scene is opened directly in
    ///     the Editor.
    ///   </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// After running: open <b>File ▸ Build Settings</b>, add Bootstrap.unity, and drag
    /// it to index 0 so it loads first in every build.
    /// </para>
    /// </summary>
    internal static class BootstrapSceneBuilder
    {
        private const string ScenePath  = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string PrefabPath = "Assets/_Project/Resources/GameManager.prefab";
        private const string ResourcesFolder = "Assets/_Project/Resources";

        [MenuItem("Chroma-Logic/Setup/Create Bootstrap Scene")]
        private static void CreateBootstrapScene()
        {
            // Guard: prompt before overwriting an existing scene.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite Bootstrap Scene?",
                    ScenePath + " already exists.\n\nOverwrite it and regenerate the GameManager prefab?",
                    "Overwrite", "Cancel");

                if (!overwrite) return;
            }

            // 1 — Ensure the Resources folder exists.
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");

            // 2 — Create an empty scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3 — Build the GameManager GameObject with all four manager components.
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
            go.AddComponent<PlayerDataManager>();
            go.AddComponent<ProgressionManager>();
            go.AddComponent<AudioManager>();

            // 4 — Save as a Resources prefab for BootstrapLoader to instantiate.
            //     The scene GO remains standalone — not linked to the prefab.
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath, out bool prefabSuccess);
            if (!prefabSuccess)
            {
                Debug.LogError("[BootstrapSceneBuilder] Failed to save prefab at " + PrefabPath);
                return;
            }

            // 5 — Save the scene.
            bool sceneSaved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!sceneSaved)
            {
                Debug.LogError("[BootstrapSceneBuilder] Failed to save scene at " + ScenePath);
                return;
            }

            AssetDatabase.Refresh();

            Debug.Log("[BootstrapSceneBuilder] Bootstrap scene created: " + ScenePath);
            Debug.Log("[BootstrapSceneBuilder] GameManager prefab created: " + PrefabPath);
            Debug.Log("[BootstrapSceneBuilder] Next step: File ▸ Build Settings ▸ " +
                      "Add Open Scenes, then drag Bootstrap to index 0.");
        }
    }
}

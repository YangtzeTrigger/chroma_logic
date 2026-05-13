using ChromaLogic.Managers;
using ChromaLogic.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.Editor
{
    /// <summary>
    /// One-time Editor utility that creates the Meditation scene and wires up
    /// <see cref="MeditationController"/>.
    /// <para>
    /// Run via <b>Chroma-Logic ▸ Setup ▸ Create Meditation Scene</b> after the project
    /// has compiled successfully. Safe to re-run — prompts before overwriting.
    /// </para>
    /// <para>
    /// After running:
    /// <list type="bullet">
    ///   <item><description>
    ///     Open <b>File ▸ Build Settings</b> and add <c>Meditation.unity</c> to the
    ///     scene list.
    ///   </description></item>
    ///   <item><description>
    ///     Assign a <b>PanelSettings</b> asset to the UIDocument component if the
    ///     default one has not been auto-assigned by Unity.
    ///   </description></item>
    /// </list>
    /// </para>
    /// </summary>
    internal static class MeditationSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Meditation.unity";
        private const string UxmlPath  = "Assets/_Project/UI/Meditation.uxml";

        [MenuItem("Chroma-Logic/Setup/Create Meditation Scene")]
        private static void CreateMeditationScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite Meditation Scene?",
                    ScenePath + " already exists.\n\nOverwrite it?",
                    "Overwrite", "Cancel");

                if (!overwrite) return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("BootstrapLoader");
            bootstrap.AddComponent<BootstrapLoader>();

            var go    = new GameObject("Meditation");
            var uiDoc = go.AddComponent<UIDocument>();

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTreeAsset != null)
                uiDoc.visualTreeAsset = visualTreeAsset;
            else
                Debug.LogWarning("[MeditationSceneBuilder] Meditation.uxml not found at " + UxmlPath +
                                 ". Assign it manually on the UIDocument component.");

            go.AddComponent<MeditationController>();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError("[MeditationSceneBuilder] Failed to save scene at " + ScenePath);
                return;
            }

            AssetDatabase.Refresh();

            Debug.Log("[MeditationSceneBuilder] Meditation scene created: " + ScenePath);
            Debug.Log("[MeditationSceneBuilder] Next steps:\n" +
                      "  1. File ▸ Build Settings ▸ add Meditation.unity to the scene list.\n" +
                      "  2. Assign a PanelSettings asset to the UIDocument if needed.");
        }
    }
}

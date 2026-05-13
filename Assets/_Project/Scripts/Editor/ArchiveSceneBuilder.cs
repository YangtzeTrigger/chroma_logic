using ChromaLogic.Managers;
using ChromaLogic.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.Editor
{
    /// <summary>
    /// One-time Editor utility that creates the Archive scene and wires up
    /// <see cref="ArchiveController"/>.
    /// <para>
    /// Run via <b>Chroma-Logic ▸ Setup ▸ Create Archive Scene</b> after the project
    /// has compiled successfully. Safe to re-run — prompts before overwriting.
    /// </para>
    /// <para>
    /// After running:
    /// <list type="bullet">
    ///   <item><description>
    ///     Open <b>File ▸ Build Settings</b> and add <c>Archive.unity</c> to the scene list.
    ///   </description></item>
    ///   <item><description>
    ///     Assign a <b>PanelSettings</b> asset to the UIDocument component if the
    ///     default one has not been auto-assigned by Unity.
    ///   </description></item>
    /// </list>
    /// </para>
    /// </summary>
    internal static class ArchiveSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Archive.unity";
        private const string UxmlPath  = "Assets/_Project/UI/Archive.uxml";

        [MenuItem("Chroma-Logic/Setup/Create Archive Scene")]
        private static void CreateArchiveScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite Archive Scene?",
                    ScenePath + " already exists.\n\nOverwrite it?",
                    "Overwrite", "Cancel");

                if (!overwrite) return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("BootstrapLoader");
            bootstrap.AddComponent<BootstrapLoader>();

            var go    = new GameObject("Archive");
            var uiDoc = go.AddComponent<UIDocument>();

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTreeAsset != null)
                uiDoc.visualTreeAsset = visualTreeAsset;
            else
                Debug.LogWarning("[ArchiveSceneBuilder] Archive.uxml not found at " + UxmlPath +
                                 ". Assign it manually on the UIDocument component.");

            go.AddComponent<ArchiveController>();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError("[ArchiveSceneBuilder] Failed to save scene at " + ScenePath);
                return;
            }

            AssetDatabase.Refresh();

            Debug.Log("[ArchiveSceneBuilder] Archive scene created: " + ScenePath);
            Debug.Log("[ArchiveSceneBuilder] Next steps:\n" +
                      "  1. File ▸ Build Settings ▸ add Archive.unity to the scene list.\n" +
                      "  2. Assign a PanelSettings asset to the UIDocument if needed.");
        }
    }
}

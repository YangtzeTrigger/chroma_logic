using ChromaLogic.Managers;
using ChromaLogic.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.Editor
{
    /// <summary>
    /// One-time Editor utility that creates the Gallery scene and wires up
    /// <see cref="GalleryController"/>.
    /// <para>
    /// Run via <b>Chroma-Logic ▸ Setup ▸ Create Gallery Scene</b> after the project
    /// has compiled successfully. Safe to re-run — prompts before overwriting.
    /// </para>
    /// <para>
    /// After running:
    /// <list type="bullet">
    ///   <item><description>
    ///     Open <b>File ▸ Build Settings</b> and add <c>Gallery.unity</c> to the scene list.
    ///   </description></item>
    ///   <item><description>
    ///     Assign a <b>PanelSettings</b> asset to the UIDocument component if the
    ///     default one has not been auto-assigned by Unity.
    ///   </description></item>
    /// </list>
    /// </para>
    /// </summary>
    internal static class GallerySceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gallery.unity";
        private const string UxmlPath  = "Assets/_Project/UI/Gallery.uxml";

        [MenuItem("Chroma-Logic/Setup/Create Gallery Scene")]
        private static void CreateGalleryScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite Gallery Scene?",
                    ScenePath + " already exists.\n\nOverwrite it?",
                    "Overwrite", "Cancel");

                if (!overwrite) return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("BootstrapLoader");
            bootstrap.AddComponent<BootstrapLoader>();

            var go    = new GameObject("Gallery");
            var uiDoc = go.AddComponent<UIDocument>();

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTreeAsset != null)
                uiDoc.visualTreeAsset = visualTreeAsset;
            else
                Debug.LogWarning("[GallerySceneBuilder] Gallery.uxml not found at " + UxmlPath +
                                 ". Assign it manually on the UIDocument component.");

            go.AddComponent<GalleryController>();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError("[GallerySceneBuilder] Failed to save scene at " + ScenePath);
                return;
            }

            AssetDatabase.Refresh();

            Debug.Log("[GallerySceneBuilder] Gallery scene created: " + ScenePath);
            Debug.Log("[GallerySceneBuilder] Next steps:\n" +
                      "  1. File ▸ Build Settings ▸ add Gallery.unity to the scene list.\n" +
                      "  2. Assign a PanelSettings asset to the UIDocument if needed.");
        }
    }
}

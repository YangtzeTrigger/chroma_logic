using ChromaLogic.Managers;
using ChromaLogic.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.Editor
{
    /// <summary>
    /// One-time Editor utility that creates the Jigsaw scene and wires up
    /// <see cref="JigsawController"/>.
    /// <para>
    /// Run via <b>Chroma-Logic ▸ Setup ▸ Create Jigsaw Scene</b> after the project
    /// has compiled successfully. Safe to re-run — prompts before overwriting.
    /// </para>
    /// <para>
    /// After running:
    /// <list type="bullet">
    ///   <item><description>
    ///     Open <b>File ▸ Build Settings</b> and add <c>Jigsaw.unity</c> to the scene list.
    ///   </description></item>
    ///   <item><description>
    ///     Assign a <b>PanelSettings</b> asset to the UIDocument component if the
    ///     default one has not been auto-assigned by Unity.
    ///   </description></item>
    ///   <item><description>
    ///     Assign a test <b>Sprite</b> to the <c>TestSprite</c> field on
    ///     <see cref="JigsawController"/> for development testing.
    ///   </description></item>
    /// </list>
    /// </para>
    /// </summary>
    internal static class JigsawSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Jigsaw.unity";
        private const string UxmlPath  = "Assets/_Project/UI/Jigsaw.uxml";

        [MenuItem("Chroma-Logic/Setup/Create Jigsaw Scene")]
        private static void CreateJigsawScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite Jigsaw Scene?",
                    ScenePath + " already exists.\n\nOverwrite it?",
                    "Overwrite", "Cancel");

                if (!overwrite) return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("BootstrapLoader");
            bootstrap.AddComponent<BootstrapLoader>();

            var go    = new GameObject("Jigsaw");
            var uiDoc = go.AddComponent<UIDocument>();

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTreeAsset != null)
                uiDoc.visualTreeAsset = visualTreeAsset;
            else
                Debug.LogWarning("[JigsawSceneBuilder] Jigsaw.uxml not found at " + UxmlPath +
                                 ". Assign it manually on the UIDocument component.");

            go.AddComponent<JigsawController>();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError("[JigsawSceneBuilder] Failed to save scene at " + ScenePath);
                return;
            }

            AssetDatabase.Refresh();

            Debug.Log("[JigsawSceneBuilder] Jigsaw scene created: " + ScenePath);
            Debug.Log("[JigsawSceneBuilder] Next steps:\n" +
                      "  1. File ▸ Build Settings ▸ add Jigsaw.unity to the scene list.\n" +
                      "  2. Assign a PanelSettings asset to the UIDocument if needed.\n" +
                      "  3. Assign a test Sprite to the TestSprite field on JigsawController.");
        }
    }
}

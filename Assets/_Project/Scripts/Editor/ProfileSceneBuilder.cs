using ChromaLogic.Managers;
using ChromaLogic.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.Editor
{
    /// <summary>
    /// One-time Editor utility that creates the Profile scene and wires up
    /// <see cref="ProfileController"/> and <see cref="PrestigeController"/>.
    /// <para>
    /// Run via <b>Chroma-Logic ▸ Setup ▸ Create Profile Scene</b> after the project
    /// has compiled successfully. Safe to re-run — prompts before overwriting.
    /// </para>
    /// <para>
    /// After running:
    /// <list type="bullet">
    ///   <item><description>
    ///     Open <b>File ▸ Build Settings</b> and add <c>Profile.unity</c> to the
    ///     scene list.
    ///   </description></item>
    ///   <item><description>
    ///     Assign a <b>PanelSettings</b> asset to the UIDocument component if the
    ///     default one has not been auto-assigned by Unity.
    ///   </description></item>
    /// </list>
    /// </para>
    /// </summary>
    internal static class ProfileSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Profile.unity";
        private const string UxmlPath  = "Assets/_Project/UI/Profile.uxml";

        [MenuItem("Chroma-Logic/Setup/Create Profile Scene")]
        private static void CreateProfileScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite Profile Scene?",
                    ScenePath + " already exists.\n\nOverwrite it?",
                    "Overwrite", "Cancel");

                if (!overwrite) return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("BootstrapLoader");
            bootstrap.AddComponent<BootstrapLoader>();

            var go    = new GameObject("Profile");
            var uiDoc = go.AddComponent<UIDocument>();

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTreeAsset != null)
                uiDoc.visualTreeAsset = visualTreeAsset;
            else
                Debug.LogWarning("[ProfileSceneBuilder] Profile.uxml not found at " + UxmlPath +
                                 ". Assign it manually on the UIDocument component.");

            go.AddComponent<ProfileController>();
            go.AddComponent<PrestigeController>();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError("[ProfileSceneBuilder] Failed to save scene at " + ScenePath);
                return;
            }

            AssetDatabase.Refresh();

            Debug.Log("[ProfileSceneBuilder] Profile scene created: " + ScenePath);
            Debug.Log("[ProfileSceneBuilder] Next steps:\n" +
                      "  1. File ▸ Build Settings ▸ add Profile.unity to the scene list.\n" +
                      "  2. Assign a PanelSettings asset to the UIDocument if needed.");
        }
    }
}

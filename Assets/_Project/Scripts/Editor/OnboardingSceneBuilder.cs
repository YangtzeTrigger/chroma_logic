using ChromaLogic.Managers;
using ChromaLogic.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChromaLogic.Editor
{
    /// <summary>
    /// One-time Editor utility that creates the Onboarding scene and wires up the
    /// <see cref="OnboardingController"/> and <see cref="TutorialGridController"/> components.
    /// <para>
    /// Run via <b>Chroma-Logic ▸ Setup ▸ Create Onboarding Scene</b> after the project
    /// has compiled successfully. Safe to re-run — prompts before overwriting.
    /// </para>
    /// <para>
    /// After running:
    /// <list type="bullet">
    ///   <item><description>
    ///     Open <b>File ▸ Build Settings</b> and add <c>Onboarding.unity</c> to the scene list.
    ///   </description></item>
    ///   <item><description>
    ///     Assign a <b>PanelSettings</b> asset to the UIDocument component if the
    ///     default one has not been auto-assigned by Unity.
    ///   </description></item>
    /// </list>
    /// </para>
    /// </summary>
    internal static class OnboardingSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Onboarding.unity";
        private const string UxmlPath  = "Assets/_Project/UI/Onboarding.uxml";
        private const string UiFolder  = "Assets/_Project/UI";

        [MenuItem("Chroma-Logic/Setup/Create Onboarding Scene")]
        private static void CreateOnboardingScene()
        {
            // Guard: prompt before overwriting an existing scene.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite Onboarding Scene?",
                    ScenePath + " already exists.\n\nOverwrite it?",
                    "Overwrite", "Cancel");

                if (!overwrite) return;
            }

            // Ensure the UI folder exists so the UXML asset can be found.
            if (!AssetDatabase.IsValidFolder(UiFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "UI");
                AssetDatabase.Refresh();
            }

            // Create an empty scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // BootstrapLoader ensures managers are available when this scene is
            // opened directly in the editor without going through Bootstrap first.
            var bootstrap = new GameObject("BootstrapLoader");
            bootstrap.AddComponent<BootstrapLoader>();

            // Build the Onboarding GameObject.
            var go = new GameObject("Onboarding");

            // UIDocument — assign UXML asset if it already exists on disk.
            var uiDoc = go.AddComponent<UIDocument>();
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTreeAsset != null)
                uiDoc.visualTreeAsset = visualTreeAsset;
            else
                Debug.LogWarning("[OnboardingSceneBuilder] Onboarding.uxml not found at " + UxmlPath +
                                 ". Assign it manually on the UIDocument component.");

            // Add the two controller components.
            go.AddComponent<OnboardingController>();
            go.AddComponent<TutorialGridController>();

            // Save the scene.
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError("[OnboardingSceneBuilder] Failed to save scene at " + ScenePath);
                return;
            }

            AssetDatabase.Refresh();

            Debug.Log("[OnboardingSceneBuilder] Onboarding scene created: " + ScenePath);
            Debug.Log("[OnboardingSceneBuilder] Next steps:\n" +
                      "  1. File ▸ Build Settings ▸ add Onboarding.unity to the scene list.\n" +
                      "  2. Assign a PanelSettings asset to the UIDocument if needed.");
        }
    }
}

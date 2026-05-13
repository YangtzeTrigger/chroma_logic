using ChromaLogic.Gameplay;
using ChromaLogic.Managers;
using ChromaLogic.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.Editor
{
    /// <summary>
    /// One-time Editor utility that creates the Solve scene and wires up
    /// <see cref="SolveController"/>, <see cref="GridRenderer"/>, and
    /// <see cref="TileTrayController"/>.
    /// <para>
    /// Run via <b>Chroma-Logic ▸ Setup ▸ Create Solve Scene</b> after the project
    /// has compiled successfully. Safe to re-run — prompts before overwriting.
    /// </para>
    /// <para>
    /// After running:
    /// <list type="bullet">
    ///   <item><description>
    ///     Open <b>File ▸ Build Settings</b> and add <c>Solve.unity</c> to the scene list.
    ///   </description></item>
    ///   <item><description>
    ///     Assign a <b>PanelSettings</b> asset to the UIDocument component if the
    ///     default one has not been auto-assigned by Unity.
    ///   </description></item>
    /// </list>
    /// </para>
    /// </summary>
    internal static class SolveSceneBuilder
    {
        private const string ScenePath     = "Assets/_Project/Scenes/Solve.unity";
        private const string UxmlPath      = "Assets/_Project/UI/Solve.uxml";
        private const string RevealUxmlPath = "Assets/_Project/UI/Reveal.uxml";

        [MenuItem("Chroma-Logic/Setup/Create Solve Scene")]
        private static void CreateSolveScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite Solve Scene?",
                    ScenePath + " already exists.\n\nOverwrite it?",
                    "Overwrite", "Cancel");

                if (!overwrite) return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("BootstrapLoader");
            bootstrap.AddComponent<BootstrapLoader>();

            var go    = new GameObject("Solve");
            var uiDoc = go.AddComponent<UIDocument>();

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTreeAsset != null)
                uiDoc.visualTreeAsset = visualTreeAsset;
            else
                Debug.LogWarning("[SolveSceneBuilder] Solve.uxml not found at " + UxmlPath +
                                 ". Assign it manually on the UIDocument component.");

            go.AddComponent<SolveController>();
            go.AddComponent<GridRenderer>();
            go.AddComponent<TileTrayController>();

            // RevealSequence needs its own UIDocument (Reveal.uxml), so it lives on a
            // separate GameObject. Its UIDocument sort order must be higher than Solve's
            // so the overlay renders on top.
            var revealGo    = new GameObject("Reveal");
            var revealDoc   = revealGo.AddComponent<UIDocument>();
            revealDoc.sortingOrder = 1;

            var revealUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(RevealUxmlPath);
            if (revealUxml != null)
                revealDoc.visualTreeAsset = revealUxml;
            else
                Debug.LogWarning("[SolveSceneBuilder] Reveal.uxml not found at " + RevealUxmlPath +
                                 ". Assign it manually on the Reveal UIDocument component.");

            var revealSeq  = revealGo.AddComponent<RevealSequence>();
            var solveCtrl  = go.GetComponent<SolveController>();
            if (solveCtrl != null)
            {
                var so = new SerializedObject(solveCtrl);
                so.FindProperty("_revealSequence").objectReferenceValue = revealSeq;
                so.ApplyModifiedProperties();
            }

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError("[SolveSceneBuilder] Failed to save scene at " + ScenePath);
                return;
            }

            AssetDatabase.Refresh();

            Debug.Log("[SolveSceneBuilder] Solve scene created: " + ScenePath);
            Debug.Log("[SolveSceneBuilder] Next steps:\n" +
                      "  1. File ▸ Build Settings ▸ add Solve.unity to the scene list.\n" +
                      "  2. Assign a PanelSettings asset to both UIDocument components if needed.\n" +
                      "  3. On the Reveal GO: assign RevealSprite, VesselName, PackName on the RevealSequence component.\n" +
                      "  4. On SolveController: assign the RevealSequence component reference in the Inspector.");
        }
    }
}

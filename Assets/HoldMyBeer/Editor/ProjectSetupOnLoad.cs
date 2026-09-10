using UnityEditor;
using UnityEngine;

namespace HoldMyBeer.Editor
{
    /// <summary>
    /// Runs the generator once, when the Boot scene is missing, so a fresh clone is
    /// playable right after opening it. Once generated, commit the scenes and
    /// prefabs like any other asset: from then on they are yours to edit by hand and
    /// this never runs again.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetupOnLoad
    {
        private const string BootScenePath = "Assets/HoldMyBeer/Scenes/Boot.unity";

        static ProjectSetupOnLoad()
        {
            // Delayed: the asset database and the scripting domain must be ready.
            EditorApplication.delayCall += RunIfNeeded;
        }

        private static void RunIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath) != null)
            {
                return;
            }

            Debug.Log("[Hold My Beer] First run detected: generating scenes and prefabs...");
            ProjectAssetGenerator.GenerateAll();
        }
    }
}

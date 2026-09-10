using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HoldMyBeer.Networking.Steam.Editor
{
    /// <summary>
    /// Keeps steam_appid.txt in step with <see cref="SteamAppId"/>, in the editor and
    /// beside every build.
    ///
    /// Steam reads this file to learn which app is starting. Unity does not know it
    /// exists and will not copy it, so a build shipped without it fails at
    /// SteamClient.Init with a message that says nothing useful. Generating it removes
    /// a manual step that is only ever noticed once it has been forgotten.
    ///
    /// This lives in an assembly nested inside the Steam folder so that deleting that
    /// folder removes the Steam mode whole, tooling included.
    /// </summary>
    public sealed class SteamAppIdFile : IPostprocessBuildWithReport
    {
        private const string FileName = "steam_appid.txt";

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            var executable = report.summary.outputPath;
            if (string.IsNullOrEmpty(executable))
            {
                return;
            }

            var directory = Path.GetDirectoryName(executable);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            Write(Path.Combine(directory, FileName));
        }

        /// <summary>
        /// The editor needs the file too, at the project root - the working directory
        /// the editor runs from.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void EnsureProjectRootFile()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot != null)
            {
                Write(Path.Combine(projectRoot.FullName, FileName));
            }
        }

        private static void Write(string path)
        {
            var expected = SteamAppId.Value.ToString();

            // Only touch the file when it is actually wrong: rewriting it every domain
            // reload would churn the working tree for nothing.
            if (File.Exists(path) && File.ReadAllText(path).Trim() == expected)
            {
                return;
            }

            File.WriteAllText(path, expected);
            Debug.Log($"[Steam] Wrote {path} with AppID {expected}.");
        }
    }
}

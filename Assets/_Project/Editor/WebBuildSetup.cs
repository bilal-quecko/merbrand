#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MeraBrand.Expo.Editor
{
    public static class WebBuildSetup
    {
        private const string BuildFolder = "Builds/WebGL";

        [MenuItem("Mera Brand/Web/Configure Web Build")]
        public static void ConfigureWebBuild()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Web",
                    "Unity Web Build Support is not installed for this Editor version. Add the Web module from Unity Hub and run this command again.",
                    "OK");
                return;
            }

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Web",
                    "Unity could not switch the active build target to Web.",
                    "OK");
                return;
            }

            // First-pass Web settings. Lighting, backend sync, and deeper quality tuning
            // are intentionally deferred to later phases.
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Mera Brand - Web",
                "Web build target configured.\n\n" +
                "Current first-pass settings:\n" +
                "• Data caching enabled\n" +
                "• Gzip compression\n" +
                "• Decompression fallback enabled for easier initial hosting\n" +
                "• PC build remains available as a separate target\n\n" +
                "Use Mera Brand → Web → Build Development Web for the first browser test.",
                "OK");
        }

        [MenuItem("Mera Brand/Web/Build Development Web")]
        public static void BuildDevelopmentWeb()
        {
            BuildWeb(true);
        }

        [MenuItem("Mera Brand/Web/Build Production Web")]
        public static void BuildProductionWeb()
        {
            BuildWeb(false);
        }

        private static void BuildWeb(bool development)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Web",
                    "Unity Web Build Support is not installed.",
                    "OK");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                    return;
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && File.Exists(scene.path))
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Web",
                    "No enabled scenes were found in Build Settings.",
                    "OK");
                return;
            }

            Directory.CreateDirectory(BuildFolder);

            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = BuildFolder,
                target = BuildTarget.WebGL,
                options = development ? BuildOptions.Development : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Web",
                    $"Web build completed successfully.\n\nOutput: {BuildFolder}\nSize: {summary.totalSize / (1024f * 1024f):0.0} MB",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Web",
                    $"Web build failed with result: {summary.result}.\nCheck the Unity Console for the exact error.",
                    "OK");
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using System.Linq;
using MeraBrand.Expo.Core;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
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
                .Where(scene => Path.GetFileNameWithoutExtension(scene.path) != SceneNames.Exhibition)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Web",
                    "No boot/menu scenes were found in Build Settings.",
                    "OK");
                return;
            }

            EditorBuildSettingsScene[] originalBuildScenes = EditorBuildSettings.scenes;
            try
            {
                // Addressables must see the same built-in scenes as the Web player.
                // Restore the desktop Build Settings even if a build fails.
                EditorBuildSettings.scenes = originalBuildScenes
                    .Select(scene => new EditorBuildSettingsScene(scene.path,
                        scene.enabled && Path.GetFileNameWithoutExtension(scene.path) != SceneNames.Exhibition))
                    .ToArray();

                try
                {
                    WebRemoteExhibitionSetup.Configure();
                    ClearPreviousRemoteContent();
                    AddressableAssetSettings.BuildPlayerContent(out var contentResult);
                    if (!string.IsNullOrEmpty(contentResult.Error))
                        throw new System.InvalidOperationException(contentResult.Error);
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                    if (Application.isBatchMode)
                        throw;
                    EditorUtility.DisplayDialog("Mera Brand - Web",
                        "Remote exhibition content could not be built. Check the Console.", "OK");
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
                    try
                    {
                        CopyRemoteContent();
                        WriteHostingHeaders();
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogException(exception);
                        if (Application.isBatchMode)
                            throw;
                        EditorUtility.DisplayDialog("Mera Brand - Web",
                            "The Web player built, but its remote exhibition files were not copied. " +
                            "Do not deploy this build. Check the Console.", "OK");
                        return;
                    }

                    string successMessage = $"Web build completed successfully.\n\nOutput: {BuildFolder}\n" +
                        $"Initial player size: {summary.totalSize / (1024f * 1024f):0.0} MB\n" +
                        "Remote exhibition: RemoteContent/WebGL\n\n" +
                        "Upload the entire output folder to the website.";
                    if (Application.isBatchMode)
                        Debug.Log(successMessage);
                    else
                        EditorUtility.DisplayDialog("Mera Brand - Web", successMessage, "OK");
                }
                else
                {
                    string failureMessage = $"Web build failed with result: {summary.result}. Check the Unity Console.";
                    if (Application.isBatchMode)
                        throw new System.InvalidOperationException(failureMessage);
                    EditorUtility.DisplayDialog("Mera Brand - Web", failureMessage, "OK");
                }
            }
            finally
            {
                EditorBuildSettings.scenes = originalBuildScenes;
            }
        }

        private static void CopyRemoteContent()
        {
            string source = Path.GetFullPath(Path.Combine("ServerData", "WebGL"));
            string buildRoot = Path.GetFullPath(BuildFolder);
            string destination = Path.GetFullPath(Path.Combine(buildRoot, "RemoteContent", "WebGL"));
            if (!destination.StartsWith(buildRoot + Path.DirectorySeparatorChar,
                    System.StringComparison.OrdinalIgnoreCase))
                throw new System.InvalidOperationException("Remote content output is outside the Web build folder.");
            if (!Directory.Exists(source) ||
                Directory.GetFiles(source, "*.bundle", SearchOption.AllDirectories).Length == 0)
                throw new System.InvalidOperationException("No remote exhibition bundles were built.");

            if (Directory.Exists(destination))
                Directory.Delete(destination, true);
            CopyDirectory(source, destination);
        }

        private static void WriteHostingHeaders()
        {
            string buildRoot = Path.GetFullPath(BuildFolder);
            string buildAssets = Path.GetFullPath(Path.Combine(buildRoot, "Build"));
            if (!buildAssets.StartsWith(buildRoot + Path.DirectorySeparatorChar,
                    System.StringComparison.OrdinalIgnoreCase))
                throw new System.InvalidOperationException("Web assets are outside the build folder.");
            Directory.CreateDirectory(buildAssets);
            File.WriteAllText(Path.Combine(buildAssets, ".htaccess"),
                "<IfModule mod_mime.c>\n" +
                "  AddEncoding gzip .unityweb\n" +
                "  AddType application/wasm .wasm.unityweb\n" +
                "  AddType application/javascript .js.unityweb\n" +
                "</IfModule>\n");
        }

        private static void ClearPreviousRemoteContent()
        {
            string projectRoot = Path.GetFullPath(".");
            string source = Path.GetFullPath(Path.Combine("ServerData", "WebGL"));
            if (!source.StartsWith(projectRoot + Path.DirectorySeparatorChar,
                    System.StringComparison.OrdinalIgnoreCase))
                throw new System.InvalidOperationException("Remote build path is outside the Unity project.");

            if (Directory.Exists(source))
                Directory.Delete(source, true);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            foreach (string child in Directory.GetDirectories(source))
                CopyDirectory(child, Path.Combine(destination, Path.GetFileName(child)));
        }
    }
}
#endif

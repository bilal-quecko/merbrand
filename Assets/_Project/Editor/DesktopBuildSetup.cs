#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MeraBrand.Expo.Editor
{
    public static class DesktopBuildSetup
    {
        private const string RequestFile = "Library/MeraBrandDesktopBuild.request";

        [InitializeOnLoadMethod]
        private static void ProcessRequestedBuild()
        {
            if (!File.Exists(RequestFile)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                    return;
                File.Delete(RequestFile);
                ConfigureWindows();
            };
        }

        [MenuItem("Mera Brand/Desktop/Configure Windows Desktop (High)")]
        public static void ConfigureWindows()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Windows build support is not installed.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64 &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Could not switch to Windows desktop.");

            int high = Array.IndexOf(QualitySettings.names, "High");
            if (high < 0) throw new InvalidOperationException("High quality preset is missing.");
            QualitySettings.SetQualityLevel(high, true);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Project/Scenes/00_Boot.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/01_MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/02_Exhibition.unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Mera Brand: Windows desktop configured with High quality and the full exhibition scene.");
        }

        [MenuItem("Mera Brand/Desktop/Build Windows Desktop")]
        public static void BuildWindows()
        {
            ConfigureWindows();
            const string output = "Builds/Windows/MeraBrandPakistan.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path),
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("Mera Brand: Windows build succeeded: " + Path.GetFullPath(output));
        }
    }
}
#endif

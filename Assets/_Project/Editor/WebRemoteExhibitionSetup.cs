#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using MeraBrand.Expo.Core;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MeraBrand.Expo.Stalls;

namespace MeraBrand.Expo.Editor
{
    public static class WebRemoteExhibitionSetup
    {
        private const string SourceScene = "Assets/_Project/Scenes/02_Exhibition.unity";
        private const string RemoteScene = "Assets/_Project/RemoteExhibition/02_Exhibition.unity";
        private const string LiteScene = "Assets/_Project/RemoteExhibition/02_Exhibition_Lite.unity";
        private const string GroupName = "Web Remote Exhibition";
        private const string LiteGroupName = "Web Lite Exhibition";
        private const string RemoteLoadPath = "{MeraBrand.Expo.Core.RemoteExhibitionContentUrl.BaseUrl}";

        [MenuItem("Mera Brand/Web/Configure Remote Exhibition")]
        public static void ConfigureFromMenu()
        {
            Configure();
            EditorUtility.DisplayDialog("Remote Exhibition",
                "The WebGL exhibition scene is configured as remote content. " +
                "Use Mera Brand > Web > Build Production Web to build both the player and remote content.", "OK");
        }

        public static void Configure()
        {
            if (!File.Exists(SourceScene))
                throw new FileNotFoundException("Exhibition scene is missing.", SourceScene);

            SynchronizeSceneCopy();
            GenerateLiteScene();

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
                throw new InvalidOperationException("Could not create Addressables settings.");

            settings.profileSettings.SetValue(settings.activeProfileId,
                AddressableAssetSettings.kRemoteBuildPath,
                AddressableAssetSettings.kRemoteBuildPathValue);
            settings.profileSettings.SetValue(settings.activeProfileId,
                AddressableAssetSettings.kRemoteLoadPath, RemoteLoadPath);

            // A local catalog avoids a network request before the menu appears.
            settings.BuildRemoteCatalog = false;
            settings.BuildAddressablesWithPlayerBuild =
                AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;

            ConfigureSceneGroup(settings, GroupName, RemoteScene, SceneNames.Exhibition);
            ConfigureSceneGroup(settings, LiteGroupName, LiteScene, SceneNames.ExhibitionLite);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureSceneGroup(AddressableAssetSettings settings, string name,
            string scenePath, string address)
        {
            AddressableAssetGroup group = settings.FindGroup(name) ??
                settings.CreateGroup(name, false, false, true, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            BundledAssetGroupSchema bundleSchema = group.GetSchema<BundledAssetGroupSchema>() ??
                group.AddSchema<BundledAssetGroupSchema>();
            bundleSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            bundleSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            bundleSchema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;

            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            if (string.IsNullOrEmpty(sceneGuid))
                throw new InvalidOperationException($"Unity did not import scene: {scenePath}");
            settings.CreateOrMoveEntry(sceneGuid, group).address = address;
            EditorUtility.SetDirty(bundleSchema);
            EditorUtility.SetDirty(group);
        }

        private static void GenerateLiteScene()
        {
            // Keep the original exhibition geometry and lighting. Only remove
            // environment and crowd objects explicitly excluded from mobile.
            Scene scene = EditorSceneManager.OpenScene(RemoteScene, OpenSceneMode.Additive);
            int removed = 0;
            try
            {
                int stallCount = scene.GetRootGameObjects()
                    .Sum(root => root.GetComponentsInChildren<StallIdentity>(true).Length);
                if (stallCount < 100)
                    throw new InvalidOperationException($"Source exhibition has only {stallCount} stalls.");

                GameObject models = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == "Models")
                        models = root;
                    else if (root.name == "Terrain" || root.name == "Girl_11")
                    {
                        UnityEngine.Object.DestroyImmediate(root);
                        removed++;
                    }
                }

                if (models == null)
                    throw new InvalidOperationException("Source exhibition has no Models root.");
                Transform outside = models.transform.Find("OUTSIDE");
                Transform misc = models.transform.Find("MISC");
                Transform tulip = models.transform.Find("Tulip Model");
                if (outside == null || misc == null || tulip == null)
                    throw new InvalidOperationException("The venue hierarchy changed; cannot safely trim mobile content.");

                UnityEngine.Object.DestroyImmediate(outside.gameObject);
                removed++;
                foreach (string name in new[]
                {
                    "Population System", "Audience", "Talking people", "parkedCars", "Traffic"
                })
                {
                    Transform decoration = misc.Find(name);
                    if (decoration == null)
                        throw new InvalidOperationException($"Missing expected mobile decoration: MISC/{name}");
                    UnityEngine.Object.DestroyImmediate(decoration.gameObject);
                    removed++;
                }

                int remainingStalls = scene.GetRootGameObjects()
                    .Sum(root => root.GetComponentsInChildren<StallIdentity>(true).Length);
                if (remainingStalls != stallCount || tulip == null)
                    throw new InvalidOperationException("Mobile trim changed stalls or the Tulip model.");
                if (!EditorSceneManager.SaveScene(scene, LiteScene, true))
                    throw new IOException("Could not save the lite exhibition scene.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.ImportAsset(LiteScene, ImportAssetOptions.ForceUpdate);
            Debug.Log($"Lite exhibition generated: removed {removed} terrain/crowd roots; preserved all stalls and Tulip model.");
        }

        private static void SynchronizeSceneCopy()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RemoteScene));
            byte[] source = File.ReadAllBytes(SourceScene);
            if (File.Exists(RemoteScene) && source.SequenceEqual(File.ReadAllBytes(RemoteScene)))
                return;

            File.WriteAllBytes(RemoteScene, source);
            AssetDatabase.ImportAsset(RemoteScene, ImportAssetOptions.ForceUpdate);
        }
    }
}
#endif

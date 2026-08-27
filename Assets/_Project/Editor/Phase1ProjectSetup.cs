#if UNITY_EDITOR
using System.IO;
using MeraBrand.Expo.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeraBrand.Expo.Editor
{
    public static class Phase1ProjectSetup
    {
        private const string Root = "Assets/_Project";
        private const string ScenesPath = Root + "/Scenes";
        private const string ConfigPath = Root + "/Config";
        private const string AppConfigPath = ConfigPath + "/AppConfig.asset";

        [MenuItem("Mera Brand/Phase 1/Generate Project Foundation")]
        public static void GenerateProjectFoundation()
        {
            EnsureFolders();
            AppConfig config = GetOrCreateAppConfig();

            CreateBootScene(config);
            CreateMainMenuScene();
            CreateExhibitionScene();
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Mera Brand Phase 1 foundation generated successfully.");
            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 1",
                "Foundation generated. Scenes, AppConfig and Build Settings are ready.",
                "OK");
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                Root,
                ScenesPath,
                ConfigPath,
                Root + "/Art",
                Root + "/Art/Materials",
                Root + "/Art/Models",
                Root + "/Art/Textures",
                Root + "/Audio",
                Root + "/Prefabs",
                Root + "/Prefabs/Architecture",
                Root + "/Prefabs/Stalls",
                Root + "/Prefabs/UI",
                Root + "/Scripts/Authentication",
                Root + "/Scripts/Booking",
                Root + "/Scripts/Camera",
                Root + "/Scripts/Network",
                Root + "/Scripts/Platform",
                Root + "/Scripts/Stalls",
                Root + "/Scripts/UI",
                Root + "/UI",
                Root + "/UI/Fonts",
                Root + "/UI/Sprites"
            };

            foreach (string folder in folders)
                EnsureFolder(folder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        private static AppConfig GetOrCreateAppConfig()
        {
            AppConfig config = AssetDatabase.LoadAssetAtPath<AppConfig>(AppConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<AppConfig>();
            AssetDatabase.CreateAsset(config, AppConfigPath);
            return config;
        }

        private static void CreateBootScene(AppConfig config)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = SceneNames.Boot;

            GameObject systems = new("[SYSTEMS]");

            GameObject appManagerObject = new("AppManager");
            appManagerObject.transform.SetParent(systems.transform);
            AppManager appManager = appManagerObject.AddComponent<AppManager>();
            appManager.SetConfig(config);

            GameObject sceneLoaderObject = new("SceneLoader");
            sceneLoaderObject.transform.SetParent(systems.transform);
            sceneLoaderObject.AddComponent<SceneLoader>();

            GameObject bootControllerObject = new("BootController");
            bootControllerObject.transform.SetParent(systems.transform);
            bootControllerObject.AddComponent<BootController>();

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/{SceneNames.Boot}.unity");
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = SceneNames.MainMenu;

            new GameObject("[MAIN MENU - UI PLACEHOLDER]");

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/{SceneNames.MainMenu}.unity");
        }

        private static void CreateExhibitionScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = SceneNames.Exhibition;

            CreateRoot("Architecture");
            CreateRoot("Stalls");
            CreateRoot("Navigation");
            CreateRoot("Lighting");
            CreateRoot("Cameras");
            CreateRoot("Systems");
            CreateRoot("UI");

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/{SceneNames.Exhibition}.unity");
        }

        private static void CreateRoot(string name)
        {
            new GameObject(name);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesPath}/{SceneNames.Boot}.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/{SceneNames.MainMenu}.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/{SceneNames.Exhibition}.unity", true)
            };
        }
    }
}
#endif

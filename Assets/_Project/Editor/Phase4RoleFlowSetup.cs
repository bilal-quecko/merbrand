#if UNITY_EDITOR
using System.IO;
using MeraBrand.Expo.CameraSystem;
using MeraBrand.Expo.Core;
using MeraBrand.Expo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MeraBrand.Expo.Editor
{
    public static class Phase4RoleFlowSetup
    {
        private const string MainMenuPath = "Assets/_Project/Scenes/01_MainMenu.unity";
        private const string ExhibitionPath = "Assets/_Project/Scenes/02_Exhibition.unity";

        [MenuItem("Mera Brand/Phase 4/Setup Main Menu + Role Flow")]
        public static void SetupPhase4()
        {
            if (!File.Exists(MainMenuPath) || !File.Exists(ExhibitionPath))
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 4", "Required scenes were not found. Run Phase 1 first.", "OK");
                return;
            }

            SetupMainMenu();
            SetupExhibitionRoleFlow();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 4",
                "Phase 4 setup completed.\n\nDevelopment admin credentials:\nUsername: admin\nPassword: admin123\n\nVisitor enters flythrough mode. Admin enters top-down mode and can switch between Top View and Free Fly.",
                "OK");
        }

        private static void SetupMainMenu()
        {
            Scene scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

            DestroyIfExists("[MAIN MENU - UI PLACEHOLDER]");
            DestroyIfExists("Phase4_MainMenu");
            EnsureEventSystem();

            GameObject root = new("Phase4_MainMenu");
            MainMenuController controller = root.AddComponent<MainMenuController>();

            Canvas canvas = CreateCanvas("Canvas", root.transform);
            CreateImage("Background", canvas.transform, Stretch(), new Color(0.035f, 0.05f, 0.07f, 1f));

            TextMeshProUGUI title = CreateText("Title_TMP", canvas.transform, "MERA BRAND PAKISTAN", 48f, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), new Vector2(0f, 0f), new Vector2(900f, 80f));

            TextMeshProUGUI subtitle = CreateText("Subtitle_TMP", canvas.transform, "Family Expo 2026", 27f, TextAlignmentOptions.Center);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.70f), new Vector2(0.5f, 0.70f), Vector2.zero, new Vector2(600f, 55f));

            GameObject rolePanel = CreatePanel("RolePanel", canvas.transform, new Vector2(0.5f, 0.42f), new Vector2(520f, 260f));
            Button visitorButton = CreateButton("VisitorButton", rolePanel.transform, "VISITOR", new Vector2(0f, 45f));
            Button adminButton = CreateButton("AdminButton", rolePanel.transform, "ADMIN", new Vector2(0f, -45f));
            UnityEventTools.AddPersistentListener(visitorButton.onClick, controller.ContinueAsVisitor);
            UnityEventTools.AddPersistentListener(adminButton.onClick, controller.OpenAdminLogin);

            GameObject loginPanel = CreatePanel("AdminLoginPanel", canvas.transform, new Vector2(0.5f, 0.43f), new Vector2(580f, 420f));
            CreateTextAt("LoginTitle_TMP", loginPanel.transform, "ADMIN LOGIN", 30f, new Vector2(0f, 145f), new Vector2(480f, 55f));

            TMP_InputField username = CreateInputField("UsernameInput", loginPanel.transform, "Username", new Vector2(0f, 65f));
            TMP_InputField password = CreateInputField("PasswordInput", loginPanel.transform, "Password", new Vector2(0f, 0f));
            password.contentType = TMP_InputField.ContentType.Password;

            TextMeshProUGUI error = CreateTextAt("LoginError_TMP", loginPanel.transform, string.Empty, 18f, new Vector2(0f, -58f), new Vector2(500f, 40f));
            error.color = new Color(1f, 0.45f, 0.45f);

            Button loginButton = CreateButton("LoginButton", loginPanel.transform, "LOGIN", new Vector2(-115f, -130f));
            Button cancelButton = CreateButton("CancelButton", loginPanel.transform, "CANCEL", new Vector2(115f, -130f));
            UnityEventTools.AddPersistentListener(loginButton.onClick, controller.LoginAsAdmin);
            UnityEventTools.AddPersistentListener(cancelButton.onClick, controller.CancelAdminLogin);

            SerializedObject so = new(controller);
            so.FindProperty("rolePanel").objectReferenceValue = rolePanel;
            so.FindProperty("adminLoginPanel").objectReferenceValue = loginPanel;
            so.FindProperty("usernameInput").objectReferenceValue = username;
            so.FindProperty("passwordInput").objectReferenceValue = password;
            so.FindProperty("loginErrorText").objectReferenceValue = error;
            so.ApplyModifiedPropertiesWithoutUndo();

            loginPanel.SetActive(false);

            CreateTextAt("Version_TMP", canvas.transform, "Development Build", 15f, new Vector2(0f, 22f), new Vector2(300f, 35f), new Vector2(0.5f, 0f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupExhibitionRoleFlow()
        {
            Scene scene = EditorSceneManager.OpenScene(ExhibitionPath, OpenSceneMode.Single);
            EnsureEventSystem();

            Transform camerasRoot = GetOrCreateRoot("Cameras");
            Transform navigationRoot = GetOrCreateRoot("Navigation");
            Transform systemsRoot = GetOrCreateRoot("Systems");
            Transform uiRoot = GetOrCreateRoot("UI");

            RemoveChildIfExists(camerasRoot, "AdminTopDownCamera");
            RemoveChildIfExists(navigationRoot, "Phase4_SpawnPoints");
            RemoveChildIfExists(systemsRoot, "Phase4_RoleSystems");
            RemoveChildIfExists(uiRoot, "Phase4_HUD");

            Transform visitorCameraTransform = camerasRoot.Find("VisitorFlyCamera");
            if (visitorCameraTransform == null)
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 4", "VisitorFlyCamera not found. Run Phase 3 setup first.", "OK");
                return;
            }

            GameObject adminCamera = new("AdminTopDownCamera");
            adminCamera.transform.SetParent(camerasRoot, false);
            adminCamera.transform.position = new Vector3(130f, 260f, 110f);
            adminCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Camera adminCameraComponent = adminCamera.AddComponent<Camera>();
            adminCameraComponent.orthographic = true;
            adminCameraComponent.orthographicSize = 125f;
            adminCameraComponent.nearClipPlane = 0.1f;
            adminCameraComponent.farClipPlane = 1000f;
            adminCamera.AddComponent<AdminTopDownCameraController>();
            adminCamera.SetActive(false);

            GameObject spawnRoot = new("Phase4_SpawnPoints");
            spawnRoot.transform.SetParent(navigationRoot, false);
            Transform visitorSpawn = new GameObject("VisitorSpawnPoint").transform;
            visitorSpawn.SetParent(spawnRoot.transform, false);
            visitorSpawn.position = visitorCameraTransform.position;
            visitorSpawn.rotation = visitorCameraTransform.rotation;
            Transform adminPoint = new GameObject("AdminTopDownPoint").transform;
            adminPoint.SetParent(spawnRoot.transform, false);
            adminPoint.position = adminCamera.transform.position;
            adminPoint.rotation = adminCamera.transform.rotation;

            GameObject systemObject = new("Phase4_RoleSystems");
            systemObject.transform.SetParent(systemsRoot, false);
            CameraModeManager cameraModeManager = systemObject.AddComponent<CameraModeManager>();
            cameraModeManager.Configure(visitorCameraTransform.gameObject, adminCamera);
            ExhibitionModeController modeController = systemObject.AddComponent<ExhibitionModeController>();

            GameObject hudRoot = new("Phase4_HUD");
            hudRoot.transform.SetParent(uiRoot, false);
            Canvas hudCanvas = CreateCanvas("HUDCanvas", hudRoot.transform);

            GameObject adminHud = CreatePanel("AdminHUD", hudCanvas.transform, new Vector2(0.5f, 0.94f), new Vector2(720f, 82f));
            CreateTextAt("AdminTitle_TMP", adminHud.transform, "MERA BRAND EXPO — ADMIN", 20f, new Vector2(-205f, 0f), new Vector2(300f, 50f));
            Button topButton = CreateButton("TopViewButton", adminHud.transform, "TOP VIEW", new Vector2(90f, 0f), new Vector2(130f, 45f));
            Button flyButton = CreateButton("FreeFlyButton", adminHud.transform, "FREE FLY", new Vector2(235f, 0f), new Vector2(130f, 45f));
            Button logoutButton = CreateButton("LogoutButton", adminHud.transform, "LOGOUT", new Vector2(370f, 0f), new Vector2(110f, 45f));
            UnityEventTools.AddPersistentListener(topButton.onClick, modeController.ShowTopView);
            UnityEventTools.AddPersistentListener(flyButton.onClick, modeController.ShowFreeFly);
            UnityEventTools.AddPersistentListener(logoutButton.onClick, modeController.Logout);

            GameObject visitorHud = CreatePanel("VisitorHUD", hudCanvas.transform, new Vector2(0.91f, 0.94f), new Vector2(220f, 70f));
            Button exitButton = CreateButton("ExitToMenuButton", visitorHud.transform, "EXIT TO MENU", Vector2.zero, new Vector2(180f, 42f));
            UnityEventTools.AddPersistentListener(exitButton.onClick, modeController.ExitVisitorToMenu);

            SerializedObject modeSo = new(modeController);
            modeSo.FindProperty("cameraModeManager").objectReferenceValue = cameraModeManager;
            modeSo.FindProperty("adminHud").objectReferenceValue = adminHud;
            modeSo.FindProperty("visitorHud").objectReferenceValue = visitorHud;
            modeSo.ApplyModifiedPropertiesWithoutUndo();

            adminHud.SetActive(false);
            visitorHud.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Canvas CreateCanvas(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 size)
        {
            GameObject go = CreateImage(name, parent, new RectSpec(anchor, anchor, Vector2.zero, size), new Color(0.06f, 0.08f, 0.11f, 0.94f));
            return go;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2? size = null)
        {
            Vector2 finalSize = size ?? new Vector2(320f, 68f);
            GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, finalSize);
            go.GetComponent<Image>().color = new Color(0.15f, 0.24f, 0.31f, 1f);
            TextMeshProUGUI text = CreateText("Text_TMP", go.transform, label, 21f, TextAlignmentOptions.Center);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderText, Vector2 pos)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetRect(rootRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(440f, 54f));
            root.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 1f);

            GameObject viewport = new("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(14f, 6f); vpRect.offsetMax = new Vector2(-14f, -6f);

            TextMeshProUGUI placeholder = CreateText("Placeholder", viewport.transform, placeholderText, 19f, TextAlignmentOptions.MidlineLeft);
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            placeholder.rectTransform.anchorMin = Vector2.zero; placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = Vector2.zero; placeholder.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI text = CreateText("Text", viewport.transform, string.Empty, 19f, TextAlignmentOptions.MidlineLeft);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.textViewport = vpRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private static TextMeshProUGUI CreateTextAt(string name, Transform parent, string value, float size, Vector2 position, Vector2 dimensions, Vector2? anchor = null)
        {
            TextMeshProUGUI tmp = CreateText(name, parent, value, size, TextAlignmentOptions.Center);
            Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
            SetRect(tmp.rectTransform, a, a, position, dimensions);
            return tmp;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = value;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static GameObject CreateImage(string name, Transform parent, RectSpec spec, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, spec.anchorMin, spec.anchorMax, spec.position, spec.size);
            if (spec.anchorMin == Vector2.zero && spec.anchorMax == Vector2.one)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static RectSpec Stretch() => new(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void EnsureEventSystem()
        {
            EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                StandaloneInputModule legacy = existing.GetComponent<StandaloneInputModule>();
                if (legacy != null) Object.DestroyImmediate(legacy);
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                return;
            }

            GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static Transform GetOrCreateRoot(string name)
        {
            GameObject found = GameObject.Find(name);
            return found != null ? found.transform : new GameObject(name).transform;
        }

        private static void DestroyIfExists(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null) Object.DestroyImmediate(found);
        }

        private static void RemoveChildIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        private readonly struct RectSpec
        {
            public readonly Vector2 anchorMin;
            public readonly Vector2 anchorMax;
            public readonly Vector2 position;
            public readonly Vector2 size;

            public RectSpec(Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
            {
                this.anchorMin = anchorMin;
                this.anchorMax = anchorMax;
                this.position = position;
                this.size = size;
            }
        }
    }
}
#endif

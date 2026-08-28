#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using MeraBrand.Expo.CameraSystem;
using MeraBrand.Expo.Stalls;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MeraBrand.Expo.Editor
{
    public static class Phase5StallInteractionSetup
    {
        private const string ExhibitionPath = "Assets/_Project/Scenes/02_Exhibition.unity";
        private const string MaterialPath = "Assets/_Project/Art/Materials/MAT_Stall_Selected.mat";

        [MenuItem("Mera Brand/Phase 5/Setup Clickable Stalls + Visit Stall")]
        public static void SetupPhase5()
        {
            if (!File.Exists(ExhibitionPath))
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 5", "02_Exhibition scene not found.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ExhibitionPath, OpenSceneMode.Single);

            CameraModeManager cameraModeManager = Object.FindFirstObjectByType<CameraModeManager>();
            GameObject adminCameraObject = GameObject.Find("AdminTopDownCamera");
            Camera adminCamera = adminCameraObject != null ? adminCameraObject.GetComponent<Camera>() : null;

            if (cameraModeManager == null || adminCamera == null)
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Phase 5",
                    "Phase 4 camera systems were not found. Run 'Setup Main Menu + Role Flow' first.",
                    "OK");
                return;
            }

            Transform systemsRoot = GetOrCreateRoot("Systems");
            Transform uiRoot = GetOrCreateRoot("UI");

            RemoveChildIfExists(systemsRoot, "Phase5_StallInteraction");
            RemoveChildIfExists(uiRoot, "Phase5_StallSelectionUI");

            Material highlightMaterial = GetOrCreateHighlightMaterial();
            StallIdentity[] stalls = Object.FindObjectsByType<StallIdentity>(FindObjectsSortMode.None);
            AssignMissingUniqueIds(stalls);
            SetupSelectionHighlights(stalls, highlightMaterial);

            GameObject systemObject = new("Phase5_StallInteraction");
            systemObject.transform.SetParent(systemsRoot, false);
            StallSelectionController selectionController = systemObject.AddComponent<StallSelectionController>();

            GameObject uiObject = new("Phase5_StallSelectionUI");
            uiObject.transform.SetParent(uiRoot, false);
            Canvas canvas = CreateCanvas(uiObject.transform);

            GameObject panel = CreatePanel("StallSelectionPanel", canvas.transform);
            TextMeshProUGUI title = CreateText("StallName_TMP", panel.transform, "STALL", 28f, new Vector2(0f, 142f), new Vector2(360f, 50f));
            TextMeshProUGUI id = CreateText("StallId_TMP", panel.transform, "ID:", 18f, new Vector2(0f, 88f), new Vector2(360f, 36f));
            TextMeshProUGUI hall = CreateText("Hall_TMP", panel.transform, "Hall:", 18f, new Vector2(0f, 48f), new Vector2(360f, 36f));
            TextMeshProUGUI size = CreateText("Size_TMP", panel.transform, "Size:", 18f, new Vector2(0f, 8f), new Vector2(360f, 36f));

            Button visit = CreateButton("VisitStallButton", panel.transform, "VISIT STALL", new Vector2(0f, -72f), new Vector2(300f, 52f));
            Button close = CreateButton("CloseButton", panel.transform, "CLOSE", new Vector2(0f, -134f), new Vector2(300f, 44f));
            UnityEventTools.AddPersistentListener(visit.onClick, selectionController.VisitSelectedStall);
            UnityEventTools.AddPersistentListener(close.onClick, selectionController.CloseSelection);

            selectionController.Configure(cameraModeManager, adminCamera, panel, title, id, hall, size);
            panel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = systemObject;

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 5",
                $"Phase 5 configured for {stalls.Length} stall objects.\n\n" +
                "Admin Top View:\n" +
                "• Left-click a stall to select it\n" +
                "• Selected stall is highlighted\n" +
                "• Selection panel shows ID, hall and size\n" +
                "• VISIT STALL moves the fly camera to that stall\n\n" +
                "Any stall without an ID was assigned a stable STALL-### ID. Rerun this setup after adding new stall prefabs.",
                "OK");
        }

        private static void AssignMissingUniqueIds(StallIdentity[] stalls)
        {
            HashSet<string> used = new();
            foreach (StallIdentity stall in stalls)
            {
                if (stall == null)
                    continue;
                string existing = stall.StallId;
                if (!string.IsNullOrWhiteSpace(existing) && existing != "UNASSIGNED")
                    used.Add(existing);
            }

            int counter = 1;
            foreach (StallIdentity stall in stalls)
            {
                if (stall == null || (!string.IsNullOrWhiteSpace(stall.StallId) && stall.StallId != "UNASSIGNED"))
                    continue;

                string id;
                do
                {
                    id = $"STALL-{counter:000}";
                    counter++;
                }
                while (used.Contains(id));

                stall.EditorConfigure(
                    id,
                    stall.DisplayName,
                    stall.Hall,
                    stall.Size,
                    stall.FootprintMeters,
                    stall.FootprintUnityUnits,
                    stall.WallHeightMeters);

                EditorUtility.SetDirty(stall);
                used.Add(id);
            }
        }

        private static void SetupSelectionHighlights(StallIdentity[] stalls, Material material)
        {
            foreach (StallIdentity stall in stalls)
            {
                if (stall == null)
                    continue;

                Transform old = stall.transform.Find("Phase5_SelectionHighlight");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                highlight.name = "Phase5_SelectionHighlight";
                highlight.transform.SetParent(stall.transform, false);
                highlight.transform.localPosition = new Vector3(0f, 0.13f, 0f);
                highlight.transform.localRotation = Quaternion.identity;
                highlight.transform.localScale = new Vector3(
                    stall.FootprintUnityUnits.x + 0.5f,
                    0.12f,
                    stall.FootprintUnityUnits.y + 0.5f);

                Collider collider = highlight.GetComponent<Collider>();
                if (collider != null)
                    Object.DestroyImmediate(collider);

                Renderer renderer = highlight.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = material;

                highlight.SetActive(false);
            }
        }

        private static Material GetOrCreateHighlightMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader) { color = new Color(1f, 0.72f, 0.12f, 1f) };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject go = new("Phase5Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-28f, 0f);
            rect.sizeDelta = new Vector2(420f, 390f);
            go.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.085f, 0.96f);
            return go;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float fontSize, Vector2 pos, Vector2 size)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = value;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 pos, Vector2 size)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.15f, 0.24f, 0.31f, 1f);

            TextMeshProUGUI text = CreateText("Text_TMP", go.transform, label, 19f, Vector2.zero, size);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        private static Transform GetOrCreateRoot(string name)
        {
            GameObject found = GameObject.Find(name);
            return found != null ? found.transform : new GameObject(name).transform;
        }

        private static void RemoveChildIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }
    }
}
#endif

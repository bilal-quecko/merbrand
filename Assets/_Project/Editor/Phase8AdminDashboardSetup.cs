#if UNITY_EDITOR
using System.IO;
using System.Linq;
using MeraBrand.Expo.CameraSystem;
using MeraBrand.Expo.Stalls;
using MeraBrand.Expo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MeraBrand.Expo.Editor
{
    public static class Phase8AdminDashboardSetup
    {
        private const string ExhibitionPath = "Assets/_Project/Scenes/02_Exhibition.unity";

        [MenuItem("Mera Brand/Phase 8/Setup Admin Stall Dashboard")]
        public static void SetupPhase8()
        {
            if (!File.Exists(ExhibitionPath))
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 8", "02_Exhibition scene not found.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ExhibitionPath, OpenSceneMode.Single);

            StallSelectionController selection = FindSceneComponent<StallSelectionController>(scene);
            CameraModeManager cameraMode = FindSceneComponent<CameraModeManager>(scene);
            AdminTopDownCameraController topDown = FindSceneComponent<AdminTopDownCameraController>(scene);

            if (selection == null || cameraMode == null || topDown == null)
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 8", "Phase 4/5 camera and stall interaction systems are missing.", "OK");
                return;
            }

            Transform systemsRoot = GetOrCreateRoot("Systems");
            Transform uiRoot = GetOrCreateRoot("UI");
            RemoveChildIfExists(systemsRoot, "Phase8_StallRegistry");
            RemoveChildIfExists(uiRoot, "Phase8_AdminDashboard");

            GameObject registryObject = new("Phase8_StallRegistry");
            registryObject.transform.SetParent(systemsRoot, false);
            registryObject.AddComponent<StallRegistry>();

            GameObject dashboardRoot = new("Phase8_AdminDashboard");
            dashboardRoot.transform.SetParent(uiRoot, false);
            AdminDashboardController controller = dashboardRoot.AddComponent<AdminDashboardController>();
            Canvas canvas = CreateCanvas(dashboardRoot.transform);

            GameObject panel = CreatePanel("DashboardPanel", canvas.transform);
            CreateText("Title_TMP", panel.transform, "STALL MANAGEMENT", 22f, new Vector2(0f, 205f), new Vector2(650f, 36f));

            TextMeshProUGUI total = CreateText("Total_TMP", panel.transform, "Total: 0", 17f, new Vector2(-210f, 165f), new Vector2(180f, 30f));
            TextMeshProUGUI available = CreateText("Available_TMP", panel.transform, "Available: 0", 17f, new Vector2(0f, 165f), new Vector2(180f, 30f));
            TextMeshProUGUI booked = CreateText("Booked_TMP", panel.transform, "Booked: 0", 17f, new Vector2(210f, 165f), new Vector2(180f, 30f));

            TMP_InputField search = CreateInputField("SearchInput", panel.transform, "Search ID, stall, hall, exhibitor", new Vector2(-145f, 110f), new Vector2(390f, 42f));
            Button searchButton = CreateButton("SearchButton", panel.transform, "SEARCH", new Vector2(225f, 110f), new Vector2(150f, 42f));

            TMP_Dropdown hallDropdown = CreateDropdown("HallDropdown", panel.transform, new Vector2(-170f, 55f), new Vector2(300f, 42f), new[] { "ALL HALLS" });
            TMP_Dropdown statusDropdown = CreateDropdown("StatusDropdown", panel.transform, new Vector2(170f, 55f), new Vector2(300f, 42f), new[] { "ALL STATUS", "AVAILABLE", "BOOKED" });

            TextMeshProUGUI result = CreateText("Result_TMP", panel.transform, "Search by stall ID, name, hall, or exhibitor.", 15f, new Vector2(0f, 4f), new Vector2(650f, 55f));
            result.enableWordWrapping = true;

            Button previous = CreateButton("PreviousButton", panel.transform, "< PREV", new Vector2(-115f, -52f), new Vector2(180f, 40f));
            Button next = CreateButton("NextButton", panel.transform, "NEXT >", new Vector2(115f, -52f), new Vector2(180f, 40f));
            Button refresh = CreateButton("RefreshButton", panel.transform, "REFRESH COUNTS", new Vector2(0f, -105f), new Vector2(260f, 38f));

            UnityEventTools.AddPersistentListener(searchButton.onClick, controller.Search);
            UnityEventTools.AddPersistentListener(previous.onClick, controller.PreviousResult);
            UnityEventTools.AddPersistentListener(next.onClick, controller.NextResult);
            UnityEventTools.AddPersistentListener(refresh.onClick, controller.RefreshCounters);

            SerializedObject so = new(controller);
            so.FindProperty("dashboardPanel").objectReferenceValue = panel;
            so.FindProperty("totalText").objectReferenceValue = total;
            so.FindProperty("bookedText").objectReferenceValue = booked;
            so.FindProperty("availableText").objectReferenceValue = available;
            so.FindProperty("resultText").objectReferenceValue = result;
            so.FindProperty("searchInput").objectReferenceValue = search;
            so.FindProperty("hallDropdown").objectReferenceValue = hallDropdown;
            so.FindProperty("statusDropdown").objectReferenceValue = statusDropdown;
            so.FindProperty("selectionController").objectReferenceValue = selection;
            so.FindProperty("topDownCamera").objectReferenceValue = topDown;
            so.FindProperty("cameraModeManager").objectReferenceValue = cameraMode;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 8",
                "Admin Stall Dashboard is ready.\n\n" +
                "• Live Total / Available / Booked counters\n" +
                "• Search by stall ID, display name, hall, or exhibitor\n" +
                "• Hall and booking-status filters\n" +
                "• Previous / Next result navigation\n" +
                "• Search result automatically switches to Top View, centers the camera, highlights the stall, and opens its booking panel\n" +
                "• Central StallRegistry validates IDs at runtime",
                "OK");
        }

        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(x => x != null && x.gameObject.scene == scene);
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject go = new("Phase8Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
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
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -105f);
            rect.sizeDelta = new Vector2(720f, 360f);
            go.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.085f, 0.96f);
            return go;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float fontSize, Vector2 pos, Vector2 size)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
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
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.15f, 0.24f, 0.31f, 1f);
            TextMeshProUGUI text = CreateText("Text_TMP", go.transform, label, 16f, Vector2.zero, Vector2.zero);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderValue, Vector2 pos, Vector2 size)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 1f);

            GameObject area = new("Text Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(root.transform, false);
            RectTransform areaRect = area.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero; areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(10f, 4f); areaRect.offsetMax = new Vector2(-10f, -4f);

            TextMeshProUGUI placeholder = CreateText("Placeholder", area.transform, placeholderValue, 14f, Vector2.zero, Vector2.zero);
            placeholder.color = new Color(1f, 1f, 1f, 0.4f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.rectTransform.anchorMin = Vector2.zero; placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = Vector2.zero; placeholder.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI text = CreateText("Text", area.transform, string.Empty, 14f, Vector2.zero, Vector2.zero);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.textViewport = areaRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private static TMP_Dropdown CreateDropdown(string name, Transform parent, Vector2 pos, Vector2 size, string[] options)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 1f);

            TextMeshProUGUI label = CreateText("Label", root.transform, options.Length > 0 ? options[0] : string.Empty, 14f, Vector2.zero, Vector2.zero);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(12f, 2f); label.rectTransform.offsetMax = new Vector2(-30f, -2f);

            GameObject template = new("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(root.transform, false);
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f); templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f); templateRect.anchoredPosition = new Vector2(0f, -2f); templateRect.sizeDelta = new Vector2(0f, 180f);
            template.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 1f);

            GameObject viewport = new("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one; viewportRect.offsetMin = Vector2.zero; viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f); contentRect.anchorMax = new Vector2(1f, 1f); contentRect.pivot = new Vector2(0.5f, 1f); contentRect.sizeDelta = new Vector2(0f, 35f);

            GameObject item = new("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f); itemRect.anchorMax = new Vector2(1f, 0.5f); itemRect.sizeDelta = new Vector2(0f, 35f);
            TextMeshProUGUI itemLabel = CreateText("Item Label", item.transform, "Option", 14f, Vector2.zero, Vector2.zero);
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.rectTransform.anchorMin = Vector2.zero; itemLabel.rectTransform.anchorMax = Vector2.one;
            itemLabel.rectTransform.offsetMin = new Vector2(12f, 0f); itemLabel.rectTransform.offsetMax = new Vector2(-8f, 0f);

            TMP_Dropdown dropdown = root.GetComponent<TMP_Dropdown>();
            dropdown.captionText = label;
            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;
            dropdown.ClearOptions();
            dropdown.AddOptions(options.ToList());
            template.SetActive(false);
            return dropdown;
        }

        private static Transform GetOrCreateRoot(string name)
        {
            GameObject found = GameObject.Find(name);
            return found != null ? found.transform : new GameObject(name).transform;
        }

        private static void RemoveChildIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }
    }
}
#endif

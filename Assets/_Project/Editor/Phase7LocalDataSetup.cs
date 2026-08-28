#if UNITY_EDITOR
using System.IO;
using MeraBrand.Expo.Booking;
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
    public static class Phase7LocalDataSetup
    {
        private const string ExhibitionPath = "Assets/_Project/Scenes/02_Exhibition.unity";

        [MenuItem("Mera Brand/Phase 7/Setup Local Data + Logo Management")]
        public static void SetupPhase7()
        {
            if (!File.Exists(ExhibitionPath))
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 7", "02_Exhibition scene not found.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ExhibitionPath, OpenSceneMode.Single);
            StallSelectionController selection = Object.FindFirstObjectByType<StallSelectionController>();
            StallBookingManager bookingManager = Object.FindFirstObjectByType<StallBookingManager>();
            if (selection == null || bookingManager == null)
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 7", "Phase 6 booking system not found. Run Phase 6 setup first.", "OK");
                return;
            }

            Transform systemsRoot = GetOrCreateRoot("Systems");
            Transform uiRoot = GetOrCreateRoot("UI");
            RemoveChildIfExists(systemsRoot, "Phase7_LocalDataSystem");
            RemoveChildIfExists(uiRoot, "Phase7_LocalDataUI");

            GameObject systemObject = new("Phase7_LocalDataSystem");
            systemObject.transform.SetParent(systemsRoot, false);
            LocalDataManagementController controller = systemObject.AddComponent<LocalDataManagementController>();

            GameObject uiObject = new("Phase7_LocalDataUI");
            uiObject.transform.SetParent(uiRoot, false);
            Canvas canvas = CreateCanvas(uiObject.transform);

            GameObject panel = CreatePanel("LocalDataPanel", canvas.transform);
            CreateText("Title_TMP", panel.transform, "LOCAL DATA", 24f, new Vector2(0f, 220f), new Vector2(360f, 40f));
            CreateText("LogoLabel_TMP", panel.transform, "Selected Stall Logo", 17f, new Vector2(0f, 168f), new Vector2(360f, 30f));

            TMP_InputField logoPath = CreateInputField("LogoPathInput", panel.transform, "Paste logo path here (PC build)", new Vector2(0f, 125f));
            Button importLogo = CreateButton("ImportLogoButton", panel.transform, "IMPORT LOGO", new Vector2(0f, 75f));
            Button removeLogo = CreateButton("RemoveLogoButton", panel.transform, "REMOVE LOGO", new Vector2(0f, 29f));

            CreateText("BackupLabel_TMP", panel.transform, "Backup / Restore", 17f, new Vector2(0f, -25f), new Vector2(360f, 30f));
            Button export = CreateButton("ExportBackupButton", panel.transform, "EXPORT BACKUP", new Vector2(0f, -68f));
            Button import = CreateButton("ImportBackupButton", panel.transform, "IMPORT BACKUP", new Vector2(0f, -114f));
            Button openFolder = CreateButton("OpenFolderButton", panel.transform, "OPEN DATA FOLDER", new Vector2(0f, -160f));
            Button reset = CreateButton("ResetAllButton", panel.transform, "RESET ALL BOOKINGS", new Vector2(0f, -206f));

            TextMeshProUGUI status = CreateText("Status_TMP", panel.transform, string.Empty, 14f, new Vector2(0f, -274f), new Vector2(360f, 90f));
            status.enableWordWrapping = true;

            UnityEventTools.AddPersistentListener(importLogo.onClick, controller.ImportLogoForSelectedStall);
            UnityEventTools.AddPersistentListener(removeLogo.onClick, controller.RemoveLogoFromSelectedStall);
            UnityEventTools.AddPersistentListener(export.onClick, controller.ExportBackup);
            UnityEventTools.AddPersistentListener(import.onClick, controller.ImportBackup);
            UnityEventTools.AddPersistentListener(openFolder.onClick, controller.OpenLocalDataFolder);
            UnityEventTools.AddPersistentListener(reset.onClick, controller.ResetAllBookings);

            SerializedObject so = new(controller);
            so.FindProperty("selectionController").objectReferenceValue = selection;
            so.FindProperty("logoPathInput").objectReferenceValue = logoPath;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 7",
                "Local-only data management is ready.\n\n" +
                "• Logos are embedded into the local booking JSON as image data\n" +
                "• Export creates timestamped JSON backups\n" +
                "• Import reads import_bookings.json from the local data folder\n" +
                "• Reset requires two presses\n" +
                "• No Supabase, server, API key, or internet connection is required",
                "OK");
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject go = new("Phase7Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 35;
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
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(25f, 0f);
            rect.sizeDelta = new Vector2(420f, 650f);
            go.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.085f, 0.97f);
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

        private static Button CreateButton(string name, Transform parent, string label, Vector2 pos)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(310f, 38f);
            go.GetComponent<Image>().color = new Color(0.15f, 0.24f, 0.31f, 1f);
            TextMeshProUGUI text = CreateText("Text_TMP", go.transform, label, 16f, Vector2.zero, Vector2.zero);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderValue, Vector2 pos)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(350f, 42f);
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

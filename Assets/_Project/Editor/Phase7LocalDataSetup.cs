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

            Transform bookingPanel = uiRoot.Find("Phase6_BookingUI/Phase6Canvas/BookingPanel");
            if (bookingPanel == null)
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 7", "Phase 6 BookingPanel was not found. Rerun Phase 6 setup first.", "OK");
                return;
            }

            // Remove old Phase 7 controls if setup is rerun.
            RemoveChildIfExists(bookingPanel, "Phase7_LogoControls");

            GameObject systemObject = new("Phase7_LocalDataSystem");
            systemObject.transform.SetParent(systemsRoot, false);
            LocalDataManagementController controller = systemObject.AddComponent<LocalDataManagementController>();

            // -----------------------------
            // LOGO CONTROLS INSIDE BOOKING POPUP
            // -----------------------------
            RectTransform bookingRect = bookingPanel.GetComponent<RectTransform>();
            if (bookingRect != null)
                bookingRect.sizeDelta = new Vector2(560f, 570f);

            // Move Phase 6 lower controls down to make room for logo upload.
            SetChildY(bookingPanel, "BookingError_TMP", -118f);
            SetChildY(bookingPanel, "ConfirmBookingButton", -190f);
            SetChildY(bookingPanel, "CancelBookingButton", -245f);

            GameObject logoControls = new("Phase7_LogoControls", typeof(RectTransform));
            logoControls.transform.SetParent(bookingPanel, false);
            RectTransform logoControlsRect = logoControls.GetComponent<RectTransform>();
            logoControlsRect.anchorMin = logoControlsRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoControlsRect.anchoredPosition = Vector2.zero;
            logoControlsRect.sizeDelta = new Vector2(520f, 180f);

            CreateText("LogoLabel_TMP", logoControls.transform, "Exhibitor Logo", 17f, new Vector2(0f, -34f), new Vector2(440f, 28f));
            TMP_InputField logoPath = CreateInputField("LogoPathInput", logoControls.transform, "Logo path (PC build) — leave blank in Editor", new Vector2(0f, -70f));
            Button importLogo = CreateButton("ImportLogoButton", logoControls.transform, "UPLOAD LOGO", new Vector2(-105f, -116f), new Vector2(200f, 40f));
            Button removeLogo = CreateButton("RemoveLogoButton", logoControls.transform, "REMOVE LOGO", new Vector2(105f, -116f), new Vector2(200f, 40f));
            TextMeshProUGUI logoStatus = CreateText("LogoStatus_TMP", logoControls.transform, string.Empty, 13f, new Vector2(0f, -154f), new Vector2(460f, 32f));

            UnityEventTools.AddPersistentListener(importLogo.onClick, controller.ImportLogoForSelectedStall);
            UnityEventTools.AddPersistentListener(removeLogo.onClick, controller.RemoveLogoFromSelectedStall);

            // -----------------------------
            // BACKUP / RESTORE PANEL (HIDDEN BY DEFAULT)
            // -----------------------------
            GameObject uiObject = new("Phase7_LocalDataUI");
            uiObject.transform.SetParent(uiRoot, false);
            Canvas canvas = CreateCanvas(uiObject.transform);

            GameObject dataPanel = CreatePanel("LocalDataPanel", canvas.transform);
            CreateText("Title_TMP", dataPanel.transform, "LOCAL DATA", 24f, new Vector2(0f, 185f), new Vector2(360f, 40f));
            CreateText("BackupLabel_TMP", dataPanel.transform, "Backup / Restore", 17f, new Vector2(0f, 135f), new Vector2(360f, 30f));

            Button export = CreateButton("ExportBackupButton", dataPanel.transform, "EXPORT BACKUP", new Vector2(0f, 82f));
            Button import = CreateButton("ImportBackupButton", dataPanel.transform, "IMPORT BACKUP", new Vector2(0f, 32f));
            Button openFolder = CreateButton("OpenFolderButton", dataPanel.transform, "OPEN DATA FOLDER", new Vector2(0f, -18f));
            Button reset = CreateButton("ResetAllButton", dataPanel.transform, "RESET ALL BOOKINGS", new Vector2(0f, -68f));
            Button closeData = CreateButton("CloseDataButton", dataPanel.transform, "CLOSE", new Vector2(0f, -118f));
            TextMeshProUGUI dataStatus = CreateText("Status_TMP", dataPanel.transform, string.Empty, 13f, new Vector2(0f, -174f), new Vector2(360f, 70f));
            dataStatus.enableWordWrapping = true;

            UnityEventTools.AddPersistentListener(export.onClick, controller.ExportBackup);
            UnityEventTools.AddPersistentListener(import.onClick, controller.ImportBackup);
            UnityEventTools.AddPersistentListener(openFolder.onClick, controller.OpenLocalDataFolder);
            UnityEventTools.AddPersistentListener(reset.onClick, controller.ResetAllBookings);
            UnityEventTools.AddPersistentListener(closeData.onClick, controller.CloseAdminPanel);

            // Data-management button lives in the Admin HUD, so Visitors never see it.
            Transform adminHud = uiRoot.Find("Phase4_HUD/HUDCanvas/AdminHUD");
            if (adminHud != null)
            {
                RemoveChildIfExists(adminHud, "LocalDataButton");
                Button dataButton = CreateButton("LocalDataButton", adminHud, "DATA", new Vector2(490f, 0f), new Vector2(90f, 45f));
                UnityEventTools.AddPersistentListener(dataButton.onClick, controller.ToggleAdminPanel);
            }

            SerializedObject so = new(controller);
            so.FindProperty("adminPanel").objectReferenceValue = dataPanel;
            so.FindProperty("selectionController").objectReferenceValue = selection;
            so.FindProperty("logoPathInput").objectReferenceValue = logoPath;
            // Logo feedback belongs to the booking popup; backup functions log details to console.
            so.FindProperty("statusText").objectReferenceValue = logoStatus;
            so.ApplyModifiedPropertiesWithoutUndo();

            dataPanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 7",
                "Phase 7 UI updated.\n\n" +
                "• The logo panel no longer floats on screen\n" +
                "• BOOK STALL / EDIT BOOKING now contains Exhibitor Name + UPLOAD LOGO\n" +
                "• A small DATA button in the Admin HUD opens backup/restore tools\n" +
                "• Local data remains fully offline",
                "OK");
        }

        private static void SetChildY(Transform parent, string childName, float y)
        {
            Transform child = parent.Find(childName);
            if (child == null) return;
            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
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
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(430f, 470f);
            go.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.085f, 0.98f);
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

        private static Button CreateButton(string name, Transform parent, string label, Vector2 pos, Vector2? size = null)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size ?? new Vector2(310f, 40f);
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
            rect.sizeDelta = new Vector2(440f, 42f);
            root.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 1f);

            GameObject area = new("Text Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(root.transform, false);
            RectTransform areaRect = area.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero; areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(10f, 4f); areaRect.offsetMax = new Vector2(-10f, -4f);

            TextMeshProUGUI placeholder = CreateText("Placeholder", area.transform, placeholderValue, 13f, Vector2.zero, Vector2.zero);
            placeholder.color = new Color(1f, 1f, 1f, 0.4f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.rectTransform.anchorMin = Vector2.zero; placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = Vector2.zero; placeholder.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI text = CreateText("Text", area.transform, string.Empty, 13f, Vector2.zero, Vector2.zero);
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

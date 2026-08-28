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
    public static class Phase6BookingSetup
    {
        private const string ExhibitionPath = "Assets/_Project/Scenes/02_Exhibition.unity";
        private const string MaterialPath = "Assets/_Project/Art/Materials/MAT_Stall_Booked.mat";

        [MenuItem("Mera Brand/Phase 6/Setup Stall Booking System")]
        public static void SetupPhase6()
        {
            if (!File.Exists(ExhibitionPath))
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 6", "02_Exhibition scene not found.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ExhibitionPath, OpenSceneMode.Single);
            StallSelectionController selection = Object.FindFirstObjectByType<StallSelectionController>();
            if (selection == null)
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 6", "Phase 5 stall interaction system not found. Run Phase 5 setup first.", "OK");
                return;
            }

            Transform systemsRoot = GetOrCreateRoot("Systems");
            Transform uiRoot = GetOrCreateRoot("UI");
            RemoveChildIfExists(systemsRoot, "Phase6_BookingSystem");
            RemoveChildIfExists(uiRoot, "Phase6_BookingUI");
            RemoveChildIfExists(uiRoot, "Phase5_StallSelectionUI");

            GameObject bookingSystemObject = new("Phase6_BookingSystem");
            bookingSystemObject.transform.SetParent(systemsRoot, false);
            StallBookingManager bookingManager = bookingSystemObject.AddComponent<StallBookingManager>();

            Material bookedMaterial = GetOrCreateMaterial();
            StallIdentity[] stalls = Object.FindObjectsByType<StallIdentity>(FindObjectsSortMode.None);
            foreach (StallIdentity stall in stalls)
            {
                if (stall == null) continue;
                StallBookingVisual visual = stall.GetComponent<StallBookingVisual>();
                if (visual == null) visual = stall.gameObject.AddComponent<StallBookingVisual>();
                visual.Configure(bookedMaterial);
                EditorUtility.SetDirty(stall.gameObject);
            }

            GameObject uiRootObject = new("Phase6_BookingUI");
            uiRootObject.transform.SetParent(uiRoot, false);
            Canvas canvas = CreateCanvas(uiRootObject.transform);

            GameObject selectionPanel = CreatePanel("StallSelectionPanel", canvas.transform, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(440f, 540f), new Vector2(1f, 0.5f));
            TextMeshProUGUI title = CreateText("StallName_TMP", selectionPanel.transform, "STALL", 28f, new Vector2(0f, 210f), new Vector2(380f, 45f));
            TextMeshProUGUI id = CreateText("StallId_TMP", selectionPanel.transform, "ID:", 18f, new Vector2(0f, 165f), new Vector2(380f, 32f));
            TextMeshProUGUI hall = CreateText("Hall_TMP", selectionPanel.transform, "Hall:", 18f, new Vector2(0f, 130f), new Vector2(380f, 32f));
            TextMeshProUGUI size = CreateText("Size_TMP", selectionPanel.transform, "Size:", 18f, new Vector2(0f, 95f), new Vector2(380f, 32f));
            TextMeshProUGUI status = CreateText("Status_TMP", selectionPanel.transform, "Status: AVAILABLE", 18f, new Vector2(0f, 55f), new Vector2(380f, 32f));
            TextMeshProUGUI exhibitor = CreateText("Exhibitor_TMP", selectionPanel.transform, "Exhibitor: —", 18f, new Vector2(0f, 15f), new Vector2(380f, 42f));

            Button visit = CreateButton("VisitButton", selectionPanel.transform, "VISIT STALL", new Vector2(0f, -45f), new Vector2(300f, 44f));
            Button book = CreateButton("BookButton", selectionPanel.transform, "BOOK STALL", new Vector2(0f, -98f), new Vector2(300f, 44f));
            Button edit = CreateButton("EditButton", selectionPanel.transform, "EDIT BOOKING", new Vector2(0f, -98f), new Vector2(300f, 44f));
            Button available = CreateButton("AvailableButton", selectionPanel.transform, "MAKE AVAILABLE", new Vector2(0f, -151f), new Vector2(300f, 44f));
            Button close = CreateButton("CloseButton", selectionPanel.transform, "CLOSE", new Vector2(0f, -208f), new Vector2(300f, 40f));

            GameObject bookingPanel = CreatePanel("BookingPanel", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 390f), new Vector2(0.5f, 0.5f));
            CreateText("BookingTitle_TMP", bookingPanel.transform, "BOOK STALL", 28f, new Vector2(0f, 145f), new Vector2(480f, 45f));
            TextMeshProUGUI bookingStall = CreateText("BookingStall_TMP", bookingPanel.transform, "STALL", 18f, new Vector2(0f, 95f), new Vector2(480f, 55f));
            TMP_InputField exhibitorInput = CreateInputField("ExhibitorInput", bookingPanel.transform, "Exhibitor / Brand Name", new Vector2(0f, 25f));
            TextMeshProUGUI bookingError = CreateText("BookingError_TMP", bookingPanel.transform, string.Empty, 16f, new Vector2(0f, -30f), new Vector2(480f, 35f));
            bookingError.color = new Color(1f, 0.45f, 0.45f);
            Button confirmBooking = CreateButton("ConfirmBookingButton", bookingPanel.transform, "CONFIRM BOOKING", new Vector2(0f, -90f), new Vector2(320f, 46f));
            Button cancelBooking = CreateButton("CancelBookingButton", bookingPanel.transform, "CANCEL", new Vector2(0f, -145f), new Vector2(320f, 40f));

            GameObject confirmPanel = CreatePanel("MakeAvailableConfirmPanel", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 290f), new Vector2(0.5f, 0.5f));
            CreateText("ConfirmTitle_TMP", confirmPanel.transform, "MAKE STALL AVAILABLE?", 26f, new Vector2(0f, 85f), new Vector2(450f, 45f));
            CreateText("ConfirmBody_TMP", confirmPanel.transform, "This will remove the current booking and exhibitor information.", 18f, new Vector2(0f, 25f), new Vector2(430f, 65f));
            Button confirmAvailable = CreateButton("ConfirmAvailableButton", confirmPanel.transform, "CONFIRM", new Vector2(-105f, -75f), new Vector2(180f, 44f));
            Button cancelAvailable = CreateButton("CancelAvailableButton", confirmPanel.transform, "CANCEL", new Vector2(105f, -75f), new Vector2(180f, 44f));

            UnityEventTools.AddPersistentListener(visit.onClick, selection.VisitSelectedStall);
            UnityEventTools.AddPersistentListener(book.onClick, selection.OpenBookPopup);
            UnityEventTools.AddPersistentListener(edit.onClick, selection.OpenEditPopup);
            UnityEventTools.AddPersistentListener(available.onClick, selection.RequestMakeAvailable);
            UnityEventTools.AddPersistentListener(close.onClick, selection.CloseSelection);
            UnityEventTools.AddPersistentListener(confirmBooking.onClick, selection.ConfirmBooking);
            UnityEventTools.AddPersistentListener(cancelBooking.onClick, selection.CloseBookingPopup);
            UnityEventTools.AddPersistentListener(confirmAvailable.onClick, selection.ConfirmMakeAvailable);
            UnityEventTools.AddPersistentListener(cancelAvailable.onClick, selection.CancelMakeAvailable);

            selection.ConfigureBooking(bookingManager, status, exhibitor, book, edit, available, bookingPanel, confirmPanel, exhibitorInput, bookingStall, bookingError);

            SerializedObject so = new(selection);
            so.FindProperty("selectionPanel").objectReferenceValue = selectionPanel;
            so.FindProperty("stallNameText").objectReferenceValue = title;
            so.FindProperty("stallIdText").objectReferenceValue = id;
            so.FindProperty("hallText").objectReferenceValue = hall;
            so.FindProperty("sizeText").objectReferenceValue = size;
            so.ApplyModifiedPropertiesWithoutUndo();

            selectionPanel.SetActive(false);
            bookingPanel.SetActive(false);
            confirmPanel.SetActive(false);
            edit.gameObject.SetActive(false);
            available.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Mera Brand - Phase 6",
                $"Phase 6 booking system configured for {stalls.Length} stalls.\n\nBookings persist locally between sessions.\nDesktop/Editor: stall_bookings.json\nWebGL: browser PlayerPrefs storage\n\nLogo file upload is intentionally deferred to the cloud/storage phase.", "OK");
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject go = new("Phase6Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = pos; rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.085f, 0.97f);
            return go;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float fontSize, Vector2 pos, Vector2 size)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = pos; rect.sizeDelta = size;
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = value; tmp.fontSize = fontSize; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 pos, Vector2 size)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = pos; rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.15f, 0.24f, 0.31f, 1f);
            TextMeshProUGUI text = CreateText("Text_TMP", go.transform, label, 18f, Vector2.zero, size);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderValue, Vector2 pos)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(440f, 54f);
            root.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 1f);

            GameObject area = new("Text Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(root.transform, false);
            RectTransform areaRect = area.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero; areaRect.anchorMax = Vector2.one; areaRect.offsetMin = new Vector2(12f, 5f); areaRect.offsetMax = new Vector2(-12f, -5f);
            TextMeshProUGUI placeholder = CreateText("Placeholder", area.transform, placeholderValue, 18f, Vector2.zero, Vector2.zero);
            placeholder.color = new Color(1f, 1f, 1f, 0.45f); placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.rectTransform.anchorMin = Vector2.zero; placeholder.rectTransform.anchorMax = Vector2.one; placeholder.rectTransform.offsetMin = Vector2.zero; placeholder.rectTransform.offsetMax = Vector2.zero;
            TextMeshProUGUI text = CreateText("Text", area.transform, string.Empty, 18f, Vector2.zero, Vector2.zero);
            text.alignment = TextAlignmentOptions.MidlineLeft; text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;
            TMP_InputField input = root.GetComponent<TMP_InputField>(); input.textViewport = areaRect; input.textComponent = text; input.placeholder = placeholder; input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private static Material GetOrCreateMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) shader = Shader.Find("Standard");
            Material material = new(shader) { color = new Color(0.16f, 0.72f, 0.38f, 1f) };
            AssetDatabase.CreateAsset(material, MaterialPath); return material;
        }

        private static Transform GetOrCreateRoot(string name)
        {
            GameObject found = GameObject.Find(name); return found != null ? found.transform : new GameObject(name).transform;
        }

        private static void RemoveChildIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName); if (child != null) Object.DestroyImmediate(child.gameObject);
        }
    }
}
#endif

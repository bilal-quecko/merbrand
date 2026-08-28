#if UNITY_EDITOR
using System.IO;
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
    public static class Phase4PauseMenuSetup
    {
        private const string ExhibitionPath = "Assets/_Project/Scenes/02_Exhibition.unity";

        [MenuItem("Mera Brand/Phase 4/Setup Pause Menu")]
        public static void SetupPauseMenu()
        {
            if (!File.Exists(ExhibitionPath))
            {
                EditorUtility.DisplayDialog("Mera Brand - Pause Menu", "02_Exhibition scene not found.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ExhibitionPath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find("PauseMenuSystem");
            if (existing != null)
                Object.DestroyImmediate(existing);

            GameObject system = new("PauseMenuSystem");
            PauseMenuController controller = system.AddComponent<PauseMenuController>();

            Canvas canvas = new GameObject("PauseCanvas").AddComponent<Canvas>();
            canvas.transform.SetParent(system.transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            GameObject pausePanel = CreateImage("PausePanel", canvas.transform, new Color(0f, 0f, 0f, 0.72f));
            RectTransform panelRect = pausePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            GameObject card = CreateImage("MenuCard", pausePanel.transform, new Color(0.05f, 0.07f, 0.09f, 0.98f));
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(520f, 390f);
            cardRect.anchoredPosition = Vector2.zero;

            CreateText("Title_TMP", card.transform, "PAUSED", 42f, new Vector2(0f, 115f), new Vector2(420f, 70f));
            CreateText("Hint_TMP", card.transform, "Press ESC to resume", 20f, new Vector2(0f, 65f), new Vector2(420f, 45f));

            Button resume = CreateButton("ResumeButton", card.transform, "RESUME", new Vector2(0f, -5f));
            Button exit = CreateButton("ExitButton", card.transform, "EXIT TO MAIN MENU", new Vector2(0f, -90f));

            UnityEventTools.AddPersistentListener(resume.onClick, controller.Resume);
            UnityEventTools.AddPersistentListener(exit.onClick, controller.ExitToMainMenu);

            SerializedObject serialized = new(controller);
            serialized.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            pausePanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = system;

            EditorUtility.DisplayDialog(
                "Mera Brand - Pause Menu",
                "Pause menu added.\n\nESC pauses the simulation, shows the menu, and releases the mouse.\nESC or Resume continues the simulation.\nExit To Main Menu returns safely with time scale restored.",
                "OK");
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static void CreateText(string name, Transform parent, string text, float size, Vector2 pos, Vector2 rectSize)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = rectSize;
            rect.anchoredPosition = pos;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 pos)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(340f, 64f);
            rect.anchoredPosition = pos;
            go.GetComponent<Image>().color = new Color(0.13f, 0.18f, 0.23f, 1f);

            Button button = go.GetComponent<Button>();

            GameObject text = new("Text_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(go.transform, false);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return button;
        }
    }
}
#endif

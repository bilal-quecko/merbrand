using MeraBrand.Expo.Authentication;
using MeraBrand.Expo.CameraSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace MeraBrand.Expo.Core
{
    public sealed class ExhibitionModeController : MonoBehaviour
    {
        [SerializeField] private CameraModeManager cameraModeManager;
        [SerializeField] private GameObject adminHud;
        [SerializeField] private GameObject visitorHud;

        private void Start()
        {
            SessionManager session = SessionManager.Instance;
            if (session == null || session.CurrentRole == UserRole.None)
            {
                ReturnToMenu();
                return;
            }

            if (session.IsAdmin)
            {
                if (adminHud != null) adminHud.SetActive(true);
                if (visitorHud != null) visitorHud.SetActive(false);
                cameraModeManager?.ShowTopDown();
            }
            else
            {
                if (adminHud != null) adminHud.SetActive(false);
                if (visitorHud != null) visitorHud.SetActive(true);
                EnsureVisitorNavigationControls();
                MobileFlyControls.Ensure(cameraModeManager);
                cameraModeManager?.ShowFlythrough();
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            SessionManager session = SessionManager.Instance;
            Keyboard keyboard = Keyboard.current;
            if (session == null || session.CurrentRole == UserRole.None || keyboard == null || !keyboard.tabKey.wasPressedThisFrame)
                return;

            if (cameraModeManager != null && cameraModeManager.CurrentMode == CameraMode.TopDown)
                cameraModeManager.ShowFlythrough();
            else
                cameraModeManager?.ShowTopDown();
        }

        public void ShowTopView()
        {
            if (HasActiveRole())
                cameraModeManager?.ShowTopDown();
        }

        public void ShowFreeFly()
        {
            if (HasActiveRole())
                cameraModeManager?.ShowFlythrough();
        }

        private bool HasActiveRole()
        {
            return SessionManager.Instance != null && SessionManager.Instance.CurrentRole != UserRole.None;
        }

        private void EnsureVisitorNavigationControls()
        {
            if (visitorHud == null || visitorHud.transform.Find("VisitorTopViewButton") != null)
                return;

            RectTransform panelRect = visitorHud.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.sizeDelta = new Vector2(Mathf.Max(panelRect.sizeDelta.x, 570f), Mathf.Max(panelRect.sizeDelta.y, 70f));
                panelRect.anchoredPosition += new Vector2(-175f, 0f);
            }

            Transform exitTransform = visitorHud.transform.Find("ExitToMenuButton");
            RectTransform exitRect = exitTransform != null ? exitTransform.GetComponent<RectTransform>() : null;
            if (exitRect != null)
            {
                exitRect.anchoredPosition = new Vector2(185f, 0f);
                exitRect.sizeDelta = new Vector2(155f, 42f);
            }

            CreateVisitorButton("VisitorTopViewButton", "TOP VIEW", new Vector2(-185f, 0f), ShowTopView);
            CreateVisitorButton("VisitorFreeFlyButton", "FREE FLY", Vector2.zero, ShowFreeFly);
        }

        private void CreateVisitorButton(string name, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(visitorHud.transform, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(155f, 42f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.15f, 0.24f, 0.31f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);

            GameObject textObject = new("Text_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 18f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        public void Logout()
        {
            SessionManager.Instance?.ClearSession();
            ReturnToMenu();
        }

        public void ExitVisitorToMenu()
        {
            SessionManager.Instance?.ClearSession();
            ReturnToMenu();
        }

        private static void ReturnToMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadMainMenu();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}

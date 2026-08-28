using MeraBrand.Expo.Authentication;
using MeraBrand.Expo.CameraSystem;
using UnityEngine;
using UnityEngine.InputSystem;

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
                cameraModeManager?.ShowFlythrough();
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            SessionManager session = SessionManager.Instance;
            Keyboard keyboard = Keyboard.current;
            if (session == null || !session.IsAdmin || keyboard == null || !keyboard.tabKey.wasPressedThisFrame)
                return;

            if (cameraModeManager != null && cameraModeManager.CurrentMode == CameraMode.TopDown)
                cameraModeManager.ShowFlythrough();
            else
                cameraModeManager?.ShowTopDown();
        }

        public void ShowTopView()
        {
            if (SessionManager.Instance != null && SessionManager.Instance.IsAdmin)
                cameraModeManager?.ShowTopDown();
        }

        public void ShowFreeFly()
        {
            if (SessionManager.Instance != null && SessionManager.Instance.IsAdmin)
                cameraModeManager?.ShowFlythrough();
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

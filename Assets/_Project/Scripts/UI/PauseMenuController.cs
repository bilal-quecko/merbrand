using MeraBrand.Expo.CameraSystem;
using MeraBrand.Expo.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MeraBrand.Expo.UI
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;

        private bool isPaused;
        private FlyCameraController flyCamera;
        private CameraModeManager cameraModeManager;

        private void Awake()
        {
            Time.timeScale = 1f;
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        private void Start()
        {
            flyCamera = FindFirstObjectByType<FlyCameraController>();
            cameraModeManager = FindFirstObjectByType<CameraModeManager>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (isPaused)
                    Resume();
                else
                    Pause();
            }
        }

        public void Pause()
        {
            if (isPaused)
                return;

            isPaused = true;
            Time.timeScale = 0f;

            if (pausePanel != null)
                pausePanel.SetActive(true);

            flyCamera ??= FindFirstObjectByType<FlyCameraController>();
            if (flyCamera != null)
                flyCamera.SetCursorLocked(false);
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void Resume()
        {
            if (!isPaused)
                return;

            isPaused = false;
            Time.timeScale = 1f;

            if (pausePanel != null)
                pausePanel.SetActive(false);

            cameraModeManager ??= FindFirstObjectByType<CameraModeManager>();
            if (cameraModeManager != null)
            {
                cameraModeManager.RefreshCursorState();
                return;
            }

            flyCamera ??= FindFirstObjectByType<FlyCameraController>();
            if (flyCamera != null && flyCamera.gameObject.activeInHierarchy && !UIInteractionState.IsBlocked)
                flyCamera.SetCursorLocked(true);
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void ExitToMainMenu()
        {
            isPaused = false;
            Time.timeScale = 1f;
            UIInteractionState.ClearAll();

            if (pausePanel != null)
                pausePanel.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadMainMenu();
                return;
            }

            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}

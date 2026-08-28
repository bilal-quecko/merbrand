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

        private void Awake()
        {
            Time.timeScale = 1f;
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        private void Start()
        {
            flyCamera = FindFirstObjectByType<FlyCameraController>();
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

            flyCamera ??= FindFirstObjectByType<FlyCameraController>();
            if (flyCamera != null && flyCamera.gameObject.activeInHierarchy)
                flyCamera.SetCursorLocked(true);
        }

        public void ExitToMainMenu()
        {
            // Always restore global runtime state BEFORE any scene transition.
            isPaused = false;
            Time.timeScale = 1f;

            if (pausePanel != null)
                pausePanel.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Use the persistent loader when available. Fall back to a direct load so this
            // action still works when the exhibition scene is tested directly in the Editor.
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadMainMenu();
                return;
            }

            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private void OnDisable()
        {
            // A disabled pause controller must never leave the application frozen.
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}

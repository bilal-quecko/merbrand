using UnityEngine;

namespace MeraBrand.Expo.CameraSystem
{
    public enum CameraMode
    {
        Flythrough = 0,
        TopDown = 1,
        StallFocus = 2
    }

    public sealed class CameraModeManager : MonoBehaviour
    {
        [SerializeField] private GameObject flythroughCamera;
        [SerializeField] private GameObject topDownCamera;

        public CameraMode CurrentMode { get; private set; }

        public void Configure(GameObject flyCamera, GameObject adminCamera)
        {
            flythroughCamera = flyCamera;
            topDownCamera = adminCamera;
        }

        public void ShowFlythrough()
        {
            CurrentMode = CameraMode.Flythrough;
            SetCameraStates(true, false);
        }

        public void ShowTopDown()
        {
            CurrentMode = CameraMode.TopDown;
            SetCameraStates(false, true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SetCameraStates(bool flyActive, bool topDownActive)
        {
            if (flythroughCamera != null) flythroughCamera.SetActive(flyActive);
            if (topDownCamera != null) topDownCamera.SetActive(topDownActive);
        }
    }
}

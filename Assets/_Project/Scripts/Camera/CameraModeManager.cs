using MeraBrand.Expo.UI;
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

        private bool lastUiBlocked;

        public void Configure(GameObject flyCamera, GameObject adminCamera)
        {
            flythroughCamera = flyCamera;
            topDownCamera = adminCamera;
        }

        private void Update()
        {
            bool blocked = UIInteractionState.IsBlocked;
            if (blocked == lastUiBlocked)
                return;

            lastUiBlocked = blocked;
            ApplyCursorForCurrentState();
        }

        public void ShowFlythrough()
        {
            CurrentMode = CameraMode.Flythrough;
            SetCameraStates(true, false);
            ApplyCursorForCurrentState();
        }

        public void ShowTopDown()
        {
            CurrentMode = CameraMode.TopDown;
            SetCameraStates(false, true);
            ApplyCursorForCurrentState();
        }

        public void FocusStall(Vector3 cameraPosition, Vector3 lookTarget)
        {
            CurrentMode = CameraMode.StallFocus;
            SetCameraStates(true, false);

            FlyCameraController fly = GetFlyController();
            if (fly != null)
                fly.SnapToLookAt(cameraPosition, lookTarget);

            ApplyCursorForCurrentState();
        }

        public void RefreshCursorState()
        {
            ApplyCursorForCurrentState();
        }

        private void ApplyCursorForCurrentState()
        {
            if (UIInteractionState.IsBlocked || CurrentMode == CameraMode.TopDown)
            {
                FlyCameraController fly = GetFlyController();
                if (fly != null)
                    fly.SetCursorLocked(false);
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            FlyCameraController activeFly = GetFlyController();
            if (activeFly != null && flythroughCamera != null && flythroughCamera.activeInHierarchy)
                activeFly.SetCursorLocked(true);
        }

        private FlyCameraController GetFlyController()
        {
            return flythroughCamera != null
                ? flythroughCamera.GetComponent<FlyCameraController>()
                : null;
        }

        private void SetCameraStates(bool flyActive, bool topDownActive)
        {
            if (flythroughCamera != null) flythroughCamera.SetActive(flyActive);
            if (topDownCamera != null) topDownCamera.SetActive(topDownActive);
        }
    }
}

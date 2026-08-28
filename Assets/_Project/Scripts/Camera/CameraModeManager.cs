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

            FlyCameraController fly = GetFlyController();
            if (fly != null)
                fly.SetCursorLocked(true);
        }

        public void ShowTopDown()
        {
            CurrentMode = CameraMode.TopDown;
            SetCameraStates(false, true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void FocusStall(Vector3 cameraPosition, Vector3 lookTarget)
        {
            CurrentMode = CameraMode.StallFocus;
            SetCameraStates(true, false);

            FlyCameraController fly = GetFlyController();
            if (fly != null)
            {
                fly.SnapToLookAt(cameraPosition, lookTarget);
                fly.SetCursorLocked(true);
            }
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

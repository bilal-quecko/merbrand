using MeraBrand.Expo.Stalls;
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
        [SerializeField] private string modelLayerName = "Models";

        public CameraMode CurrentMode { get; private set; }

        private bool lastUiBlocked;
        private TopDownPresentationController topDownPresentation;

        public void Configure(GameObject flyCamera, GameObject adminCamera)
        {
            flythroughCamera = flyCamera;
            topDownCamera = adminCamera;
            ApplyCameraLayerMasks();
            EnsureTopDownPresentation();
        }

        private void Awake()
        {
            ApplyCameraLayerMasks();
            EnsureTopDownPresentation();
        }

        private void Start()
        {
            ApplyPresentationForCurrentMode();
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
            ApplyCameraLayerMasks();
            SetCameraStates(true, false);
            ApplyPresentationForCurrentMode();
            ApplyCursorForCurrentState();
        }

        public void ShowTopDown()
        {
            CurrentMode = CameraMode.TopDown;
            ApplyCameraLayerMasks();
            SetCameraStates(false, true);
            ApplyPresentationForCurrentMode();
            ApplyCursorForCurrentState();
        }

        public void FocusStall(Vector3 cameraPosition, Vector3 lookTarget)
        {
            CurrentMode = CameraMode.StallFocus;
            ApplyCameraLayerMasks();
            SetCameraStates(true, false);

            FlyCameraController fly = GetFlyController();
            if (fly != null)
                fly.SnapToLookAt(cameraPosition, lookTarget);

            ApplyPresentationForCurrentMode();
            ApplyCursorForCurrentState();
        }

        public void RefreshCursorState()
        {
            ApplyCursorForCurrentState();
        }

        public void RefreshCameraLayerMasks()
        {
            ApplyCameraLayerMasks();
        }

        private void ApplyPresentationForCurrentMode()
        {
            EnsureTopDownPresentation();
            bool isTopDown = CurrentMode == CameraMode.TopDown;

            StallTopDownLabel.SetAllVisible(isTopDown);

            if (topDownPresentation != null)
            {
                if (isTopDown)
                    topDownPresentation.EnterTopDown();
                else
                    topDownPresentation.ExitTopDown();
            }
        }

        private void EnsureTopDownPresentation()
        {
            if (topDownCamera == null)
            {
                topDownPresentation = null;
                return;
            }

            topDownPresentation = topDownCamera.GetComponent<TopDownPresentationController>();
            if (topDownPresentation == null)
                topDownPresentation = topDownCamera.AddComponent<TopDownPresentationController>();
        }

        private void ApplyCameraLayerMasks()
        {
            int modelsLayer = LayerMask.NameToLayer(modelLayerName);
            if (modelsLayer < 0)
                return;

            int modelsBit = 1 << modelsLayer;

            if (flythroughCamera != null)
            {
                Camera visitorCamera = flythroughCamera.GetComponent<Camera>();
                if (visitorCamera != null)
                    visitorCamera.cullingMask |= modelsBit;
            }

            if (topDownCamera != null)
            {
                Camera adminCamera = topDownCamera.GetComponent<Camera>();
                if (adminCamera != null)
                    adminCamera.cullingMask &= ~modelsBit;
            }
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

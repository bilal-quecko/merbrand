using System.Collections.Generic;
using MeraBrand.Expo.Stalls;
using MeraBrand.Expo.UI;
using TMPro;
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
        private const string StallNumberObjectName = "Phase6_ExhibitorNumber_TMP";

        [SerializeField] private GameObject flythroughCamera;
        [SerializeField] private GameObject topDownCamera;
        [SerializeField] private string modelTag = "Model";

        public CameraMode CurrentMode { get; private set; }

        private readonly List<GameObject> modelRoots = new();
        private bool lastUiBlocked;
        private bool warnedMissingModelTag;
        private TopDownPresentationController topDownPresentation;

        public void Configure(GameObject flyCamera, GameObject adminCamera)
        {
            flythroughCamera = flyCamera;
            topDownCamera = adminCamera;
            CacheTaggedModelRoots();
            EnsureTopDownPresentation();
        }

        private void Awake()
        {
            CacheTaggedModelRoots();
            EnsureTopDownPresentation();
            SetStallNumberVisibility(false);
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
            SetTaggedModelsActive(true);
            SetCameraStates(true, false);
            ApplyPresentationForCurrentMode();
            ApplyCursorForCurrentState();
        }

        public void ShowTopDown()
        {
            CurrentMode = CameraMode.TopDown;

            // Cache while the tagged objects are still active. Once disabled,
            // FindGameObjectsWithTag cannot discover them again.
            CacheTaggedModelRoots();
            SetTaggedModelsActive(false);
            SetCameraStates(false, true);
            ApplyPresentationForCurrentMode();
            ApplyCursorForCurrentState();
        }

        public void FocusStall(Vector3 cameraPosition, Vector3 lookTarget)
        {
            CurrentMode = CameraMode.StallFocus;
            SetTaggedModelsActive(true);
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

        // Kept for compatibility with older setup code. The project now uses the
        // Model tag rather than a camera culling layer for detailed scene models.
        public void RefreshCameraLayerMasks()
        {
            CacheTaggedModelRoots();
            SetTaggedModelsActive(CurrentMode != CameraMode.TopDown);
        }

        private void ApplyPresentationForCurrentMode()
        {
            EnsureTopDownPresentation();
            bool isTopDown = CurrentMode == CameraMode.TopDown;

            SetStallNumberVisibility(isTopDown);

            if (topDownPresentation != null)
            {
                if (isTopDown)
                    topDownPresentation.EnterTopDown();
                else
                    topDownPresentation.ExitTopDown();
            }
        }

        private void SetStallNumberVisibility(bool visible)
        {
            StallIdentity[] stalls = FindObjectsByType<StallIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (StallIdentity stall in stalls)
            {
                if (stall == null)
                    continue;

                TMP_Text[] labels = stall.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text label in labels)
                {
                    if (label != null && label.gameObject.name == StallNumberObjectName)
                        label.gameObject.SetActive(visible);
                }
            }
        }

        private void CacheTaggedModelRoots()
        {
            modelRoots.RemoveAll(item => item == null);

            if (string.IsNullOrWhiteSpace(modelTag))
                return;

            try
            {
                GameObject[] found = GameObject.FindGameObjectsWithTag(modelTag);
                foreach (GameObject item in found)
                {
                    if (item != null && !modelRoots.Contains(item))
                        modelRoots.Add(item);
                }
            }
            catch (UnityException)
            {
                if (!warnedMissingModelTag)
                {
                    warnedMissingModelTag = true;
                    Debug.LogWarning($"CameraModeManager: tag '{modelTag}' does not exist. Create it in Unity Tags and Layers and assign it to the external-model parent object.");
                }
            }
        }

        private void SetTaggedModelsActive(bool active)
        {
            if (active)
                CacheTaggedModelRoots();

            for (int i = modelRoots.Count - 1; i >= 0; i--)
            {
                GameObject item = modelRoots[i];
                if (item == null)
                {
                    modelRoots.RemoveAt(i);
                    continue;
                }

                if (item.activeSelf != active)
                    item.SetActive(active);
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

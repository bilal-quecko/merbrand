using MeraBrand.Expo.Authentication;
using MeraBrand.Expo.CameraSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MeraBrand.Expo.Stalls
{
    public sealed class StallSelectionController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private CameraModeManager cameraModeManager;
        [SerializeField] private Camera topDownCamera;

        [Header("Selection UI")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private TextMeshProUGUI stallNameText;
        [SerializeField] private TextMeshProUGUI stallIdText;
        [SerializeField] private TextMeshProUGUI hallText;
        [SerializeField] private TextMeshProUGUI sizeText;

        private StallIdentity selectedStall;
        private GameObject activeHighlight;

        public StallIdentity SelectedStall => selectedStall;

        private void Start()
        {
            if (selectionPanel != null)
                selectionPanel.SetActive(false);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            SessionManager session = SessionManager.Instance;
            if (session == null || !session.IsAdmin)
                return;

            if (cameraModeManager == null || cameraModeManager.CurrentMode != CameraMode.TopDown)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            TrySelectAt(mouse.position.ReadValue());
        }

        public void Configure(
            CameraModeManager modeManager,
            Camera adminCamera,
            GameObject panel,
            TextMeshProUGUI nameText,
            TextMeshProUGUI idText,
            TextMeshProUGUI hallLabel,
            TextMeshProUGUI sizeLabel)
        {
            cameraModeManager = modeManager;
            topDownCamera = adminCamera;
            selectionPanel = panel;
            stallNameText = nameText;
            stallIdText = idText;
            hallText = hallLabel;
            sizeText = sizeLabel;
        }

        public void CloseSelection()
        {
            ClearHighlight();
            selectedStall = null;
            if (selectionPanel != null)
                selectionPanel.SetActive(false);
        }

        public void VisitSelectedStall()
        {
            if (selectedStall == null || cameraModeManager == null)
                return;

            Vector3 cameraPosition;
            Vector3 lookTarget;

            if (selectedStall.VisitPoint != null)
                cameraPosition = selectedStall.VisitPoint.position;
            else
                cameraPosition = selectedStall.transform.position
                    - selectedStall.transform.forward * Mathf.Max(8f, selectedStall.FootprintUnityUnits.y * 0.9f)
                    + Vector3.up * 5.5f;

            if (selectedStall.LookTarget != null)
                lookTarget = selectedStall.LookTarget.position;
            else
                lookTarget = selectedStall.transform.position + Vector3.up * 4.5f;

            ClearHighlight();
            if (selectionPanel != null)
                selectionPanel.SetActive(false);

            cameraModeManager.FocusStall(cameraPosition, lookTarget);
        }

        private void TrySelectAt(Vector2 screenPosition)
        {
            if (topDownCamera == null || !topDownCamera.gameObject.activeInHierarchy)
                return;

            Ray ray = topDownCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 2000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                CloseSelection();
                return;
            }

            StallIdentity stall = hit.collider.GetComponentInParent<StallIdentity>();
            if (stall == null)
            {
                CloseSelection();
                return;
            }

            SelectStall(stall);
        }

        private void SelectStall(StallIdentity stall)
        {
            ClearHighlight();
            selectedStall = stall;

            Transform highlight = stall.transform.Find("Phase5_SelectionHighlight");
            if (highlight != null)
            {
                activeHighlight = highlight.gameObject;
                activeHighlight.SetActive(true);
            }

            if (stallNameText != null)
                stallNameText.text = string.IsNullOrWhiteSpace(stall.DisplayName) ? "STALL" : stall.DisplayName;
            if (stallIdText != null)
                stallIdText.text = $"ID: {stall.StallId}";
            if (hallText != null)
                hallText.text = $"Hall: {stall.Hall}";
            if (sizeText != null)
                sizeText.text = $"Size: {stall.FootprintMeters.x:0.#} x {stall.FootprintMeters.y:0.#} m";

            if (selectionPanel != null)
                selectionPanel.SetActive(true);
        }

        private void ClearHighlight()
        {
            if (activeHighlight != null)
                activeHighlight.SetActive(false);
            activeHighlight = null;
        }
    }
}

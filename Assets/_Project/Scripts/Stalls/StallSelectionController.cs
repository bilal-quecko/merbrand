using MeraBrand.Expo.Authentication;
using MeraBrand.Expo.Booking;
using MeraBrand.Expo.CameraSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI exhibitorText;
        [SerializeField] private Button bookButton;
        [SerializeField] private Button editButton;
        [SerializeField] private Button availableButton;

        [Header("Booking UI")]
        [SerializeField] private StallBookingManager bookingManager;
        [SerializeField] private GameObject bookingPanel;
        [SerializeField] private GameObject availableConfirmPanel;
        [SerializeField] private TMP_InputField exhibitorInput;
        [SerializeField] private TextMeshProUGUI bookingStallText;
        [SerializeField] private TextMeshProUGUI bookingErrorText;

        private StallIdentity selectedStall;
        private GameObject activeHighlight;

        public StallIdentity SelectedStall => selectedStall;

        private void Start()
        {
            if (selectionPanel != null) selectionPanel.SetActive(false);
            if (bookingPanel != null) bookingPanel.SetActive(false);
            if (availableConfirmPanel != null) availableConfirmPanel.SetActive(false);
            bookingManager ??= StallBookingManager.Instance;
            if (bookingManager != null) bookingManager.BookingChanged += OnBookingChanged;
        }

        private void OnDestroy()
        {
            if (bookingManager != null) bookingManager.BookingChanged -= OnBookingChanged;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            SessionManager session = SessionManager.Instance;
            if (session == null || !session.IsAdmin) return;
            if (cameraModeManager == null || cameraModeManager.CurrentMode != CameraMode.TopDown) return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            TrySelectAt(mouse.position.ReadValue());
        }

        public void Configure(CameraModeManager modeManager, Camera adminCamera, GameObject panel,
            TextMeshProUGUI nameText, TextMeshProUGUI idText, TextMeshProUGUI hallLabel, TextMeshProUGUI sizeLabel)
        {
            cameraModeManager = modeManager; topDownCamera = adminCamera; selectionPanel = panel;
            stallNameText = nameText; stallIdText = idText; hallText = hallLabel; sizeText = sizeLabel;
        }

        public void ConfigureBooking(StallBookingManager manager, TextMeshProUGUI status, TextMeshProUGUI exhibitor,
            Button book, Button edit, Button available, GameObject bookingPopup, GameObject confirmPopup,
            TMP_InputField exhibitorField, TextMeshProUGUI bookingStallLabel, TextMeshProUGUI errorLabel)
        {
            bookingManager = manager; statusText = status; exhibitorText = exhibitor;
            bookButton = book; editButton = edit; availableButton = available;
            bookingPanel = bookingPopup; availableConfirmPanel = confirmPopup; exhibitorInput = exhibitorField;
            bookingStallText = bookingStallLabel; bookingErrorText = errorLabel;
        }

        public void CloseSelection()
        {
            ClearHighlight(); selectedStall = null;
            if (selectionPanel != null) selectionPanel.SetActive(false);
            CloseBookingPopup(); CancelMakeAvailable();
        }

        public void SelectFromDashboard(StallIdentity stall)
        {
            if (stall == null) return;
            SelectStall(stall);
        }

        public void VisitSelectedStall()
        {
            if (selectedStall == null || cameraModeManager == null) return;
            Vector3 cameraPosition = selectedStall.VisitPoint != null ? selectedStall.VisitPoint.position :
                selectedStall.transform.position - selectedStall.transform.forward * Mathf.Max(8f, selectedStall.FootprintUnityUnits.y * 0.9f) + Vector3.up * 5.5f;
            Vector3 lookTarget = selectedStall.LookTarget != null ? selectedStall.LookTarget.position : selectedStall.transform.position + Vector3.up * 4.5f;
            ClearHighlight(); if (selectionPanel != null) selectionPanel.SetActive(false);
            cameraModeManager.FocusStall(cameraPosition, lookTarget);
        }

        public void OpenBookPopup() => OpenBookingPopup(false);
        public void OpenEditPopup() => OpenBookingPopup(true);

        public void ConfirmBooking()
        {
            bookingManager ??= StallBookingManager.Instance;
            if (selectedStall == null || bookingManager == null) return;
            string exhibitor = exhibitorInput != null ? exhibitorInput.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(exhibitor))
            {
                if (bookingErrorText != null) bookingErrorText.text = "Exhibitor / brand name is required.";
                return;
            }

            StallBookingRecord existing = bookingManager.Get(selectedStall.StallId);
            string logo = existing != null && existing.isBooked
                ? existing.logoReference
                : LocalDataManagementController.ConsumePendingLogo(selectedStall.StallId);

            bookingManager.Book(selectedStall.StallId, exhibitor, logo);
            CloseBookingPopup();
            RefreshSelectionUI();
        }

        public void CloseBookingPopup()
        {
            if (selectedStall != null)
                LocalDataManagementController.ClearPendingLogo(selectedStall.StallId);
            if (bookingPanel != null) bookingPanel.SetActive(false);
            if (bookingErrorText != null) bookingErrorText.text = string.Empty;
        }

        public void RequestMakeAvailable()
        {
            if (selectedStall != null && availableConfirmPanel != null) availableConfirmPanel.SetActive(true);
        }

        public void ConfirmMakeAvailable()
        {
            bookingManager ??= StallBookingManager.Instance;
            if (selectedStall != null && bookingManager != null) bookingManager.MakeAvailable(selectedStall.StallId);
            CancelMakeAvailable(); RefreshSelectionUI();
        }

        public void CancelMakeAvailable()
        {
            if (availableConfirmPanel != null) availableConfirmPanel.SetActive(false);
        }

        private void OpenBookingPopup(bool editing)
        {
            bookingManager ??= StallBookingManager.Instance;
            if (selectedStall == null || bookingPanel == null) return;
            LocalDataManagementController.ClearPendingLogo(selectedStall.StallId);
            StallBookingRecord record = bookingManager?.Get(selectedStall.StallId);
            if (bookingStallText != null) bookingStallText.text = $"{selectedStall.DisplayName}\n{selectedStall.StallId}";
            if (exhibitorInput != null) exhibitorInput.text = editing && record != null ? record.exhibitorName : string.Empty;
            if (bookingErrorText != null) bookingErrorText.text = string.Empty;
            bookingPanel.SetActive(true);
        }

        private void TrySelectAt(Vector2 screenPosition)
        {
            if (topDownCamera == null || !topDownCamera.gameObject.activeInHierarchy) return;
            Ray ray = topDownCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 2000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) { CloseSelection(); return; }
            StallIdentity stall = hit.collider.GetComponentInParent<StallIdentity>();
            if (stall == null) { CloseSelection(); return; }
            SelectStall(stall);
        }

        private void SelectStall(StallIdentity stall)
        {
            ClearHighlight(); selectedStall = stall;
            Transform highlight = stall.transform.Find("Phase5_SelectionHighlight");
            if (highlight != null) { activeHighlight = highlight.gameObject; activeHighlight.SetActive(true); }
            RefreshSelectionUI();
            if (selectionPanel != null) selectionPanel.SetActive(true);
        }

        private void RefreshSelectionUI()
        {
            if (selectedStall == null) return;
            if (stallNameText != null) stallNameText.text = string.IsNullOrWhiteSpace(selectedStall.DisplayName) ? "STALL" : selectedStall.DisplayName;
            if (stallIdText != null) stallIdText.text = $"ID: {selectedStall.StallId}";
            if (hallText != null) hallText.text = $"Hall: {selectedStall.Hall}";
            if (sizeText != null) sizeText.text = $"Size: {selectedStall.FootprintMeters.x:0.#} x {selectedStall.FootprintMeters.y:0.#} m";

            bookingManager ??= StallBookingManager.Instance;
            StallBookingRecord record = bookingManager?.Get(selectedStall.StallId);
            bool booked = record?.isBooked == true;
            if (statusText != null) statusText.text = booked ? "Status: BOOKED" : "Status: AVAILABLE";
            if (exhibitorText != null) exhibitorText.text = booked ? $"Exhibitor: {record.exhibitorName}" : "Exhibitor: —";
            if (bookButton != null) bookButton.gameObject.SetActive(!booked);
            if (editButton != null) editButton.gameObject.SetActive(booked);
            if (availableButton != null) availableButton.gameObject.SetActive(booked);
        }

        private void OnBookingChanged(string stallId)
        {
            if (selectedStall != null && selectedStall.StallId == stallId) RefreshSelectionUI();
        }

        private void ClearHighlight()
        {
            if (activeHighlight != null) activeHighlight.SetActive(false);
            activeHighlight = null;
        }
    }
}

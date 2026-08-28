using System;
using System.Collections.Generic;
using System.Linq;
using MeraBrand.Expo.Authentication;
using MeraBrand.Expo.Booking;
using MeraBrand.Expo.CameraSystem;
using MeraBrand.Expo.Stalls;
using TMPro;
using UnityEngine;

namespace MeraBrand.Expo.UI
{
    public sealed class AdminDashboardController : MonoBehaviour
    {
        [SerializeField] private GameObject dashboardPanel;
        [SerializeField] private TextMeshProUGUI totalText;
        [SerializeField] private TextMeshProUGUI bookedText;
        [SerializeField] private TextMeshProUGUI availableText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Dropdown hallDropdown;
        [SerializeField] private TMP_Dropdown statusDropdown;
        [SerializeField] private StallSelectionController selectionController;
        [SerializeField] private AdminTopDownCameraController topDownCamera;
        [SerializeField] private CameraModeManager cameraModeManager;

        private StallRegistry registry;
        private StallBookingManager bookingManager;
        private List<StallIdentity> currentMatches = new();
        private int currentMatchIndex;

        private void Start()
        {
            registry = StallRegistry.Instance ?? FindFirstObjectByType<StallRegistry>();
            bookingManager = StallBookingManager.Instance ?? FindFirstObjectByType<StallBookingManager>();
            bool isAdmin = SessionManager.Instance != null && SessionManager.Instance.IsAdmin;

            if (dashboardPanel != null)
                dashboardPanel.SetActive(false);

            if (!isAdmin)
                return;

            PopulateHallDropdown();
            if (bookingManager != null)
                bookingManager.BookingChanged += OnBookingChanged;
            RefreshCounters();
            SetResult("Search by stall ID, name, hall, or exhibitor.");
        }

        private void OnDestroy()
        {
            UIInteractionState.Release(this);
            if (bookingManager != null)
                bookingManager.BookingChanged -= OnBookingChanged;
        }

        public void ToggleDashboard()
        {
            if (SessionManager.Instance == null || !SessionManager.Instance.IsAdmin || dashboardPanel == null)
                return;

            if (dashboardPanel.activeSelf)
                CloseDashboard();
            else
                OpenDashboard();
        }

        public void OpenDashboard()
        {
            if (SessionManager.Instance == null || !SessionManager.Instance.IsAdmin || dashboardPanel == null)
                return;

            dashboardPanel.SetActive(true);
            RefreshCounters();
            PopulateHallDropdownIfEmpty();
            UIInteractionState.Acquire(this);
            cameraModeManager?.RefreshCursorState();
        }

        public void CloseDashboard()
        {
            if (dashboardPanel != null)
                dashboardPanel.SetActive(false);

            UIInteractionState.Release(this);
            cameraModeManager?.RefreshCursorState();
        }

        public void Search()
        {
            registry ??= StallRegistry.Instance ?? FindFirstObjectByType<StallRegistry>();
            bookingManager ??= StallBookingManager.Instance ?? FindFirstObjectByType<StallBookingManager>();
            if (registry == null)
            {
                SetResult("Stall registry unavailable.");
                return;
            }

            string query = searchInput != null ? searchInput.text.Trim() : string.Empty;
            string hallFilter = hallDropdown != null && hallDropdown.value > 0 ? hallDropdown.options[hallDropdown.value].text : string.Empty;
            int statusFilter = statusDropdown != null ? statusDropdown.value : 0;

            currentMatches = registry.Stalls.Where(stall => Matches(stall, query, hallFilter, statusFilter)).ToList();
            currentMatchIndex = 0;

            if (currentMatches.Count == 0)
            {
                SetResult("No matching stalls found.");
                return;
            }

            FocusCurrentMatch();
        }

        public void NextResult()
        {
            if (currentMatches == null || currentMatches.Count == 0)
            {
                Search();
                return;
            }
            currentMatchIndex = (currentMatchIndex + 1) % currentMatches.Count;
            FocusCurrentMatch();
        }

        public void PreviousResult()
        {
            if (currentMatches == null || currentMatches.Count == 0)
            {
                Search();
                return;
            }
            currentMatchIndex = (currentMatchIndex - 1 + currentMatches.Count) % currentMatches.Count;
            FocusCurrentMatch();
        }

        public void RefreshCounters()
        {
            registry ??= StallRegistry.Instance ?? FindFirstObjectByType<StallRegistry>();
            bookingManager ??= StallBookingManager.Instance ?? FindFirstObjectByType<StallBookingManager>();
            if (registry == null) return;

            int total = registry.Count;
            int booked = 0;
            foreach (StallIdentity stall in registry.Stalls)
            {
                if (stall != null && bookingManager != null && bookingManager.IsBooked(stall.StallId))
                    booked++;
            }
            int available = Mathf.Max(0, total - booked);

            if (totalText != null) totalText.text = $"Total: {total}";
            if (bookedText != null) bookedText.text = $"Booked: {booked}";
            if (availableText != null) availableText.text = $"Available: {available}";
        }

        private void PopulateHallDropdownIfEmpty()
        {
            if (hallDropdown != null && hallDropdown.options.Count <= 1)
                PopulateHallDropdown();
        }

        private void PopulateHallDropdown()
        {
            if (hallDropdown == null) return;
            registry ??= StallRegistry.Instance ?? FindFirstObjectByType<StallRegistry>();
            List<string> options = new() { "ALL HALLS" };
            if (registry != null)
            {
                options.AddRange(registry.Stalls
                    .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Hall))
                    .Select(s => s.Hall.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s));
            }
            hallDropdown.ClearOptions();
            hallDropdown.AddOptions(options);
            hallDropdown.value = 0;
        }

        private bool Matches(StallIdentity stall, string query, string hallFilter, int statusFilter)
        {
            if (stall == null) return false;
            if (!string.IsNullOrWhiteSpace(hallFilter) && !string.Equals(stall.Hall, hallFilter, StringComparison.OrdinalIgnoreCase)) return false;

            bool booked = bookingManager != null && bookingManager.IsBooked(stall.StallId);
            if (statusFilter == 1 && booked) return false;
            if (statusFilter == 2 && !booked) return false;

            if (string.IsNullOrWhiteSpace(query)) return true;
            StringComparison cmp = StringComparison.OrdinalIgnoreCase;
            StallBookingRecord record = bookingManager?.Get(stall.StallId);
            return (stall.StallId?.IndexOf(query, cmp) ?? -1) >= 0
                || (stall.DisplayName?.IndexOf(query, cmp) ?? -1) >= 0
                || (stall.Hall?.IndexOf(query, cmp) ?? -1) >= 0
                || (record?.exhibitorName?.IndexOf(query, cmp) ?? -1) >= 0;
        }

        private void FocusCurrentMatch()
        {
            if (currentMatches == null || currentMatches.Count == 0) return;
            StallIdentity stall = currentMatches[currentMatchIndex];
            cameraModeManager?.ShowTopDown();
            topDownCamera?.FocusOn(stall.transform.position);
            selectionController?.SelectFromDashboard(stall);

            StallBookingRecord record = bookingManager?.Get(stall.StallId);
            string state = record?.isBooked == true ? $"BOOKED — {record.exhibitorName}" : "AVAILABLE";
            SetResult($"{currentMatchIndex + 1}/{currentMatches.Count}  {stall.DisplayName}  |  {stall.StallId}  |  {stall.Hall}  |  {state}");
        }

        private void OnBookingChanged(string _)
        {
            RefreshCounters();
        }

        private void SetResult(string message)
        {
            if (resultText != null) resultText.text = message;
        }
    }
}

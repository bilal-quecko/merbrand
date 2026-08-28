using System;
using System.IO;
using MeraBrand.Expo.CameraSystem;
using MeraBrand.Expo.Stalls;
using MeraBrand.Expo.UI;
using TMPro;
using UnityEngine;

namespace MeraBrand.Expo.Booking
{
    public sealed class LocalDataManagementController : MonoBehaviour
    {
        [SerializeField] private GameObject adminPanel;
        [SerializeField] private StallSelectionController selectionController;
        [SerializeField] private TMP_InputField logoPathInput;
        [SerializeField] private TextMeshProUGUI statusText;

        private StallBookingManager bookingManager;
        private CameraModeManager cameraModeManager;
        private bool resetArmed;

        private void Start()
        {
            bookingManager = StallBookingManager.Instance;
            cameraModeManager = FindFirstObjectByType<CameraModeManager>();
            if (adminPanel != null)
                adminPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            UIInteractionState.Release(this);
        }

        public void ToggleAdminPanel()
        {
            if (adminPanel == null)
                return;

            if (adminPanel.activeSelf)
                CloseAdminPanel();
            else
                OpenAdminPanel();
        }

        public void OpenAdminPanel()
        {
            if (adminPanel == null)
                return;

            adminPanel.SetActive(true);
            UIInteractionState.Acquire(this);
            cameraModeManager ??= FindFirstObjectByType<CameraModeManager>();
            cameraModeManager?.RefreshCursorState();
        }

        public void CloseAdminPanel()
        {
            if (adminPanel != null)
                adminPanel.SetActive(false);

            UIInteractionState.Release(this);
            cameraModeManager ??= FindFirstObjectByType<CameraModeManager>();
            cameraModeManager?.RefreshCursorState();
        }

        public void ImportLogoForSelectedStall()
        {
            bookingManager ??= StallBookingManager.Instance;
            StallIdentity stall = selectionController != null ? selectionController.SelectedStall : null;
            if (stall == null) { SetStatus("Select a stall first."); return; }

            string path = logoPathInput != null ? logoPathInput.text.Trim().Trim('"') : string.Empty;
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(path))
                path = UnityEditor.EditorUtility.OpenFilePanel("Select Exhibitor Logo", string.Empty, "png,jpg,jpeg");
#endif
            if (string.IsNullOrWhiteSpace(path)) { SetStatus("No logo selected. In a PC build, paste the image file path into the Logo Path field."); return; }
            if (!File.Exists(path)) { SetStatus($"Logo file not found:\n{path}"); return; }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length > 2 * 1024 * 1024) { SetStatus("Logo is too large. Use an image below 2 MB."); return; }
                Texture2D test = new(2, 2);
                bool valid = test.LoadImage(bytes);
                Destroy(test);
                if (!valid) { SetStatus("The selected file is not a supported image."); return; }

                string ext = Path.GetExtension(path).ToLowerInvariant();
                string mime = ext == ".png" ? "image/png" : "image/jpeg";
                string dataUri = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";

                StallBookingRecord record = bookingManager?.Get(stall.StallId);
                if (record != null && record.isBooked)
                {
                    bookingManager.SetLogo(stall.StallId, dataUri);
                    SetStatus($"Logo updated for {stall.DisplayName}.");
                }
                else
                {
                    SetPendingLogo(stall.StallId, dataUri);
                    SetStatus($"Logo selected for {stall.DisplayName}. Confirm the booking to save it.");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Logo import failed: {ex.Message}");
            }
        }

        public void RemoveLogoFromSelectedStall()
        {
            bookingManager ??= StallBookingManager.Instance;
            StallIdentity stall = selectionController != null ? selectionController.SelectedStall : null;
            if (stall == null) { SetStatus("Select a stall first."); return; }
            bookingManager?.SetLogo(stall.StallId, string.Empty);
            ClearPendingLogo(stall.StallId);
            SetStatus($"Logo removed from {stall.DisplayName}.");
        }

        public static string ConsumePendingLogo(string stallId)
        {
            if (string.IsNullOrWhiteSpace(stallId)) return string.Empty;
            string key = PendingLogoKey(stallId);
            string value = PlayerPrefs.GetString(key, string.Empty);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            return value;
        }

        public static void ClearPendingLogo(string stallId)
        {
            if (string.IsNullOrWhiteSpace(stallId)) return;
            PlayerPrefs.DeleteKey(PendingLogoKey(stallId));
            PlayerPrefs.Save();
        }

        private static void SetPendingLogo(string stallId, string dataUri)
        {
            PlayerPrefs.SetString(PendingLogoKey(stallId), dataUri ?? string.Empty);
            PlayerPrefs.Save();
        }

        private static string PendingLogoKey(string stallId) => $"MERA_BRAND_PENDING_LOGO_{stallId}";

        public void ExportBackup()
        {
            bookingManager ??= StallBookingManager.Instance;
            if (bookingManager == null) { SetStatus("Booking manager unavailable."); return; }
            try { SetStatus($"Backup exported:\n{bookingManager.ExportBackup()}"); }
            catch (Exception ex) { SetStatus($"Export failed: {ex.Message}"); }
        }

        public void ImportBackup()
        {
            bookingManager ??= StallBookingManager.Instance;
            if (bookingManager == null) { SetStatus("Booking manager unavailable."); return; }
            bookingManager.ImportFromDefaultFile(out string message);
            SetStatus(message);
        }

        public void OpenLocalDataFolder()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SetStatus("WebGL data is stored in this browser and has no normal filesystem folder.");
#else
            try
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                string folder = Application.persistentDataPath.Replace('/', '\\');
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                System.Diagnostics.Process.Start("open", Application.persistentDataPath);
#else
                Application.OpenURL("file://" + Application.persistentDataPath);
#endif
                SetStatus("Opened local data folder.");
            }
            catch (Exception ex) { SetStatus($"Could not open folder: {ex.Message}\n{Application.persistentDataPath}"); }
#endif
        }

        public void ResetAllBookings()
        {
            bookingManager ??= StallBookingManager.Instance;
            if (bookingManager == null) return;
            if (!resetArmed)
            {
                resetArmed = true;
                SetStatus("RESET is armed. Press RESET ALL again to permanently clear every local booking.");
                return;
            }

            resetArmed = false;
            bookingManager.ResetAllBookings();
            selectionController?.CloseSelection();
            SetStatus("All local booking data has been cleared.");
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
            Debug.Log($"[Local Data] {message}");
        }
    }
}

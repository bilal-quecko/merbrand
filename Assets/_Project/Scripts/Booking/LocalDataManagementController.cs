using System;
using System.IO;
using MeraBrand.Expo.Authentication;
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
            if (logoPathInput != null && logoPathInput.placeholder is TMP_Text placeholder)
                placeholder.text = "Optional image path, or click UPLOAD LOGO";
            if (adminPanel != null)
                adminPanel.SetActive(false);

            if (!IsAdmin())
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            UIInteractionState.Release(this);
        }

        public void ToggleAdminPanel()
        {
            if (!IsAdmin()) return;
            if (adminPanel == null)
                return;

            if (adminPanel.activeSelf)
                CloseAdminPanel();
            else
                OpenAdminPanel();
        }

        public void OpenAdminPanel()
        {
            if (!IsAdmin()) return;
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
            if (!IsAdmin()) return;
            bookingManager ??= StallBookingManager.Instance;
            StallIdentity stall = selectionController != null ? selectionController.SelectedStall : null;
            if (stall == null) { SetStatus("Select a stall first."); return; }

            string path = logoPathInput != null ? logoPathInput.text.Trim().Trim('"') : string.Empty;
            try
            {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                string directory = File.Exists(path) ? Path.GetDirectoryName(path) : string.Empty;
#if UNITY_EDITOR
                path = UnityEditor.EditorUtility.OpenFilePanel("Select Exhibitor Logo", directory, "png,jpg,jpeg");
#else
                path = WindowsLogoFilePicker.Open(directory);
#endif
                if (string.IsNullOrWhiteSpace(path)) { SetStatus("Logo selection cancelled."); return; }
#else
                if (string.IsNullOrWhiteSpace(path)) { SetStatus("Enter the full path of a PNG or JPG logo image."); return; }
#endif
                if (!File.Exists(path)) { SetStatus($"Logo file not found:\n{path}"); return; }
                if (logoPathInput != null) logoPathInput.SetTextWithoutNotify(path);
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length > 2 * 1024 * 1024) { SetStatus("Logo is too large. Use an image below 2 MB."); return; }
                bytes = ResizeLogo(bytes);
                if (bytes == null) { SetStatus("The selected file is not a supported image."); return; }
                string dataUri = $"data:image/png;base64,{Convert.ToBase64String(bytes)}";

                StallBookingRecord record = bookingManager?.Get(stall.StallId);
                if (record != null && record.isBooked)
                {
                    bookingManager.SetLogo(stall.StallId, dataUri, (success, message) =>
                        SetStatus(success ? $"Logo updated for {stall.DisplayName}." : message));
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

        private static byte[] ResizeLogo(byte[] bytes)
        {
            const int size = 215;
            Texture2D source = new(2, 2, TextureFormat.RGBA32, false);
            Texture2D resized = null;
            try
            {
                if (!source.LoadImage(bytes)) return null;
                source.wrapMode = TextureWrapMode.Clamp;
                float scale = Mathf.Min((float)size / source.width, (float)size / source.height);
                int width = Mathf.Clamp(Mathf.RoundToInt(source.width * scale), 1, size);
                int height = Mathf.Clamp(Mathf.RoundToInt(source.height * scale), 1, size);
                int left = (size - width) / 2;
                int bottom = (size - height) / 2;
                // Fit the entire logo within a transparent square without stretching or cropping.
                Color[] pixels = new Color[size * size];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        pixels[(bottom + y) * size + left + x] =
                            source.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height);
                resized = new Texture2D(size, size, TextureFormat.RGBA32, false);
                resized.SetPixels(pixels);
                resized.Apply(false, false);
                return resized.EncodeToPNG();
            }
            finally
            {
                Destroy(source);
                if (resized != null) Destroy(resized);
            }
        }

        public void RemoveLogoFromSelectedStall()
        {
            if (!IsAdmin()) return;
            bookingManager ??= StallBookingManager.Instance;
            StallIdentity stall = selectionController != null ? selectionController.SelectedStall : null;
            if (stall == null) { SetStatus("Select a stall first."); return; }
            bookingManager?.SetLogo(stall.StallId, string.Empty, (success, message) =>
                SetStatus(success ? $"Logo removed from {stall.DisplayName}." : message));
            ClearPendingLogo(stall.StallId);
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

        public static string GetPendingLogo(string stallId) =>
            string.IsNullOrWhiteSpace(stallId) ? string.Empty :
            PlayerPrefs.GetString(PendingLogoKey(stallId), string.Empty);

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
            if (!IsAdmin()) return;
            bookingManager ??= StallBookingManager.Instance;
            if (bookingManager == null) { SetStatus("Booking manager unavailable."); return; }
            try { SetStatus($"Backup exported:\n{bookingManager.ExportBackup()}"); }
            catch (Exception ex) { SetStatus($"Export failed: {ex.Message}"); }
        }

        public void ImportBackup()
        {
            if (!IsAdmin()) return;
            bookingManager ??= StallBookingManager.Instance;
            if (bookingManager == null) { SetStatus("Booking manager unavailable."); return; }
            SetStatus("Importing into Supabase...");
            bookingManager.ImportFromDefaultFile((success, message) =>
            {
                SetStatus(success ? "Imported booking records into Supabase." : message);
                if (success) bookingManager.Refresh();
            });
        }

        public void OpenLocalDataFolder()
        {
            if (!IsAdmin()) return;
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
            if (!IsAdmin()) return;
            bookingManager ??= StallBookingManager.Instance;
            if (bookingManager == null) return;
            if (!resetArmed)
            {
                resetArmed = true;
                SetStatus("RESET is armed. Press RESET ALL again to clear every shared booking.");
                return;
            }

            resetArmed = false;
            SetStatus("Clearing shared bookings...");
            bookingManager.ResetAllBookings((success, message) =>
            {
                if (success) selectionController?.CloseSelection();
                SetStatus(message);
            });
        }

        private static bool IsAdmin()
        {
            return SessionManager.Instance != null && SessionManager.Instance.IsAdmin;
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
            Debug.Log($"[Local Data] {message}");
        }
    }
}

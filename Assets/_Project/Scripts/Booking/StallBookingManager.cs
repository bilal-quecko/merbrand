using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MeraBrand.Expo.Authentication;
using MeraBrand.Expo.Core;
using MeraBrand.Expo.Stalls;
using UnityEngine;
using UnityEngine.Networking;

namespace MeraBrand.Expo.Booking
{
    public sealed class StallBookingManager : MonoBehaviour
    {
        public static StallBookingManager Instance { get; private set; }
        private const string Path = "/rest/v1/stall_bookings";
        private StallBookingDatabase database = new();
        private readonly Dictionary<string, StallBookingRecord> byId = new();
        private bool writing;
        private float nextRefreshTime;
        public event Action<string> BookingChanged;
        public event Action DatabaseReloaded;
        public bool IsLoaded { get; private set; }
        public string LoadError { get; private set; } = string.Empty;
        public string LocalDataFolder => Application.persistentDataPath;

        private void Awake() => Instance = this;
        private void Start() { ValidateSceneIds(); Refresh(); }
        private void Update()
        {
            if (IsLoaded && !writing && Time.unscaledTime >= nextRefreshTime)
                Refresh();
        }
        public void Refresh() => StartCoroutine(LoadBookings());

        public StallBookingRecord Get(string stallId)
        {
            if (string.IsNullOrWhiteSpace(stallId)) return null;
            byId.TryGetValue(stallId, out StallBookingRecord record);
            return record;
        }
        public bool IsBooked(string stallId) => Get(stallId)?.isBooked == true;

        public void Book(string stallId, string exhibitorName, string logoReference = null, Action<bool, string> completed = null)
        {
            if (string.IsNullOrWhiteSpace(stallId) || string.IsNullOrWhiteSpace(exhibitorName))
            { completed?.Invoke(false, "Choose a stall and enter an exhibitor name."); return; }
            Save(new StallBookingRecord { stallId = stallId, isBooked = true, exhibitorName = exhibitorName.Trim(),
                logoReference = logoReference ?? Get(stallId)?.logoReference ?? string.Empty }, completed);
        }

        public void SetLogo(string stallId, string dataReference, Action<bool, string> completed = null)
        {
            StallBookingRecord existing = Get(stallId);
            if (existing == null || !existing.isBooked)
            { completed?.Invoke(false, "Book this stall before saving its logo."); return; }
            Save(new StallBookingRecord { stallId = stallId, isBooked = true, exhibitorName = existing.exhibitorName,
                logoReference = dataReference ?? string.Empty }, completed);
        }

        public void MakeAvailable(string stallId, Action<bool, string> completed = null)
        {
            if (string.IsNullOrWhiteSpace(stallId))
            { completed?.Invoke(false, "Select a stall first."); return; }
            Save(new StallBookingRecord { stallId = stallId, isBooked = false, exhibitorName = string.Empty,
                logoReference = string.Empty }, completed);
        }

        private IEnumerator LoadBookings()
        {
            IsLoaded = false; LoadError = string.Empty;
            using (UnityWebRequest request = UnityWebRequest.Get(SupabaseStallsClient.ProjectUrl +
                Path + "?select=stall_id,is_booked,exhibitor_name,logo_reference,updated_at"))
            {
                request.SetRequestHeader("apikey", SupabaseStallsClient.ApiKey);
                request.timeout = 20;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    LoadError = $"Could not load bookings (HTTP {request.responseCode}). Check internet and retry.";
                    Debug.LogWarning(LoadError + " " + request.error); yield break;
                }
                BookingRows response;
                try { response = Parse(request.downloadHandler.text); }
                catch (Exception ex)
                { LoadError = "Could not read bookings response."; Debug.LogWarning(ex.Message); yield break; }
                if (response?.items == null) { LoadError = "Bookings response has no items."; yield break; }
                database = new StallBookingDatabase();
                foreach (BookingRow row in response.items) database.records.Add(FromRow(row));
                RebuildIndex(); IsLoaded = true; nextRefreshTime = Time.unscaledTime + 30f;
                ApplyAllVisuals(); DatabaseReloaded?.Invoke();
                Debug.Log($"Supabase: loaded {database.records.Count} booking records.");
            }
        }

        private void Save(StallBookingRecord record, Action<bool, string> completed)
        {
            if (!IsLoaded)
            { completed?.Invoke(false, string.IsNullOrEmpty(LoadError) ? "Bookings are still loading." : LoadError); return; }
            if (writing) { completed?.Invoke(false, "Another booking change is still saving."); return; }
            if (SessionManager.Instance == null || !SessionManager.Instance.IsAdmin)
            { completed?.Invoke(false, "Admin session expired. Sign in again."); return; }
            writing = true;
            StartCoroutine(Upsert(new[] { record }, completed));
        }

        private IEnumerator Upsert(StallBookingRecord[] records, Action<bool, string> completed)
        {
            string token = SessionManager.Instance?.AdminAccessToken;
            if (string.IsNullOrEmpty(token))
            { writing = false; completed?.Invoke(false, "Admin session expired. Sign in again."); yield break; }
            BookingWriteRows rows = new() { items = new BookingWriteRow[records.Length] };
            for (int i = 0; i < records.Length; i++) rows.items[i] = ToRow(records[i]);
            string wrapped = JsonUtility.ToJson(rows);
            string body = wrapped.Substring(9, wrapped.Length - 10);
            using (UnityWebRequest request = new UnityWebRequest(SupabaseStallsClient.ProjectUrl +
                Path + "?on_conflict=stall_id", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", SupabaseStallsClient.ApiKey);
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=representation");
                request.timeout = 30;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    string message = $"Booking save failed (HTTP {request.responseCode}).";
                    Debug.LogWarning(message + " " + request.downloadHandler.text);
                    writing = false;
                    completed?.Invoke(false, message); yield break;
                }
                BookingRows saved;
                try { saved = Parse(request.downloadHandler.text); }
                catch (Exception) { saved = null; }
                if (saved?.items == null || saved.items.Length != records.Length)
                { writing = false; completed?.Invoke(false, "Server response could not be verified. Refresh bookings."); yield break; }
                foreach (BookingRow row in saved.items)
                {
                    StallBookingRecord updated = FromRow(row);
                    byId[updated.stallId] = updated;
                    ApplyVisual(updated.stallId);
                    BookingChanged?.Invoke(updated.stallId);
                }
                database.records = new List<StallBookingRecord>(byId.Values);
                writing = false;
                nextRefreshTime = Time.unscaledTime + 30f;
                completed?.Invoke(true, string.Empty);
            }
        }

        public string ExportBackup()
        {
            if (!IsLoaded) return "Bookings are not loaded yet.";
            string json = JsonUtility.ToJson(database, true);
#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.SetString("MERA_BRAND_LAST_EXPORT_V1", json); PlayerPrefs.Save();
            return "Browser export snapshot saved.";
#else
            string folder = System.IO.Path.Combine(Application.persistentDataPath, "Backups");
            Directory.CreateDirectory(folder);
            string file = System.IO.Path.Combine(folder, $"stall_bookings_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(file, json); return file;
#endif
        }

        public void ImportFromDefaultFile(Action<bool, string> completed)
        {
            string json;
#if UNITY_WEBGL && !UNITY_EDITOR
            json = PlayerPrefs.GetString("MERA_BRAND_LAST_EXPORT_V1", string.Empty);
#else
            string file = System.IO.Path.Combine(Application.persistentDataPath, "import_bookings.json");
            if (!File.Exists(file))
            { completed?.Invoke(false, $"Place import_bookings.json in:\n{Application.persistentDataPath}"); return; }
            json = File.ReadAllText(file);
#endif
            StallBookingDatabase imported;
            try { imported = JsonUtility.FromJson<StallBookingDatabase>(json); }
            catch (Exception) { imported = null; }
            if (imported?.records == null || imported.records.Count == 0)
            { completed?.Invoke(false, "Import file contains no booking records."); return; }
            if (!IsLoaded || writing || SessionManager.Instance == null || !SessionManager.Instance.IsAdmin)
            { completed?.Invoke(false, "Load bookings and sign in as admin first."); return; }
            writing = true;
            StartCoroutine(Upsert(imported.records.ToArray(), completed));
        }

        public void ResetAllBookings(Action<bool, string> completed)
        {
            if (!IsLoaded || writing || SessionManager.Instance == null || !SessionManager.Instance.IsAdmin)
            { completed?.Invoke(false, "Load bookings and sign in as admin first."); return; }
            writing = true;
            StartCoroutine(DeleteAll(completed));
        }

        private IEnumerator DeleteAll(Action<bool, string> completed)
        {
            using (UnityWebRequest request = new UnityWebRequest(SupabaseStallsClient.ProjectUrl +
                Path + "?stall_id=not.is.null", "DELETE"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("apikey", SupabaseStallsClient.ApiKey);
                request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Instance.AdminAccessToken);
                request.SetRequestHeader("Prefer", "return=representation");
                request.timeout = 30;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                { writing = false; completed?.Invoke(false, $"Reset failed (HTTP {request.responseCode})."); yield break; }
                BookingRows deleted;
                try { deleted = Parse(request.downloadHandler.text); }
                catch (Exception) { deleted = null; }
                if (deleted?.items == null)
                { writing = false; completed?.Invoke(false, "Reset response could not be verified. Refresh bookings."); yield break; }
                database = new StallBookingDatabase(); byId.Clear(); ApplyAllVisuals(); DatabaseReloaded?.Invoke();
                writing = false;
                nextRefreshTime = Time.unscaledTime + 30f;
                completed?.Invoke(true, $"Cleared {deleted.items.Length} shared booking records.");
            }
        }

        public bool ValidateSceneIds()
        {
            StallIdentity[] stalls = FindObjectsByType<StallIdentity>(FindObjectsSortMode.None);
            HashSet<string> ids = new(); bool valid = true;
            foreach (StallIdentity stall in stalls)
            {
                if (stall == null) continue;
                if (string.IsNullOrWhiteSpace(stall.StallId) || stall.StallId == "UNASSIGNED")
                { Debug.LogError($"Booking disabled for '{stall.name}': missing Stall ID."); valid = false; continue; }
                if (!ids.Add(stall.StallId))
                { Debug.LogError($"Duplicate Stall ID detected: {stall.StallId}."); valid = false; }
            }
            return valid;
        }

        private void RebuildIndex()
        {
            byId.Clear();
            foreach (StallBookingRecord record in database.records)
                if (record != null && !string.IsNullOrWhiteSpace(record.stallId)) byId[record.stallId] = record;
        }
        private static BookingRows Parse(string json) =>
            JsonUtility.FromJson<BookingRows>("{\"items\":" + json + "}");
        private static BookingWriteRow ToRow(StallBookingRecord record) => new()
        { stall_id = record.stallId, is_booked = record.isBooked, exhibitor_name = record.exhibitorName ?? string.Empty,
            logo_reference = record.logoReference ?? string.Empty };
        private static StallBookingRecord FromRow(BookingRow row) => new()
        { stallId = row.stall_id, isBooked = row.is_booked, exhibitorName = row.exhibitor_name,
            logoReference = row.logo_reference, updatedUtc = row.updated_at };
        private void ApplyAllVisuals()
        {
            foreach (StallIdentity stall in FindObjectsByType<StallIdentity>(FindObjectsSortMode.None)) ApplyVisual(stall);
        }
        private void ApplyVisual(string stallId)
        {
            foreach (StallIdentity stall in FindObjectsByType<StallIdentity>(FindObjectsSortMode.None))
                if (stall != null && stall.StallId == stallId) { ApplyVisual(stall); return; }
        }
        private void ApplyVisual(StallIdentity stall)
        {
            if (stall == null) return;
            StallBookingVisual visual = stall.GetComponent<StallBookingVisual>();
            if (visual != null) visual.Apply(Get(stall.StallId));
        }

        [Serializable] private sealed class BookingRows { public BookingRow[] items; }
        [Serializable] private sealed class BookingWriteRows { public BookingWriteRow[] items; }
        [Serializable] private sealed class BookingWriteRow
        {
            public string stall_id;
            public bool is_booked;
            public string exhibitor_name;
            public string logo_reference;
        }
        [Serializable] private sealed class BookingRow
        {
            public string stall_id;
            public bool is_booked;
            public string exhibitor_name;
            public string logo_reference;
            public string updated_at;
        }
    }
}

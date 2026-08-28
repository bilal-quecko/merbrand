using System;
using System.Collections.Generic;
using System.IO;
using MeraBrand.Expo.Stalls;
using UnityEngine;

namespace MeraBrand.Expo.Booking
{
    public sealed class StallBookingManager : MonoBehaviour
    {
        public static StallBookingManager Instance { get; private set; }

        private IBookingRepository repository;
        private StallBookingDatabase database;
        private readonly Dictionary<string, StallBookingRecord> byId = new();

        public event Action<string> BookingChanged;
        public event Action DatabaseReloaded;

        public string LocalDataFolder => Application.persistentDataPath;

        private void Awake()
        {
            Instance = this;
            repository = new LocalBookingRepository();
            database = repository.Load();
            RebuildIndex();
        }

        private void Start()
        {
            ValidateSceneIds();
            ApplyAllVisuals();
        }

        public StallBookingRecord Get(string stallId)
        {
            if (string.IsNullOrWhiteSpace(stallId)) return null;
            byId.TryGetValue(stallId, out StallBookingRecord record);
            return record;
        }

        public bool IsBooked(string stallId) => Get(stallId)?.isBooked == true;

        public void Book(string stallId, string exhibitorName, string logoReference = null)
        {
            if (string.IsNullOrWhiteSpace(stallId) || string.IsNullOrWhiteSpace(exhibitorName)) return;
            StallBookingRecord record = GetOrCreate(stallId);
            record.isBooked = true;
            record.exhibitorName = exhibitorName.Trim();
            if (logoReference != null) record.logoReference = logoReference;
            record.updatedUtc = DateTime.UtcNow.ToString("O");
            PersistAndApply(stallId);
        }

        public void SetLogo(string stallId, string dataReference)
        {
            if (string.IsNullOrWhiteSpace(stallId)) return;
            StallBookingRecord record = GetOrCreate(stallId);
            record.logoReference = dataReference ?? string.Empty;
            record.updatedUtc = DateTime.UtcNow.ToString("O");
            PersistAndApply(stallId);
        }

        public void MakeAvailable(string stallId)
        {
            if (string.IsNullOrWhiteSpace(stallId)) return;
            StallBookingRecord record = GetOrCreate(stallId);
            record.isBooked = false;
            record.exhibitorName = string.Empty;
            record.logoReference = string.Empty;
            record.updatedUtc = DateTime.UtcNow.ToString("O");
            PersistAndApply(stallId);
        }

        public string ExportBackup()
        {
            string json = JsonUtility.ToJson(database ?? new StallBookingDatabase(), true);
#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.SetString("MERA_BRAND_LAST_EXPORT_V1", json);
            PlayerPrefs.Save();
            return "Browser local export snapshot saved.";
#else
            string folder = Path.Combine(Application.persistentDataPath, "Backups");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"stall_bookings_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path, json);
            return path;
#endif
        }

        public bool ImportFromJson(string json, out string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) { message = "Import data is empty."; return false; }
                StallBookingDatabase imported = JsonUtility.FromJson<StallBookingDatabase>(json);
                if (imported == null || imported.records == null) { message = "Invalid booking backup."; return false; }
                database = imported;
                RebuildIndex();
                repository.Save(database);
                ApplyAllVisuals();
                DatabaseReloaded?.Invoke();
                message = $"Imported {database.records.Count} booking records.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Import failed: {ex.Message}";
                return false;
            }
        }

        public bool ImportFromDefaultFile(out string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string json = PlayerPrefs.GetString("MERA_BRAND_LAST_EXPORT_V1", string.Empty);
            return ImportFromJson(json, out message);
#else
            string path = Path.Combine(Application.persistentDataPath, "import_bookings.json");
            if (!File.Exists(path)) { message = $"Place import_bookings.json in:\n{Application.persistentDataPath}"; return false; }
            return ImportFromJson(File.ReadAllText(path), out message);
#endif
        }

        public void ResetAllBookings()
        {
            database = new StallBookingDatabase();
            RebuildIndex();
            repository.Save(database);
            ApplyAllVisuals();
            DatabaseReloaded?.Invoke();
        }

        public bool ValidateSceneIds()
        {
            StallIdentity[] stalls = FindObjectsByType<StallIdentity>(FindObjectsSortMode.None);
            HashSet<string> ids = new();
            bool valid = true;
            foreach (StallIdentity stall in stalls)
            {
                if (stall == null) continue;
                if (string.IsNullOrWhiteSpace(stall.StallId) || stall.StallId == "UNASSIGNED")
                {
                    Debug.LogError($"Booking disabled for '{stall.name}': missing Stall ID.");
                    valid = false; continue;
                }
                if (!ids.Add(stall.StallId))
                {
                    Debug.LogError($"Duplicate Stall ID detected: {stall.StallId}. Each stall must have a unique ID before booking.");
                    valid = false;
                }
            }
            return valid;
        }

        private StallBookingRecord GetOrCreate(string stallId)
        {
            if (byId.TryGetValue(stallId, out StallBookingRecord existing)) return existing;
            StallBookingRecord record = new() { stallId = stallId, isBooked = false, exhibitorName = string.Empty, logoReference = string.Empty, updatedUtc = DateTime.UtcNow.ToString("O") };
            database.records.Add(record); byId[stallId] = record; return record;
        }

        private void PersistAndApply(string stallId)
        {
            repository.Save(database); ApplyVisual(stallId); BookingChanged?.Invoke(stallId);
        }

        private void RebuildIndex()
        {
            database ??= new StallBookingDatabase(); database.records ??= new List<StallBookingRecord>();
            byId.Clear();
            foreach (StallBookingRecord record in database.records)
                if (record != null && !string.IsNullOrWhiteSpace(record.stallId)) byId[record.stallId] = record;
        }

        private void ApplyAllVisuals()
        {
            StallIdentity[] stalls = FindObjectsByType<StallIdentity>(FindObjectsSortMode.None);
            foreach (StallIdentity stall in stalls) ApplyVisual(stall);
        }

        private void ApplyVisual(string stallId)
        {
            StallIdentity[] stalls = FindObjectsByType<StallIdentity>(FindObjectsSortMode.None);
            foreach (StallIdentity stall in stalls)
                if (stall != null && stall.StallId == stallId) { ApplyVisual(stall); return; }
        }

        private void ApplyVisual(StallIdentity stall)
        {
            if (stall == null) return;
            StallBookingVisual visual = stall.GetComponent<StallBookingVisual>();
            if (visual != null) visual.Apply(Get(stall.StallId));
        }
    }
}

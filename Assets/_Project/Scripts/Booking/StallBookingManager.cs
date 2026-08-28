using System;
using System.Collections.Generic;
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
            if (string.IsNullOrWhiteSpace(stallId))
                return null;
            byId.TryGetValue(stallId, out StallBookingRecord record);
            return record;
        }

        public bool IsBooked(string stallId) => Get(stallId)?.isBooked == true;

        public void Book(string stallId, string exhibitorName, string logoReference = "")
        {
            if (string.IsNullOrWhiteSpace(stallId) || string.IsNullOrWhiteSpace(exhibitorName))
                return;

            StallBookingRecord record = GetOrCreate(stallId);
            record.isBooked = true;
            record.exhibitorName = exhibitorName.Trim();
            record.logoReference = logoReference ?? string.Empty;
            record.updatedUtc = DateTime.UtcNow.ToString("O");
            PersistAndApply(stallId);
        }

        public void MakeAvailable(string stallId)
        {
            if (string.IsNullOrWhiteSpace(stallId))
                return;

            StallBookingRecord record = GetOrCreate(stallId);
            record.isBooked = false;
            record.exhibitorName = string.Empty;
            record.logoReference = string.Empty;
            record.updatedUtc = DateTime.UtcNow.ToString("O");
            PersistAndApply(stallId);
        }

        public bool ValidateSceneIds()
        {
            StallIdentity[] stalls = FindObjectsByType<StallIdentity>(FindObjectsSortMode.None);
            HashSet<string> ids = new();
            bool valid = true;

            foreach (StallIdentity stall in stalls)
            {
                if (stall == null)
                    continue;

                if (string.IsNullOrWhiteSpace(stall.StallId) || stall.StallId == "UNASSIGNED")
                {
                    Debug.LogError($"Booking disabled for '{stall.name}': missing Stall ID.");
                    valid = false;
                    continue;
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
            if (byId.TryGetValue(stallId, out StallBookingRecord existing))
                return existing;

            StallBookingRecord record = new()
            {
                stallId = stallId,
                isBooked = false,
                exhibitorName = string.Empty,
                logoReference = string.Empty,
                updatedUtc = DateTime.UtcNow.ToString("O")
            };
            database.records.Add(record);
            byId[stallId] = record;
            return record;
        }

        private void PersistAndApply(string stallId)
        {
            repository.Save(database);
            ApplyVisual(stallId);
            BookingChanged?.Invoke(stallId);
        }

        private void RebuildIndex()
        {
            database ??= new StallBookingDatabase();
            database.records ??= new List<StallBookingRecord>();
            byId.Clear();
            foreach (StallBookingRecord record in database.records)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.stallId))
                    continue;
                byId[record.stallId] = record;
            }
        }

        private void ApplyAllVisuals()
        {
            StallIdentity[] stalls = FindObjectsByType<StallIdentity>(FindObjectsSortMode.None);
            foreach (StallIdentity stall in stalls)
                ApplyVisual(stall);
        }

        private void ApplyVisual(string stallId)
        {
            StallIdentity[] stalls = FindObjectsByType<StallIdentity>(FindObjectsSortMode.None);
            foreach (StallIdentity stall in stalls)
            {
                if (stall != null && stall.StallId == stallId)
                {
                    ApplyVisual(stall);
                    return;
                }
            }
        }

        private void ApplyVisual(StallIdentity stall)
        {
            if (stall == null)
                return;
            StallBookingVisual visual = stall.GetComponent<StallBookingVisual>();
            if (visual != null)
                visual.Apply(Get(stall.StallId));
        }
    }
}

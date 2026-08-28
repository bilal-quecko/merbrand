using System.IO;
using UnityEngine;

namespace MeraBrand.Expo.Booking
{
    public sealed class LocalBookingRepository : IBookingRepository
    {
        private const string PlayerPrefsKey = "MERA_BRAND_STALL_BOOKINGS_V1";
        private readonly string filePath;

        public LocalBookingRepository()
        {
            filePath = Path.Combine(Application.persistentDataPath, "stall_bookings.json");
        }

        public StallBookingDatabase Load()
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
#else
                string json = File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;
#endif
                if (string.IsNullOrWhiteSpace(json))
                    return new StallBookingDatabase();

                StallBookingDatabase data = JsonUtility.FromJson<StallBookingDatabase>(json);
                return data ?? new StallBookingDatabase();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Failed to load stall bookings: {exception.Message}");
                return new StallBookingDatabase();
            }
        }

        public void Save(StallBookingDatabase database)
        {
            try
            {
                string json = JsonUtility.ToJson(database ?? new StallBookingDatabase(), true);
#if UNITY_WEBGL && !UNITY_EDITOR
                PlayerPrefs.SetString(PlayerPrefsKey, json);
                PlayerPrefs.Save();
#else
                File.WriteAllText(filePath, json);
#endif
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Failed to save stall bookings: {exception.Message}");
            }
        }
    }
}

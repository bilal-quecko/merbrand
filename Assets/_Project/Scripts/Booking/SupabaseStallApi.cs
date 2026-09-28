using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MeraBrand.Expo.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace MeraBrand.Expo.Booking
{
    [Serializable]
    public sealed class SupabaseStallRow
    {
        public string stall_code;
        public string hall;
        public string stall_type;
        public string sponsor_type;
        public string status;
        public string exhibitor_name;
        public string logo_reference;
        public string updated_at;
    }

    [Serializable]
    internal sealed class SupabaseStallRowList
    {
        public List<SupabaseStallRow> items = new();
    }

    public sealed class SupabaseStallApi
    {
        private readonly AppConfig config;
        private string userAccessToken = string.Empty;

        public SupabaseStallApi(AppConfig appConfig)
        {
            config = appConfig;
        }

        public void SetUserAccessToken(string accessToken) => userAccessToken = accessToken ?? string.Empty;

        public bool IsConfigured =>
            config != null &&
            !string.IsNullOrWhiteSpace(config.SupabaseProjectUrl) &&
            !string.IsNullOrWhiteSpace(config.SupabasePublishableKey) &&
            !string.IsNullOrWhiteSpace(config.SupabaseStallsTable);

        public IEnumerator FetchAll(Action<List<SupabaseStallRow>> onSuccess, Action<string> onError)
        {
            if (!IsConfigured)
            {
                onError?.Invoke("Supabase is not configured.");
                yield break;
            }

            string fields = "stall_code,hall,stall_type,sponsor_type,status,exhibitor_name,logo_reference,updated_at";
            string url = $"{BaseRestUrl}/{UnityWebRequest.EscapeURL(config.SupabaseStallsTable)}?select={fields}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            ApplyHeaders(request);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(BuildError(request));
                yield break;
            }

            try
            {
                string wrapped = "{\"items\":" + request.downloadHandler.text + "}";
                SupabaseStallRowList payload = JsonUtility.FromJson<SupabaseStallRowList>(wrapped);
                onSuccess?.Invoke(payload?.items ?? new List<SupabaseStallRow>());
            }
            catch (Exception exception)
            {
                onError?.Invoke($"Could not parse Supabase stall response: {exception.Message}");
            }
        }

        public IEnumerator UpdateBooking(
            StallBookingRecord record,
            Action onSuccess,
            Action<string> onError)
        {
            if (!IsConfigured)
            {
                onError?.Invoke("Supabase is not configured.");
                yield break;
            }

            if (!config.SupabaseRemoteWritesEnabled)
            {
                onError?.Invoke("Supabase remote writes are disabled in AppConfig.");
                yield break;
            }

            if (record == null || string.IsNullOrWhiteSpace(record.stallId))
            {
                onError?.Invoke("Cannot update Supabase without a stall ID.");
                yield break;
            }

            string encodedId = UnityWebRequest.EscapeURL(record.stallId);
            string url = $"{BaseRestUrl}/{UnityWebRequest.EscapeURL(config.SupabaseStallsTable)}?stall_code=eq.{encodedId}";

            SupabaseBookingPatch patch = new()
            {
                status = record.isBooked ? "booked" : "available",
                exhibitor_name = record.isBooked ? record.exhibitorName ?? string.Empty : string.Empty,
                logo_reference = record.isBooked ? record.logoReference ?? string.Empty : string.Empty,
                updated_at = DateTime.UtcNow.ToString("O")
            };

            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(patch));
            using UnityWebRequest request = new(url, UnityWebRequest.kHttpVerbPATCH)
            {
                uploadHandler = new UploadHandlerRaw(body),
                downloadHandler = new DownloadHandlerBuffer()
            };

            ApplyHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Prefer", "return=minimal");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(BuildError(request));
                yield break;
            }

            onSuccess?.Invoke();
        }

        private string BaseRestUrl => config.SupabaseProjectUrl.TrimEnd('/') + "/rest/v1";

        private void ApplyHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("apikey", config.SupabasePublishableKey);

            if (!string.IsNullOrWhiteSpace(userAccessToken))
                request.SetRequestHeader("Authorization", "Bearer " + userAccessToken);
        }

        private static string BuildError(UnityWebRequest request)
        {
            string response = request.downloadHandler?.text;
            return string.IsNullOrWhiteSpace(response)
                ? $"Supabase request failed ({request.responseCode}): {request.error}"
                : $"Supabase request failed ({request.responseCode}): {response}";
        }

        [Serializable]
        private sealed class SupabaseBookingPatch
        {
            public string status;
            public string exhibitor_name;
            public string logo_reference;
            public string updated_at;
        }
    }
}

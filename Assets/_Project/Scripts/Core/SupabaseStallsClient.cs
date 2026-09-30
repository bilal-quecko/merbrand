using System;
using System.Collections;
using System.Collections.Generic;
using MeraBrand.Expo.Stalls;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace MeraBrand.Expo.Core
{
    // Read-only floor-plan stall catalog. Bookings are loaded separately.
    public sealed class SupabaseStallsClient : MonoBehaviour
    {
        internal const string ProjectUrl = "https://xvajhfkbsvcexexcbzmg.supabase.co";
        private const string StallsPath = "/rest/v1/stalls?select=stall_code,display_name,hall,stall_class,sponsor_type,status,exhibitor_name,logo_url,unity_object_key,unity_stall_id,floorplan_key,sort_order,position_x,position_z,width_m,depth_m,rotation_y,scene_active&is_active=eq.true&order=stall_code.asc";
        private const string PublishableKey = "sb_publishable_2D74J2bZLqNrkuUygwjxsA_NnpXydL6";

        internal static string ApiKey => PublishableKey;

        public Stall[] Stalls { get; private set; } = Array.Empty<Stall>();
        public event Action<Stall[]> StallsLoaded;
        public event Action<string> LoadFailed;

        private Coroutine loadRoutine;

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyCatalogToScene();

        private void Start() => Refresh();

        public void Refresh()
        {
            if (!isActiveAndEnabled) return;
            if (loadRoutine != null) StopCoroutine(loadRoutine);
            loadRoutine = StartCoroutine(LoadStalls());
        }

        private IEnumerator LoadStalls()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(ProjectUrl + StallsPath))
            {
                request.SetRequestHeader("apikey", PublishableKey);
                request.timeout = 15;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    ReportFailure($"Supabase stall read failed: HTTP {request.responseCode} ({request.error})");
                    loadRoutine = null;
                    yield break;
                }

                StallList response;
                try
                {
                    response = JsonUtility.FromJson<StallList>("{\"items\":" + request.downloadHandler.text + "}");
                }
                catch (Exception exception)
                {
                    ReportFailure("Invalid Supabase stall response: " + exception.Message);
                    loadRoutine = null;
                    yield break;
                }

                if (response?.items == null)
                {
                    ReportFailure("Supabase stall response has no items.");
                    loadRoutine = null;
                    yield break;
                }

                Stalls = response.items;
                loadRoutine = null;
                ApplyCatalogToScene();
                Debug.Log($"Supabase: loaded {Stalls.Length} active stalls.");
                StallsLoaded?.Invoke(Stalls);
            }
        }

        private void ApplyCatalogToScene()
        {
            if (Stalls.Length == 0) return;
            Dictionary<string, Stall> byUnityId = new(StringComparer.OrdinalIgnoreCase);
            foreach (Stall stall in Stalls)
                if (!string.IsNullOrWhiteSpace(stall.unity_stall_id))
                    byUnityId[stall.unity_stall_id] = stall;
            foreach (StallIdentity identity in FindObjectsByType<StallIdentity>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (identity != null && byUnityId.TryGetValue(identity.StallId, out Stall catalogStall))
                    identity.ApplyCatalog(catalogStall);
            }
        }

        private void ReportFailure(string message)
        {
            Debug.LogWarning(message);
            LoadFailed?.Invoke(message);
        }

        [Serializable]
        private sealed class StallList
        {
            public Stall[] items;
        }

        [Serializable]
        public sealed class Stall
        {
            public string stall_code;
            public string display_name;
            public string hall;
            public string stall_class;
            public string sponsor_type;
            public string status;
            public string exhibitor_name;
            public string logo_url;
            public string unity_object_key;
            public string unity_stall_id;
            public string floorplan_key;
            public int sort_order;
            public float position_x;
            public float position_z;
            public float width_m;
            public float depth_m;
            public float rotation_y;
            public bool scene_active;
        }
    }
}

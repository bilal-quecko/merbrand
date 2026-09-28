using UnityEngine;

namespace MeraBrand.Expo.Core
{
    [CreateAssetMenu(fileName = "AppConfig", menuName = "Mera Brand/App Config")]
    public sealed class AppConfig : ScriptableObject
    {
        [Header("Application")]
        [SerializeField] private string applicationName = "Mera Brand Pakistan Family Expo 2026";
        [SerializeField] private string eventLocation = "Tulip Hall Islamabad";

        [Header("World Scale")]
        [Tooltip("Phase 1 convention: one Unity unit represents one foot in the source exhibition plan.")]
        [SerializeField] private float feetPerUnityUnit = 1f;

        [Header("Stall Sizes (feet)")]
        [SerializeField] private Vector2 standard3x3 = new(3f, 3f);
        [SerializeField] private Vector2 standard3x6 = new(3f, 6f);
        [SerializeField] private Vector2 standard6x6 = new(6f, 6f);

        [Header("Supabase")]
        [Tooltip("Example: https://your-project-ref.supabase.co")]
        [SerializeField] private string supabaseProjectUrl = string.Empty;
        [Tooltip("Client-side publishable key (sb_publishable_...). Never use a secret/service-role key here.")]
        [SerializeField] private string supabasePublishableKey = string.Empty;
        [SerializeField] private string supabaseStallsTable = "stalls";
        [Min(5f)]
        [SerializeField] private float supabaseSyncIntervalSeconds = 15f;
        [Tooltip("Enable only after Supabase Auth is wired to the Unity admin session and RLS permits authenticated writes.")]
        [SerializeField] private bool supabaseRemoteWritesEnabled;

        public string ApplicationName => applicationName;
        public string EventLocation => eventLocation;
        public float FeetPerUnityUnit => feetPerUnityUnit;
        public Vector2 Standard3x3 => standard3x3;
        public Vector2 Standard3x6 => standard3x6;
        public Vector2 Standard6x6 => standard6x6;
        public string SupabaseProjectUrl => supabaseProjectUrl;
        public string SupabasePublishableKey => supabasePublishableKey;
        public string SupabaseStallsTable => supabaseStallsTable;
        public float SupabaseSyncIntervalSeconds => Mathf.Max(5f, supabaseSyncIntervalSeconds);
        public bool SupabaseRemoteWritesEnabled => supabaseRemoteWritesEnabled;
    }
}

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

        public string ApplicationName => applicationName;
        public string EventLocation => eventLocation;
        public float FeetPerUnityUnit => feetPerUnityUnit;
        public Vector2 Standard3x3 => standard3x3;
        public Vector2 Standard3x6 => standard3x6;
        public Vector2 Standard6x6 => standard6x6;
    }
}

using UnityEngine;

namespace MeraBrand.Expo.Core
{
    public sealed class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

        [SerializeField] private AppConfig config;

        public AppConfig Config => config;
        public SupabaseStallsClient StallsClient { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            StallsClient = GetComponent<SupabaseStallsClient>() ?? gameObject.AddComponent<SupabaseStallsClient>();
        }

        public void SetConfig(AppConfig appConfig)
        {
            config = appConfig;
        }
    }
}

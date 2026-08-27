using UnityEngine;

namespace MeraBrand.Expo.Core
{
    public sealed class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

        [SerializeField] private AppConfig config;

        public AppConfig Config => config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetConfig(AppConfig appConfig)
        {
            config = appConfig;
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeraBrand.Expo.Core
{
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        private bool isLoading;
        public bool IsLoading => isLoading;

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

        public void Load(string sceneName)
        {
            if (isLoading)
                return;

            // Scene transitions must never inherit a paused simulation state.
            // Centralizing this here protects every current and future scene change.
            Time.timeScale = 1f;

            StartCoroutine(LoadRoutine(sceneName));
        }

        public void LoadMainMenu() => Load(SceneNames.MainMenu);
        public void LoadExhibition() => Load(SceneNames.Exhibition);

        private IEnumerator LoadRoutine(string sceneName)
        {
            isLoading = true;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"Unable to start loading scene '{sceneName}'. Check Build Settings.");
                isLoading = false;
                yield break;
            }

            // Async scene loading progresses independently of scaled game time.
            // Yielding frames is safe even if another system changes timeScale later.
            while (!operation.isDone)
                yield return null;

            isLoading = false;
        }
    }
}

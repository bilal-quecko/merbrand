using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

namespace MeraBrand.Expo.Core
{
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        private bool isLoading;
        public bool IsLoading => isLoading;
        public string ExhibitionStatus { get; private set; } = string.Empty;
        public event Action<string> ExhibitionStatusChanged;

#if UNITY_WEBGL && !UNITY_EDITOR
        private Coroutine exhibitionDownload;
        private bool exhibitionReady;
        private string WebExhibitionAddress
        {
            get
            {
                // The page selects lite mode for phones and devices with little memory.
                // Mobile platforms also default to it when opened outside our page template.
                string url = Application.absoluteURL;
                bool liteRequested = !string.IsNullOrEmpty(url) &&
                    System.Text.RegularExpressions.Regex.IsMatch(url, @"[?&]lite=1(?:&|#|$)");
                return Application.isMobilePlatform || liteRequested
                    ? SceneNames.ExhibitionLite : SceneNames.Exhibition;
            }
        }
#endif

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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void PrefetchExhibition()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!exhibitionReady && exhibitionDownload == null)
                exhibitionDownload = StartCoroutine(DownloadExhibition());
#endif
        }

        public void Load(string sceneName)
        {
            if (isLoading)
                return;

            // Scene transitions must never inherit a paused simulation state.
            Time.timeScale = 1f;
            StartCoroutine(LoadRoutine(sceneName));
        }

        public void LoadMainMenu() => Load(SceneNames.MainMenu);
        public void LoadExhibition() => Load(SceneNames.Exhibition);

        private IEnumerator LoadRoutine(string sceneName)
        {
            isLoading = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (sceneName == SceneNames.Exhibition)
            {
                yield return LoadRemoteExhibition();
                isLoading = false;
                yield break;
            }
#endif
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"Unable to start loading scene '{sceneName}'. Check Build Settings.");
                isLoading = false;
                yield break;
            }

            while (!operation.isDone)
                yield return null;

            isLoading = false;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator DownloadExhibition()
        {
            SetExhibitionStatus("Checking exhibition download...");

            AsyncOperationHandle<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> initialize =
                Addressables.InitializeAsync(false);
            yield return initialize;
            if (initialize.Status != AsyncOperationStatus.Succeeded)
            {
                ReportDownloadError(initialize.OperationException);
                Addressables.Release(initialize);
                yield break;
            }
            Addressables.Release(initialize);

            string exhibitionAddress = WebExhibitionAddress;
            AsyncOperationHandle<long> size = Addressables.GetDownloadSizeAsync(exhibitionAddress);
            yield return size;
            if (size.Status != AsyncOperationStatus.Succeeded)
            {
                ReportDownloadError(size.OperationException);
                Addressables.Release(size);
                yield break;
            }

            long bytesToDownload = size.Result;
            Addressables.Release(size);
            if (bytesToDownload > 0)
            {
                AsyncOperationHandle download = Addressables.DownloadDependenciesAsync(exhibitionAddress);
                while (!download.IsDone)
                {
                    var progress = download.GetDownloadStatus();
                    int percent = progress.TotalBytes > 0
                        ? Mathf.RoundToInt(100f * progress.DownloadedBytes / progress.TotalBytes)
                        : Mathf.RoundToInt(100f * download.PercentComplete);
                    SetExhibitionStatus($"Downloading exhibition: {percent}%");
                    yield return null;
                }

                if (download.Status != AsyncOperationStatus.Succeeded)
                {
                    ReportDownloadError(download.OperationException);
                    Addressables.Release(download);
                    yield break;
                }
                Addressables.Release(download);
            }

            exhibitionReady = true;
            exhibitionDownload = null;
            SetExhibitionStatus("Exhibition ready");
        }

        private IEnumerator LoadRemoteExhibition()
        {
            PrefetchExhibition();
            while (exhibitionDownload != null)
                yield return null;

            if (!exhibitionReady)
                yield break;

            SetExhibitionStatus("Opening exhibition...");
            AsyncOperationHandle<SceneInstance> scene =
                Addressables.LoadSceneAsync(WebExhibitionAddress, LoadSceneMode.Single);
            while (!scene.IsDone)
                yield return null;
            if (scene.Status != AsyncOperationStatus.Succeeded)
            {
                SetExhibitionStatus("Could not open exhibition. Please try again.");
                Debug.LogError($"Remote exhibition scene failed: {scene.OperationException}");
            }
        }

        private void ReportDownloadError(Exception error)
        {
            exhibitionDownload = null;
            SetExhibitionStatus("Exhibition download failed. Select Visitor to retry.");
            Debug.LogError($"Remote exhibition download failed: {error}");
        }
#endif

        private void SetExhibitionStatus(string status)
        {
            if (ExhibitionStatus == status)
                return;

            ExhibitionStatus = status;
            ExhibitionStatusChanged?.Invoke(status);
        }
    }
}

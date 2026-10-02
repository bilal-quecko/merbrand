using System.Collections;
using MeraBrand.Expo.Authentication;
using MeraBrand.Expo.Core;
using TMPro;
using UnityEngine;

namespace MeraBrand.Expo.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject rolePanel;
        [SerializeField] private GameObject adminLoginPanel;

        [Header("Admin Login")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_Text loginErrorText;

#if UNITY_WEBGL && !UNITY_EDITOR
        private TMP_Text exhibitionDownloadText;
        private SceneLoader exhibitionLoader;
#endif

        private void Start()
        {
            SessionManager.Instance?.ClearSession();
            ShowRoleSelection();
            ApplyPlatformRestrictions();
#if UNITY_WEBGL && !UNITY_EDITOR
            exhibitionLoader = SceneLoader.Instance;
            if (exhibitionLoader == null)
                exhibitionLoader = new GameObject("SceneLoader").AddComponent<SceneLoader>();

            CreateDownloadStatusText();
            exhibitionLoader.ExhibitionStatusChanged += ShowDownloadStatus;
            ShowDownloadStatus(exhibitionLoader.ExhibitionStatus);
            StartCoroutine(PrefetchAfterFirstFrame());
#endif
        }

        private void OnDestroy()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (exhibitionLoader != null)
                exhibitionLoader.ExhibitionStatusChanged -= ShowDownloadStatus;
#endif
        }

        public void ContinueAsVisitor()
        {
            SessionManager.Instance.StartVisitorSession();
            LoadExhibition();
        }

        public void OpenAdminLogin()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#else
            if (rolePanel != null) rolePanel.SetActive(false);
            if (adminLoginPanel != null) adminLoginPanel.SetActive(true);
            if (loginErrorText != null) loginErrorText.text = string.Empty;
            if (usernameInput != null)
            {
                usernameInput.text = string.Empty;
                if (usernameInput.placeholder is TMP_Text placeholder)
                    placeholder.text = "Admin email";
                usernameInput.Select();
            }
            if (passwordInput != null) passwordInput.text = string.Empty;
#endif
        }

        public void CancelAdminLogin() => ShowRoleSelection();

        public void LoginAsAdmin()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#else
            string email = usernameInput != null ? usernameInput.text.Trim() : string.Empty;
            string password = passwordInput != null ? passwordInput.text : string.Empty;
            if (passwordInput != null) passwordInput.text = string.Empty;
            if (loginErrorText != null) loginErrorText.text = "Signing in...";
            if (SessionManager.Instance == null)
            {
                if (loginErrorText != null) loginErrorText.text = "Session unavailable.";
                return;
            }

            SessionManager.Instance.SignInAdmin(email, password, (success, message) =>
            {
                if (loginErrorText != null) loginErrorText.text = message;
                if (success) LoadExhibition();
            });
#endif
        }

        private void ApplyPlatformRestrictions()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (adminLoginPanel != null)
                adminLoginPanel.SetActive(false);

            if (rolePanel != null)
            {
                Transform adminButton = rolePanel.transform.Find("AdminButton");
                if (adminButton != null)
                    adminButton.gameObject.SetActive(false);

                Transform visitorButton = rolePanel.transform.Find("VisitorButton");
                RectTransform visitorRect = visitorButton != null ? visitorButton.GetComponent<RectTransform>() : null;
                if (visitorRect != null)
                    visitorRect.anchoredPosition = Vector2.zero;
            }
#endif
        }

        private void ShowRoleSelection()
        {
            if (rolePanel != null) rolePanel.SetActive(true);
            if (adminLoginPanel != null) adminLoginPanel.SetActive(false);
            if (loginErrorText != null) loginErrorText.text = string.Empty;
        }

        private static void LoadExhibition()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadExhibition();
#if UNITY_WEBGL && !UNITY_EDITOR
            else
                new GameObject("SceneLoader").AddComponent<SceneLoader>().LoadExhibition();
#else
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Exhibition);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator PrefetchAfterFirstFrame()
        {
            yield return null;
            if (exhibitionLoader != null)
                exhibitionLoader.PrefetchExhibition();
        }

        private void CreateDownloadStatusText()
        {
            Canvas canvas = rolePanel != null ? rolePanel.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            var statusObject = new GameObject("Exhibition Download Status", typeof(RectTransform));
            statusObject.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)statusObject.transform;
            rect.anchorMin = new Vector2(0.05f, 0f);
            rect.anchorMax = new Vector2(0.95f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 30f);
            rect.sizeDelta = new Vector2(0f, 50f);

            exhibitionDownloadText = statusObject.AddComponent<TextMeshProUGUI>();
            exhibitionDownloadText.alignment = TextAlignmentOptions.Center;
            exhibitionDownloadText.fontSize = 24f;
            exhibitionDownloadText.color = Color.white;
            exhibitionDownloadText.raycastTarget = false;
        }

        private void ShowDownloadStatus(string status)
        {
            if (exhibitionDownloadText != null)
                exhibitionDownloadText.text = status;
        }
#endif
    }
}

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

        // Development-only credentials. Replace with backend authentication in Phase 7.
        private const string DevUsername = "admin";
        private const string DevPassword = "admin123";

        private void Start()
        {
            SessionManager.Instance?.ClearSession();
            ShowRoleSelection();
            ApplyPlatformRestrictions();
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
            string username = usernameInput != null ? usernameInput.text.Trim() : string.Empty;
            string password = passwordInput != null ? passwordInput.text : string.Empty;

            if (username == DevUsername && password == DevPassword)
            {
                SessionManager.Instance.StartAdminSession(username);
                if (loginErrorText != null) loginErrorText.text = string.Empty;
                LoadExhibition();
                return;
            }

            if (loginErrorText != null)
                loginErrorText.text = "Invalid username or password.";
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
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Exhibition);
        }
    }
}

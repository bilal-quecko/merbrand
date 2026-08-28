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
        }

        public void ContinueAsVisitor()
        {
            SessionManager.Instance.StartVisitorSession();
            LoadExhibition();
        }

        public void OpenAdminLogin()
        {
            if (rolePanel != null) rolePanel.SetActive(false);
            if (adminLoginPanel != null) adminLoginPanel.SetActive(true);
            if (loginErrorText != null) loginErrorText.text = string.Empty;
            if (usernameInput != null)
            {
                usernameInput.text = string.Empty;
                usernameInput.Select();
            }
            if (passwordInput != null) passwordInput.text = string.Empty;
        }

        public void CancelAdminLogin() => ShowRoleSelection();

        public void LoginAsAdmin()
        {
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

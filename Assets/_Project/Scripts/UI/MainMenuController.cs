using System.Collections;
using MeraBrand.Expo.Authentication;
using MeraBrand.Expo.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        [SerializeField] private Toggle rememberMeToggle;
        private bool signingIn;

#if UNITY_WEBGL && !UNITY_EDITOR
        private TMP_Text exhibitionDownloadText;
        private SceneLoader exhibitionLoader;
#endif

        private void Start()
        {
            SessionManager.Instance?.ClearSession(false);
            CreateRememberMeToggle();
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

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame) return;
            MoveLoginFocus(keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        }

        private void MoveLoginFocus(bool backwards)
        {
            if (signingIn || adminLoginPanel == null || !adminLoginPanel.activeInHierarchy)
                return;
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            TMP_InputField next = selected == usernameInput?.gameObject ? passwordInput :
                selected == passwordInput?.gameObject ? usernameInput :
                (backwards ? passwordInput : usernameInput);
            next?.Select();
            next?.ActivateInputField();
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
                usernameInput.text = SessionManager.Instance != null ? SessionManager.Instance.RememberedEmail : string.Empty;
                if (usernameInput.placeholder is TMP_Text placeholder)
                    placeholder.text = "Admin email";
                usernameInput.Select();
            }
            if (passwordInput != null) passwordInput.text = string.Empty;
            SessionManager session = SessionManager.Instance;
            if (rememberMeToggle != null)
                rememberMeToggle.SetIsOnWithoutNotify(session != null && session.HasRememberedAdmin);
            if (session != null && session.HasRememberedAdmin)
            {
                signingIn = true;
                if (loginErrorText != null) loginErrorText.text = "Restoring saved login...";
                session.RestoreRememberedAdmin((success, message) =>
                {
                    if (this == null) return;
                    signingIn = false;
                    if (loginErrorText != null) loginErrorText.text = message;
                    if (success) LoadExhibition();
                    else
                    {
                        rememberMeToggle?.SetIsOnWithoutNotify(session.HasRememberedAdmin);
                        passwordInput?.Select();
                        passwordInput?.ActivateInputField();
                    }
                });
            }
#endif
        }

        public void CancelAdminLogin()
        {
            if (signingIn) SessionManager.Instance?.ClearSession(false);
            signingIn = false;
            ShowRoleSelection();
        }

        public void LoginAsAdmin()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#else
            if (signingIn) return;
            string email = usernameInput != null ? usernameInput.text.Trim() : string.Empty;
            string password = passwordInput != null ? passwordInput.text : string.Empty;
            if (passwordInput != null) passwordInput.text = string.Empty;
            if (loginErrorText != null) loginErrorText.text = "Signing in...";
            if (SessionManager.Instance == null)
            {
                if (loginErrorText != null) loginErrorText.text = "Session unavailable.";
                return;
            }

            signingIn = true;
            SessionManager.Instance.SignInAdmin(email, password, rememberMeToggle != null && rememberMeToggle.isOn, (success, message) =>
            {
                if (this == null) return;
                signingIn = false;
                if (loginErrorText != null) loginErrorText.text = message;
                if (success) LoadExhibition();
            });
#endif
        }

        private void CreateRememberMeToggle()
        {
            EnsureRememberMeToggle();
            if (rememberMeToggle == null) return;
            rememberMeToggle.interactable = SessionManager.Instance != null && SessionManager.Instance.CanRememberAdmin;
            rememberMeToggle.SetIsOnWithoutNotify(SessionManager.Instance != null && SessionManager.Instance.HasRememberedAdmin);
            rememberMeToggle.onValueChanged.AddListener(remember =>
            {
                if (!remember) SessionManager.Instance?.ForgetRememberedAdmin();
            });
        }

        public Toggle EnsureRememberMeToggle()
        {
            if (rememberMeToggle != null) return rememberMeToggle;
            if (adminLoginPanel == null) return null;
            Transform existing = adminLoginPanel.transform.Find("RememberMeToggle");
            if (existing != null)
            {
                rememberMeToggle = existing.GetComponent<Toggle>();
                if (rememberMeToggle != null) return rememberMeToggle;
            }
            var root = new GameObject("RememberMeToggle", typeof(RectTransform), typeof(Toggle));
            root.transform.SetParent(adminLoginPanel.transform, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            RectTransform passwordRect = passwordInput != null ? passwordInput.GetComponent<RectTransform>() : null;
            float rowY = passwordRect != null ? passwordRect.anchoredPosition.y - passwordRect.rect.height * 0.5f - 28f : -60f;
            rect.anchoredPosition = new Vector2(passwordRect != null ? passwordRect.anchoredPosition.x : 0f, rowY);
            rect.sizeDelta = new Vector2(passwordRect != null ? passwordRect.rect.width : 300f, 40f);
            rememberMeToggle = root.GetComponent<Toggle>();

            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(root.transform, false);
            RectTransform boxRect = (RectTransform)box.transform;
            boxRect.anchorMin = boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.anchoredPosition = new Vector2(22f, 0f);
            boxRect.sizeDelta = new Vector2(24f, 24f);
            Image background = box.GetComponent<Image>();
            background.color = new Color(0.85f, 0.85f, 0.85f);

            var tick = new GameObject("Checked", typeof(RectTransform), typeof(Image));
            tick.transform.SetParent(box.transform, false);
            ((RectTransform)tick.transform).sizeDelta = new Vector2(14f, 14f);
            Image checkedImage = tick.GetComponent<Image>();
            checkedImage.color = new Color(0.05f, 0.6f, 0.45f);
            checkedImage.raycastTarget = false;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            if (usernameInput != null && usernameInput.textComponent != null) label.font = usernameInput.textComponent.font;
            label.text = "Remember me";
            label.fontSize = 20f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(44f, 0f);
            labelRect.offsetMax = Vector2.zero;

            // Make the entire row clickable, including its label.
            Image row = root.AddComponent<Image>();
            row.color = new Color(0.04f, 0.07f, 0.1f, 0.95f);
            rememberMeToggle.targetGraphic = background;
            rememberMeToggle.graphic = checkedImage;
            rememberMeToggle.SetIsOnWithoutNotify(false);
            float errorY = rowY - 50f;
            if (loginErrorText != null) loginErrorText.rectTransform.anchoredPosition = new Vector2(0f, errorY);
            float bottom = errorY - 20f;
            foreach (string buttonName in new[] { "LoginButton", "CancelButton" })
            {
                RectTransform button = adminLoginPanel.transform.Find(buttonName) as RectTransform;
                if (button == null) continue;
                Vector2 position = button.anchoredPosition;
                position.y = errorY - 60f;
                button.anchoredPosition = position;
                bottom = Mathf.Min(bottom, position.y - button.rect.height * 0.5f);
            }
            RectTransform panel = adminLoginPanel.GetComponent<RectTransform>();
            if (panel != null)
                panel.sizeDelta = new Vector2(panel.sizeDelta.x, Mathf.Max(panel.sizeDelta.y, -2f * (bottom - 24f)));
            return rememberMeToggle;
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

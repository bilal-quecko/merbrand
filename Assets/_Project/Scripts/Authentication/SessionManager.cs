using UnityEngine;

namespace MeraBrand.Expo.Authentication
{
    public sealed class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        public UserRole CurrentRole { get; private set; } = UserRole.None;
        public bool IsAdminAuthenticated { get; private set; }
        public string CurrentUsername { get; private set; } = string.Empty;

        public bool IsAdmin => CurrentRole == UserRole.Admin && IsAdminAuthenticated;
        public bool IsVisitor => CurrentRole == UserRole.Visitor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;

            GameObject go = new("SessionManager");
            go.AddComponent<SessionManager>();
        }

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

        public void StartVisitorSession()
        {
            CurrentRole = UserRole.Visitor;
            IsAdminAuthenticated = false;
            CurrentUsername = string.Empty;
        }

        public void StartAdminSession(string username)
        {
            CurrentRole = UserRole.Admin;
            IsAdminAuthenticated = true;
            CurrentUsername = username ?? string.Empty;
        }

        public void ClearSession()
        {
            CurrentRole = UserRole.None;
            IsAdminAuthenticated = false;
            CurrentUsername = string.Empty;
        }
    }
}

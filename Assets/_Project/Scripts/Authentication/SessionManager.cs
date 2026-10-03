using System;
using System.Collections;
using System.Text;
using MeraBrand.Expo.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace MeraBrand.Expo.Authentication
{
    public sealed class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        public UserRole CurrentRole { get; private set; } = UserRole.None;
        public bool IsAdminAuthenticated { get; private set; }
        public string CurrentUsername { get; private set; } = string.Empty;

        public bool IsAdmin => CurrentRole == UserRole.Admin && IsAdminAuthenticated && DateTimeOffset.UtcNow < accessTokenExpiresAt;
        public bool IsVisitor => CurrentRole == UserRole.Visitor;
        public string AdminAccessToken => IsAdmin ? accessToken : string.Empty;

        private string accessToken = string.Empty;
        private string refreshToken = string.Empty;
        private DateTimeOffset accessTokenExpiresAt;
        private Coroutine signInRoutine;
        private Coroutine refreshRoutine;
        private DateTimeOffset nextRefreshAttempt;
        private bool rememberAdmin;

        public bool CanRememberAdmin => RememberedAdminStore.Supported;
        public bool HasRememberedAdmin => RememberedAdminStore.TryRead(out _, out _);
        public string RememberedEmail => RememberedAdminStore.TryRead(out string email, out _) ? email : string.Empty;

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

        private void Update()
        {
            if (CurrentRole != UserRole.Admin || string.IsNullOrEmpty(refreshToken) || refreshRoutine != null)
                return;
            if (DateTimeOffset.UtcNow >= accessTokenExpiresAt.AddMinutes(-2) &&
                DateTimeOffset.UtcNow >= nextRefreshAttempt)
                refreshRoutine = StartCoroutine(RefreshSession());
        }

        public void StartVisitorSession()
        {
            ClearSession(false);
            CurrentRole = UserRole.Visitor;
        }

        public void SignInAdmin(string email, string password, Action<bool, string> completed)
            => SignInAdmin(email, password, false, completed);

        public void SignInAdmin(string email, string password, bool remember, Action<bool, string> completed)
        {
            if (signInRoutine != null || refreshRoutine != null)
            {
                completed?.Invoke(false, "Sign-in is already in progress.");
                return;
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                completed?.Invoke(false, "Enter your admin email and password.");
                return;
            }

            rememberAdmin = remember && CanRememberAdmin;
            if (!rememberAdmin) RememberedAdminStore.Delete();
            signInRoutine = StartCoroutine(SignInAdminRequest(email.Trim(), password, completed));
        }

        public void RestoreRememberedAdmin(Action<bool, string> completed)
        {
            ClearSession(false);
            if (!RememberedAdminStore.TryRead(out string email, out string token))
            {
                completed?.Invoke(false, "Enter your admin email and password.");
                return;
            }
            CurrentUsername = email;
            refreshToken = token;
            rememberAdmin = true;
            refreshRoutine = StartCoroutine(RefreshSession(completed));
        }

        public void ForgetRememberedAdmin()
        {
            rememberAdmin = false;
            RememberedAdminStore.Delete();
        }

        public void ClearSession(bool forgetRemembered = true)
        {
            if (signInRoutine != null) { StopCoroutine(signInRoutine); signInRoutine = null; }
            if (refreshRoutine != null) { StopCoroutine(refreshRoutine); refreshRoutine = null; }
            CurrentRole = UserRole.None;
            IsAdminAuthenticated = false;
            CurrentUsername = string.Empty;
            accessToken = string.Empty;
            refreshToken = string.Empty;
            accessTokenExpiresAt = default;
            rememberAdmin = false;
            if (forgetRemembered) RememberedAdminStore.Delete();
        }

        private IEnumerator SignInAdminRequest(string email, string password, Action<bool, string> completed)
        {
            string url = SupabaseStallsClient.ProjectUrl + "/auth/v1/token?grant_type=password";
            string body = JsonUtility.ToJson(new PasswordRequest { email = email, password = password });
            byte[] bytes = Encoding.UTF8.GetBytes(body);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", SupabaseStallsClient.ApiKey);
                request.timeout = 15;
                yield return request.SendWebRequest();

                signInRoutine = null;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completed?.Invoke(false, "Sign-in failed. Check the email, password, and account confirmation.");
                    yield break;
                }

                AuthResponse response;
                try
                {
                    response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                }
                catch (Exception)
                {
                    completed?.Invoke(false, "Could not read the sign-in response.");
                    yield break;
                }

                if (response?.user?.app_metadata?.role != "admin" ||
                    string.IsNullOrEmpty(response.access_token) ||
                    string.IsNullOrEmpty(response.refresh_token) || response.expires_in <= 0)
                {
                    completed?.Invoke(false, "This account does not have admin access.");
                    yield break;
                }

                accessToken = response.access_token;
                refreshToken = response.refresh_token;
                accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(response.expires_in);
                nextRefreshAttempt = accessTokenExpiresAt.AddMinutes(-2);
                CurrentRole = UserRole.Admin;
                IsAdminAuthenticated = true;
                CurrentUsername = response.user.email ?? email;
                SaveRememberedSession();
                completed?.Invoke(true, string.Empty);
            }
        }

        private IEnumerator RefreshSession(Action<bool, string> completed = null)
        {
            string body = JsonUtility.ToJson(new RefreshRequest { refresh_token = refreshToken });
            using (UnityWebRequest request = new UnityWebRequest(
                SupabaseStallsClient.ProjectUrl + "/auth/v1/token?grant_type=refresh_token", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", SupabaseStallsClient.ApiKey);
                request.timeout = 15;
                yield return request.SendWebRequest();
                refreshRoutine = null;
                AuthResponse response = null;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try { response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text); }
                    catch (Exception) { }
                }
                if (response?.user?.app_metadata?.role == "admin" &&
                    !string.IsNullOrEmpty(response.access_token) &&
                    !string.IsNullOrEmpty(response.refresh_token) && response.expires_in > 0)
                {
                    accessToken = response.access_token;
                    refreshToken = response.refresh_token;
                    accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(response.expires_in);
                    nextRefreshAttempt = accessTokenExpiresAt.AddMinutes(-2);
                    CurrentRole = UserRole.Admin;
                    IsAdminAuthenticated = true;
                    CurrentUsername = response.user.email ?? CurrentUsername;
                    SaveRememberedSession();
                    completed?.Invoke(true, string.Empty);
                }
                else
                {
                    bool rejected = request.responseCode == 400 || request.responseCode == 401 ||
                        request.responseCode == 403 || request.result == UnityWebRequest.Result.Success;
                    if (rejected)
                    {
                        ForgetRememberedAdmin();
                        CurrentRole = UserRole.None;
                        IsAdminAuthenticated = false;
                        accessToken = refreshToken = string.Empty;
                    }
                    nextRefreshAttempt = DateTimeOffset.UtcNow.AddSeconds(15);
                    completed?.Invoke(false, rejected
                        ? "Saved login has expired. Please sign in again."
                        : "Could not restore login. Check your connection or sign in manually.");
                    Debug.LogWarning("Supabase admin session refresh failed. Sign in again if the session expires.");
                }
            }
        }

        private void SaveRememberedSession()
        {
            if (rememberAdmin && !RememberedAdminStore.Save(CurrentUsername, refreshToken))
            {
                // Never leave a stale token behind after Supabase rotates it.
                ForgetRememberedAdmin();
                Debug.LogWarning("Admin signed in, but Windows could not save the remembered session.");
            }
        }

        [Serializable]
        private sealed class PasswordRequest
        {
            public string email;
            public string password;
        }

        [Serializable]
        private sealed class AuthResponse
        {
            public string access_token;
            public string refresh_token;
            public int expires_in;
            public AuthUser user;
        }

        [Serializable]
        private sealed class RefreshRequest
        {
            public string refresh_token;
        }

        [Serializable]
        private sealed class AuthUser
        {
            public string email;
            public AppMetadata app_metadata;
        }

        [Serializable]
        private sealed class AppMetadata
        {
            public string role;
        }
    }
}

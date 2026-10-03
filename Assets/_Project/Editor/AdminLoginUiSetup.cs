#if UNITY_EDITOR
using System;
using MeraBrand.Expo.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MeraBrand.Expo.Editor
{
    public static class AdminLoginUiSetup
    {
        [MenuItem("Mera Brand/Desktop/Add Remember Me to Login UI")]
        public static void Configure()
        {
            const string path = "Assets/_Project/Scenes/01_MainMenu.unity";
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool alreadyOpen = scene.IsValid() && scene.isLoaded;
            if (!alreadyOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                MainMenuController controller = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                    controller ??= root.GetComponentInChildren<MainMenuController>(true);
                if (controller == null) throw new InvalidOperationException("Main-menu controller is missing.");
                Toggle toggle = controller.EnsureRememberMeToggle();
                if (toggle == null) throw new InvalidOperationException("Admin login panel is missing.");
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!alreadyOpen || Application.isBatchMode) EditorSceneManager.SaveScene(scene);

                // Verify the authored checkbox, label, references, and spacing in the actual scene.
                var settings = new SerializedObject(controller);
                if (settings.FindProperty("rememberMeToggle").objectReferenceValue != toggle)
                    throw new InvalidOperationException("Remember me reference was not assigned.");
                RectTransform row = toggle.GetComponent<RectTransform>();
                var password = (TMPro.TMP_InputField)settings.FindProperty("passwordInput").objectReferenceValue;
                RectTransform passwordRect = password.GetComponent<RectTransform>();
                if (row.anchoredPosition.y + row.rect.height * 0.5f >= passwordRect.anchoredPosition.y - passwordRect.rect.height * 0.5f)
                    throw new InvalidOperationException("Remember me overlaps the password input.");
                if (!toggle.gameObject.activeSelf || toggle.transform.Find("Label") == null)
                    throw new InvalidOperationException("Remember me UI is hidden or missing its label.");
                Debug.Log("PASS: Remember me checkbox saved and wired below PasswordInput, with no overlap.");
            }
            finally
            {
                if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
#endif

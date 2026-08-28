#if UNITY_EDITOR
using System.IO;
using MeraBrand.Expo.CameraSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeraBrand.Expo.Editor
{
    public static class Phase3NavigationSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/02_Exhibition.unity";

        [MenuItem("Mera Brand/Phase 3/Setup Visitor Flythrough Camera")]
        public static void SetupVisitorFlythroughCamera()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 3", "02_Exhibition scene not found.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Transform camerasRoot = GetOrCreateRoot("Cameras");

            Transform existing = camerasRoot.Find("VisitorFlyCamera");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera camera in cameras)
            {
                if (camera != null && camera.gameObject.scene == scene)
                    Object.DestroyImmediate(camera.gameObject);
            }

            GameObject rig = new("VisitorFlyCamera");
            rig.transform.SetParent(camerasRoot, false);
            rig.transform.position = new Vector3(84f, 8f, 10f);
            rig.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            CharacterController controller = rig.AddComponent<CharacterController>();
            controller.height = 5.5f;
            controller.radius = 1.1f;
            controller.center = new Vector3(0f, 0f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 60f;

            Camera cameraComponent = rig.AddComponent<Camera>();
            cameraComponent.fieldOfView = 65f;
            cameraComponent.nearClipPlane = 0.05f;
            cameraComponent.farClipPlane = 2000f;
            rig.AddComponent<AudioListener>();
            rig.AddComponent<FlyCameraController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = rig;
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 3",
                "Visitor flythrough camera created.\n\nControls:\nWASD = move\nMouse = look\nQ = up\nE = down\nShift = boost\nEsc = release cursor\nLeft Click = recapture cursor",
                "OK");
        }

        private static Transform GetOrCreateRoot(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
                return found.transform;

            return new GameObject(name).transform;
        }
    }
}
#endif

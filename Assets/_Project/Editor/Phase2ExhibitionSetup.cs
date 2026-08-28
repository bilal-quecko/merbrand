#if UNITY_EDITOR
using System.IO;
using MeraBrand.Expo.Stalls;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeraBrand.Expo.Editor
{
    public static class Phase2ExhibitionSetup
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/02_Exhibition.unity";
        private const string StallPrefabPath = Root + "/Prefabs/Stalls";
        private const string MaterialPath = Root + "/Art/Materials";

        // Project convention: 1 Unity unit = 1 foot.
        private const float WallHeight = 10f;
        private const float WallThickness = 0.25f;
        private const float HeaderHeight = 8f;

        [MenuItem("Mera Brand/Phase 2/Generate Stall Prefabs")]
        public static void GenerateStallPrefabs()
        {
            EnsureFolder(StallPrefabPath);
            EnsureFolder(MaterialPath);

            Material availableMaterial = GetOrCreateMaterial(
                MaterialPath + "/MAT_Stall_Available.mat",
                new Color(0.72f, 0.74f, 0.76f));

            CreateStallPrefab("PF_Stall_3x3", new Vector2(3f, 3f), StallSize.ThreeByThree, availableMaterial);
            CreateStallPrefab("PF_Stall_3x6", new Vector2(3f, 6f), StallSize.ThreeBySix, availableMaterial);
            CreateStallPrefab("PF_Stall_6x6", new Vector2(6f, 6f), StallSize.SixBySix, availableMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 2",
                "Standard stall prefabs created: 3x3, 3x6 and 6x6.",
                "OK");
        }

        [MenuItem("Mera Brand/Phase 2/Prepare Exhibition Layout Scene")]
        public static void PrepareExhibitionLayoutScene()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Mera Brand - Phase 2", "02_Exhibition scene was not found. Run Phase 1 first.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Transform architecture = GetOrCreateRoot("Architecture");
            Transform stalls = GetOrCreateRoot("Stalls");
            GetOrCreateRoot("Navigation");
            GetOrCreateRoot("Lighting");
            GetOrCreateRoot("Cameras");
            GetOrCreateRoot("Systems");
            GetOrCreateRoot("UI");

            RemoveChildIfExists(architecture, "Reference_MainFootprint_88x171");
            RemoveChildIfExists(architecture, "Reference_Origin");
            RemoveChildIfExists(stalls, "Stall_Size_Reference");

            // The supplied drawing explicitly gives an 88 ft x 171 ft left-side reference area.
            // This is intentionally only a calibration reference, not a claim that every wall
            // in the complete venue has been dimensioned from the image.
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Reference_MainFootprint_88x171";
            floor.transform.SetParent(architecture, false);
            floor.transform.localPosition = new Vector3(44f, -0.05f, 85.5f);
            floor.transform.localScale = new Vector3(88f, 0.1f, 171f);

            GameObject origin = new("Reference_Origin");
            origin.transform.SetParent(architecture, false);
            origin.transform.localPosition = Vector3.zero;

            GameObject sizeReference = new("Stall_Size_Reference");
            sizeReference.transform.SetParent(stalls, false);
            InstantiateReferenceStall("PF_Stall_3x3", new Vector3(4f, 0f, 4f), "REF_3x3", sizeReference.transform);
            InstantiateReferenceStall("PF_Stall_3x6", new Vector3(10f, 0f, 4f), "REF_3x6", sizeReference.transform);
            InstantiateReferenceStall("PF_Stall_6x6", new Vector3(18f, 0f, 4f), "REF_6x6", sizeReference.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = floor;
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 2",
                "Exhibition scene prepared with the 88 x 171 ft calibration footprint and stall-size references.\n\nNext step: place the complete floor-plan geometry against the supplied layout.",
                "OK");
        }

        private static void CreateStallPrefab(string prefabName, Vector2 footprint, StallSize size, Material material)
        {
            string path = $"{StallPrefabPath}/{prefabName}.prefab";

            GameObject root = new(prefabName);
            root.AddComponent<StallIdentity>();

            CreatePart("Floor", root.transform,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(footprint.x, 0.1f, footprint.y), material);

            CreatePart("BackWall", root.transform,
                new Vector3(0f, WallHeight * 0.5f, footprint.y * 0.5f),
                new Vector3(footprint.x, WallHeight, WallThickness), material);

            CreatePart("LeftWall", root.transform,
                new Vector3(-footprint.x * 0.5f, WallHeight * 0.5f, 0f),
                new Vector3(WallThickness, WallHeight, footprint.y), material);

            CreatePart("RightWall", root.transform,
                new Vector3(footprint.x * 0.5f, WallHeight * 0.5f, 0f),
                new Vector3(WallThickness, WallHeight, footprint.y), material);

            GameObject headerAnchor = new("HeaderAnchor");
            headerAnchor.transform.SetParent(root.transform, false);
            headerAnchor.transform.localPosition = new Vector3(0f, HeaderHeight, footprint.y * 0.5f - 0.02f);

            GameObject visitPoint = new("VisitPoint");
            visitPoint.transform.SetParent(root.transform, false);
            visitPoint.transform.localPosition = new Vector3(0f, 5.5f, -Mathf.Max(5f, footprint.y * 0.9f));
            visitPoint.transform.localRotation = Quaternion.identity;

            GameObject lookTarget = new("LookTarget");
            lookTarget.transform.SetParent(root.transform, false);
            lookTarget.transform.localPosition = new Vector3(0f, 4.5f, 0f);

            StallIdentity identity = root.GetComponent<StallIdentity>();
            identity.EditorConfigure("UNASSIGNED", prefabName, "UNASSIGNED", size, footprint);
            identity.EditorSetCameraAnchors(visitPoint.transform, lookTarget.transform);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreatePart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        private static void InstantiateReferenceStall(string prefabName, Vector3 position, string id, Transform parent)
        {
            string path = $"{StallPrefabPath}/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"Missing {prefabName}. Run 'Generate Stall Prefabs' first.");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = id;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Transform GetOrCreateRoot(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
                return found.transform;

            return new GameObject(name).transform;
        }

        private static void RemoveChildIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif

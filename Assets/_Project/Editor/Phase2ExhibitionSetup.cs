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

        // Venue/layout drawings are in feet, therefore 1 Unity unit = 1 foot.
        // Stall specifications are provided in metres and are converted precisely to feet here.
        private const float FeetPerMeter = 3.280839895f;
        private const float WallHeightMeters = 2.5f;
        private const float HeaderHeightMeters = 2.2f;
        private const float WallThicknessMeters = 0.08f;

        private static float MetersToUnits(float meters) => meters * FeetPerMeter;

        private static readonly float WallHeight = MetersToUnits(WallHeightMeters);       // 8.20210 ft
        private static readonly float HeaderHeight = MetersToUnits(HeaderHeightMeters);   // 7.21785 ft
        private static readonly float WallThickness = MetersToUnits(WallThicknessMeters); // 0.26247 ft

        [MenuItem("Mera Brand/Phase 2/Generate Stall Prefabs")]
        public static void GenerateStallPrefabs()
        {
            EnsureFolder(StallPrefabPath);
            EnsureFolder(MaterialPath);

            Material availableMaterial = GetOrCreateMaterial(
                MaterialPath + "/MAT_Stall_Available.mat",
                new Color(0.72f, 0.74f, 0.76f));

            // IMPORTANT:
            // Names are metric dimensions. Unity footprint is converted to feet because the
            // complete venue layout is authored at 1 Unity unit = 1 foot.
            // X = stall frontage, Z = stall side/depth.
            // 3x6 means 6 m frontage and 3 m side/depth, per exhibition specification.
            CreateStallPrefab(
                "PF_Stall_3x3",
                new Vector2(MetersToUnits(3f), MetersToUnits(3f)),
                new Vector2(3f, 3f),
                StallSize.ThreeByThree,
                availableMaterial);

            CreateStallPrefab(
                "PF_Stall_3x6",
                new Vector2(MetersToUnits(6f), MetersToUnits(3f)),
                new Vector2(6f, 3f),
                StallSize.ThreeBySix,
                availableMaterial);

            CreateStallPrefab(
                "PF_Stall_6x6",
                new Vector2(MetersToUnits(6f), MetersToUnits(6f)),
                new Vector2(6f, 6f),
                StallSize.SixBySix,
                availableMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 2",
                "Standard stall prefabs regenerated with metric specifications converted to the feet-based Unity layout.\n\n" +
                "3x3 m = 9.84252 x 9.84252 ft\n" +
                "3x6 m = 19.68504 ft FRONT x 9.84252 ft SIDE\n" +
                "6x6 m = 19.68504 x 19.68504 ft\n" +
                "Stall wall height = 2.5 m = 8.20210 ft",
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

            // Reference stalls are spaced using their actual converted dimensions.
            InstantiateReferenceStall("PF_Stall_3x3", new Vector3(6f, 0f, 6f), "REF_3x3", sizeReference.transform);
            InstantiateReferenceStall("PF_Stall_3x6", new Vector3(24f, 0f, 6f), "REF_3x6", sizeReference.transform);
            InstantiateReferenceStall("PF_Stall_6x6", new Vector3(48f, 0f, 10f), "REF_6x6", sizeReference.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = floor;
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 2",
                "Exhibition scene prepared with the 88 x 171 ft calibration footprint and correctly converted metric stall references.\n\nNext step: place the complete floor-plan geometry against the supplied layout.",
                "OK");
        }

        private static void CreateStallPrefab(
            string prefabName,
            Vector2 footprintInUnityUnits,
            Vector2 footprintMeters,
            StallSize size,
            Material material)
        {
            string path = $"{StallPrefabPath}/{prefabName}.prefab";

            GameObject root = new(prefabName);
            root.AddComponent<StallIdentity>();

            CreatePart("Floor", root.transform,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(footprintInUnityUnits.x, 0.1f, footprintInUnityUnits.y), material);

            CreatePart("BackWall", root.transform,
                new Vector3(0f, WallHeight * 0.5f, footprintInUnityUnits.y * 0.5f),
                new Vector3(footprintInUnityUnits.x, WallHeight, WallThickness), material);

            CreatePart("LeftWall", root.transform,
                new Vector3(-footprintInUnityUnits.x * 0.5f, WallHeight * 0.5f, 0f),
                new Vector3(WallThickness, WallHeight, footprintInUnityUnits.y), material);

            CreatePart("RightWall", root.transform,
                new Vector3(footprintInUnityUnits.x * 0.5f, WallHeight * 0.5f, 0f),
                new Vector3(WallThickness, WallHeight, footprintInUnityUnits.y), material);

            GameObject headerAnchor = new("HeaderAnchor");
            headerAnchor.transform.SetParent(root.transform, false);
            headerAnchor.transform.localPosition = new Vector3(
                0f,
                HeaderHeight,
                footprintInUnityUnits.y * 0.5f - 0.02f);

            GameObject visitPoint = new("VisitPoint");
            visitPoint.transform.SetParent(root.transform, false);
            visitPoint.transform.localPosition = new Vector3(
                0f,
                MetersToUnits(1.7f),
                -Mathf.Max(MetersToUnits(2f), footprintInUnityUnits.y * 0.75f));
            visitPoint.transform.localRotation = Quaternion.identity;

            GameObject lookTarget = new("LookTarget");
            lookTarget.transform.SetParent(root.transform, false);
            lookTarget.transform.localPosition = new Vector3(0f, MetersToUnits(1.5f), 0f);

            StallIdentity identity = root.GetComponent<StallIdentity>();
            identity.EditorConfigure(
                "UNASSIGNED",
                prefabName,
                "UNASSIGNED",
                size,
                footprintMeters,
                footprintInUnityUnits,
                WallHeightMeters);
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

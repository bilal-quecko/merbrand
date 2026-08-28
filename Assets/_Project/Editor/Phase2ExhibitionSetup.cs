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

        // AUTHORITATIVE SCALE RULES
        // 1 Unity unit = 1 foot for venue/layout geometry.
        // Stall specifications are provided in metres and converted to feet.
        private const float FeetPerMeter = 3.280839895f;

        // AUTHORITATIVE HALL DIMENSIONS (A x B)
        // A = width (Unity X axis), B = height/depth on plan (Unity Z axis).
        // Hall 1 = lower-left hall.
        // Hall 2 = large right-side hall, including the E2 area.
        // Hall 3 = upper-left smaller hall.
        private static readonly Vector2 Hall1Feet = new(168f, 115f);
        private static readonly Vector2 Hall2Feet = new(82f, 198f);
        private static readonly Vector2 Hall3Feet = new(98f, 78f);

        private const float StallWallHeightMeters = 2.5f;
        private const float HeaderHeightMeters = 2.2f;
        private const float StallWallThicknessMeters = 0.08f;

        private static float MetersToUnits(float meters) => meters * FeetPerMeter;

        private static readonly float StallWallHeight = MetersToUnits(StallWallHeightMeters);
        private static readonly float HeaderHeight = MetersToUnits(HeaderHeightMeters);
        private static readonly float StallWallThickness = MetersToUnits(StallWallThicknessMeters);

        [MenuItem("Mera Brand/Phase 2/Generate Stall Prefabs")]
        public static void GenerateStallPrefabs()
        {
            EnsureFolder(StallPrefabPath);
            EnsureFolder(MaterialPath);

            Material availableMaterial = GetOrCreateMaterial(
                MaterialPath + "/MAT_Stall_Available.mat",
                new Color(0.72f, 0.74f, 0.76f));

            // X = frontage, Z = side/depth.
            CreateStallPrefab(
                "PF_Stall_3x3",
                new Vector2(MetersToUnits(3f), MetersToUnits(3f)),
                new Vector2(3f, 3f),
                StallSize.ThreeByThree,
                availableMaterial);

            // 3x6 specification: 6 m frontage x 3 m side/depth.
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
                "Standard stall prefabs regenerated.\n\n" +
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

            // Remove obsolete calibration/reference objects from the earlier interpretation.
            RemoveChildIfExists(architecture, "Reference_MainFootprint_88x171");
            RemoveChildIfExists(architecture, "Reference_Origin");
            RemoveChildIfExists(architecture, "Hall_Dimension_References");
            RemoveChildIfExists(stalls, "Stall_Size_Reference");

            Material hallMaterial = GetOrCreateMaterial(
                MaterialPath + "/MAT_Hall_Reference.mat",
                new Color(0.56f, 0.53f, 0.47f));

            GameObject hallReferences = new("Hall_Dimension_References");
            hallReferences.transform.SetParent(architecture, false);

            // These three footprint objects use the exact hall dimensions supplied by the client.
            // They are intentionally separated in the editor as dimension references only.
            // Final relative placement will follow the supplied plan once corridor/connection offsets are locked.
            CreateHallReference("Hall_1_168x115_ft", Hall1Feet, new Vector3(0f, 0f, 0f), hallReferences.transform, hallMaterial);
            CreateHallReference("Hall_2_82x198_ft", Hall2Feet, new Vector3(190f, 0f, 0f), hallReferences.transform, hallMaterial);
            CreateHallReference("Hall_3_98x78_ft", Hall3Feet, new Vector3(0f, 0f, 140f), hallReferences.transform, hallMaterial);

            GameObject sizeReference = new("Stall_Size_Reference");
            sizeReference.transform.SetParent(stalls, false);

            InstantiateReferenceStall("PF_Stall_3x3", new Vector3(6f, 0f, -20f), "REF_3x3", sizeReference.transform);
            InstantiateReferenceStall("PF_Stall_3x6", new Vector3(24f, 0f, -20f), "REF_3x6", sizeReference.transform);
            InstantiateReferenceStall("PF_Stall_6x6", new Vector3(52f, 0f, -20f), "REF_6x6", sizeReference.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = hallReferences;
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 2",
                "Hall dimension references updated to the authoritative sizes:\n\n" +
                "Hall 1 (lower-left): 168 x 115 ft\n" +
                "Hall 2 (right, includes E2): 82 x 198 ft\n" +
                "Hall 3 (upper-left): 98 x 78 ft\n\n" +
                "A = width/X, B = plan height/Z.\n\n" +
                "The old 88 x 171 master footprint is no longer used.",
                "OK");
        }

        private static void CreateHallReference(
            string name,
            Vector2 sizeFeet,
            Vector3 origin,
            Transform parent,
            Material material)
        {
            GameObject hall = new(name);
            hall.transform.SetParent(parent, false);
            hall.transform.localPosition = origin;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(hall.transform, false);
            floor.transform.localPosition = new Vector3(sizeFeet.x * 0.5f, -0.05f, sizeFeet.y * 0.5f);
            floor.transform.localScale = new Vector3(sizeFeet.x, 0.1f, sizeFeet.y);

            Renderer renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;

            GameObject originMarker = new("Origin_A0_B0");
            originMarker.transform.SetParent(hall.transform, false);
            originMarker.transform.localPosition = Vector3.zero;
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
                new Vector3(0f, StallWallHeight * 0.5f, footprintInUnityUnits.y * 0.5f),
                new Vector3(footprintInUnityUnits.x, StallWallHeight, StallWallThickness), material);

            CreatePart("LeftWall", root.transform,
                new Vector3(-footprintInUnityUnits.x * 0.5f, StallWallHeight * 0.5f, 0f),
                new Vector3(StallWallThickness, StallWallHeight, footprintInUnityUnits.y), material);

            CreatePart("RightWall", root.transform,
                new Vector3(footprintInUnityUnits.x * 0.5f, StallWallHeight * 0.5f, 0f),
                new Vector3(StallWallThickness, StallWallHeight, footprintInUnityUnits.y), material);

            GameObject headerAnchor = new("HeaderAnchor");
            headerAnchor.transform.SetParent(root.transform, false);
            headerAnchor.transform.localPosition = new Vector3(0f, HeaderHeight, footprintInUnityUnits.y * 0.5f - 0.02f);

            GameObject visitPoint = new("VisitPoint");
            visitPoint.transform.SetParent(root.transform, false);
            visitPoint.transform.localPosition = new Vector3(
                0f,
                MetersToUnits(1.7f),
                -Mathf.Max(MetersToUnits(2f), footprintInUnityUnits.y * 0.75f));

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
                StallWallHeightMeters);
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

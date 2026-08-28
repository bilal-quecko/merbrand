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
        // A = width (Unity X axis), B = plan height/depth (Unity Z axis).
        // Hall 1 = lower-left hall.
        // Hall 2 = full right-side hall including E2.
        // Hall 3 = upper-left smaller hall.
        private static readonly Vector2 Hall1Feet = new(168f, 115f);
        private static readonly Vector2 Hall2Feet = new(82f, 198f);
        private static readonly Vector2 Hall3Feet = new(98f, 78f);

        // STRUCTURAL PLACEMENT
        // Hall 1 is the layout origin.
        // Hall 3 sits directly above Hall 1, matching the supplied plan.
        // Hall 2 sits to the right of Hall 1. The plan explicitly shows a 20 ft
        // connecting span at this level, therefore that 20 ft is used as the current
        // authoritative horizontal separation until a CAD plan says otherwise.
        private const float Hall2GapFromHall1Feet = 20f;
        private static readonly Vector3 Hall1Origin = Vector3.zero;
        private static readonly Vector3 Hall3Origin = new(0f, 0f, Hall1Feet.y);
        private static readonly Vector3 Hall2Origin = new(Hall1Feet.x + Hall2GapFromHall1Feet, 0f, 0f);

        private const float StructuralWallHeightFeet = 10f;
        private const float StructuralWallThicknessFeet = 0.5f;
        private const float FloorThicknessFeet = 0.15f;

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

            RemoveChildIfExists(architecture, "Reference_MainFootprint_88x171");
            RemoveChildIfExists(architecture, "Reference_Origin");
            RemoveChildIfExists(architecture, "Hall_Dimension_References");
            RemoveChildIfExists(architecture, "Structural_Halls");
            RemoveChildIfExists(stalls, "Stall_Size_Reference");

            Material hallMaterial = GetOrCreateMaterial(
                MaterialPath + "/MAT_Hall_Floor.mat",
                new Color(0.56f, 0.53f, 0.47f));

            Material wallMaterial = GetOrCreateMaterial(
                MaterialPath + "/MAT_Hall_Wall.mat",
                new Color(0.80f, 0.80f, 0.78f));

            GameObject structuralRoot = new("Structural_Halls");
            structuralRoot.transform.SetParent(architecture, false);

            CreateStructuralHall("Hall_1_LowerLeft_168x115", Hall1Feet, Hall1Origin, structuralRoot.transform, hallMaterial, wallMaterial);
            CreateStructuralHall("Hall_2_Right_82x198_Including_E2", Hall2Feet, Hall2Origin, structuralRoot.transform, hallMaterial, wallMaterial);
            CreateStructuralHall("Hall_3_UpperLeft_98x78", Hall3Feet, Hall3Origin, structuralRoot.transform, hallMaterial, wallMaterial);

            CreateConnectorGuide(structuralRoot.transform, wallMaterial);

            GameObject sizeReference = new("Stall_Size_Reference");
            sizeReference.transform.SetParent(stalls, false);
            InstantiateReferenceStall("PF_Stall_3x3", new Vector3(6f, 0f, -20f), "REF_3x3", sizeReference.transform);
            InstantiateReferenceStall("PF_Stall_3x6", new Vector3(24f, 0f, -20f), "REF_3x6", sizeReference.transform);
            InstantiateReferenceStall("PF_Stall_6x6", new Vector3(52f, 0f, -20f), "REF_6x6", sizeReference.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = structuralRoot;
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorUtility.DisplayDialog(
                "Mera Brand - Phase 2",
                "Three-hall structural layout generated.\n\n" +
                "Hall 1: 168 x 115 ft (lower-left)\n" +
                "Hall 2: 82 x 198 ft (right side, including E2)\n" +
                "Hall 3: 98 x 78 ft (upper-left)\n\n" +
                "Hall 3 is stacked directly above Hall 1.\n" +
                "Hall 2 uses the 20 ft connection shown on the supplied plan.\n\n" +
                "Next: internal zones and stall placement.",
                "OK");
        }

        private static void CreateStructuralHall(
            string name,
            Vector2 sizeFeet,
            Vector3 origin,
            Transform parent,
            Material floorMaterial,
            Material wallMaterial)
        {
            GameObject hall = new(name);
            hall.transform.SetParent(parent, false);
            hall.transform.localPosition = origin;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(hall.transform, false);
            floor.transform.localPosition = new Vector3(sizeFeet.x * 0.5f, -FloorThicknessFeet * 0.5f, sizeFeet.y * 0.5f);
            floor.transform.localScale = new Vector3(sizeFeet.x, FloorThicknessFeet, sizeFeet.y);
            SetMaterial(floor, floorMaterial);

            Transform walls = new GameObject("Boundary_Walls").transform;
            walls.SetParent(hall.transform, false);

            CreateStructuralWall("South", walls, new Vector3(sizeFeet.x * 0.5f, StructuralWallHeightFeet * 0.5f, 0f), new Vector3(sizeFeet.x, StructuralWallHeightFeet, StructuralWallThicknessFeet), wallMaterial);
            CreateStructuralWall("North", walls, new Vector3(sizeFeet.x * 0.5f, StructuralWallHeightFeet * 0.5f, sizeFeet.y), new Vector3(sizeFeet.x, StructuralWallHeightFeet, StructuralWallThicknessFeet), wallMaterial);
            CreateStructuralWall("West", walls, new Vector3(0f, StructuralWallHeightFeet * 0.5f, sizeFeet.y * 0.5f), new Vector3(StructuralWallThicknessFeet, StructuralWallHeightFeet, sizeFeet.y), wallMaterial);
            CreateStructuralWall("East", walls, new Vector3(sizeFeet.x, StructuralWallHeightFeet * 0.5f, sizeFeet.y * 0.5f), new Vector3(StructuralWallThicknessFeet, StructuralWallHeightFeet, sizeFeet.y), wallMaterial);

            GameObject metadata = new("Dimensions_AxB");
            metadata.transform.SetParent(hall.transform, false);
            metadata.transform.localPosition = Vector3.zero;
        }

        private static void CreateConnectorGuide(Transform parent, Material material)
        {
            // The visible plan marks a 20 ft connection between the left complex and Hall 2.
            // This guide is deliberately a floor/placement guide, not a finalized corridor wall system.
            GameObject connector = GameObject.CreatePrimitive(PrimitiveType.Cube);
            connector.name = "Hall1_to_Hall2_20ft_Connection_Guide";
            connector.transform.SetParent(parent, false);
            connector.transform.localPosition = new Vector3(
                Hall1Feet.x + Hall2GapFromHall1Feet * 0.5f,
                -FloorThicknessFeet * 0.5f,
                Hall1Feet.y * 0.78f);
            connector.transform.localScale = new Vector3(Hall2GapFromHall1Feet, FloorThicknessFeet, 18f);
            SetMaterial(connector, material);
        }

        private static void CreateStructuralWall(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            SetMaterial(wall, material);
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
            visitPoint.transform.localPosition = new Vector3(0f, MetersToUnits(1.7f), -Mathf.Max(MetersToUnits(2f), footprintInUnityUnits.y * 0.75f));

            GameObject lookTarget = new("LookTarget");
            lookTarget.transform.SetParent(root.transform, false);
            lookTarget.transform.localPosition = new Vector3(0f, MetersToUnits(1.5f), 0f);

            StallIdentity identity = root.GetComponent<StallIdentity>();
            identity.EditorConfigure("UNASSIGNED", prefabName, "UNASSIGNED", size, footprintMeters, footprintInUnityUnits, StallWallHeightMeters);
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
            SetMaterial(part, material);
        }

        private static void SetMaterial(GameObject gameObject, Material material)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
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

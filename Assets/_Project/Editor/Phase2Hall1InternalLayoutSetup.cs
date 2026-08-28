#if UNITY_EDITOR
using System.IO;
using MeraBrand.Expo.Stalls;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeraBrand.Expo.Editor
{
    /// <summary>
    /// Builds Hall 1 from the supplied reference plan.
    /// Venue coordinates are feet: 1 Unity unit = 1 foot.
    /// Stall specifications are metric and the prefabs are already converted to feet.
    ///
    /// Locked Hall 1 rules:
    /// - Hall 1 = 168 x 115 ft.
    /// - Normal numbered booths = 3 x 3 m.
    /// - Silver S booths (S-1, S-2, ... S-24) = 6 m frontage x 3 m depth.
    /// - Gold booths = 6 x 6 m.
    /// - Placement follows the Hall 1 reference image proportions, not a generic grid.
    /// </summary>
    public static class Phase2Hall1InternalLayoutSetup
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/02_Exhibition.unity";
        private const string StallPrefabPath = Root + "/Prefabs/Stalls";

        private static readonly Vector2 Hall1Feet = new(168f, 115f);

        // Converted dimensions used for plan spacing.
        private const float Booth3mFeet = 9.84252f;
        private const float Booth6mFeet = 19.68504f;
        private const float SilverColumnPitch = 21.0f;
        private const float SilverRowPitch = 10.5f;

        [MenuItem("Mera Brand/Phase 2/Build Hall 1 Internal Layout")]
        public static void BuildHall1InternalLayout()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Mera Brand - Hall 1", "02_Exhibition scene not found. Run Phase 1/Phase 2 setup first.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject stallsRootObject = GameObject.Find("Stalls");
            if (stallsRootObject == null)
            {
                EditorUtility.DisplayDialog("Mera Brand - Hall 1", "Stalls root not found. Run 'Prepare Exhibition Layout Scene' first.", "OK");
                return;
            }

            Transform stallsRoot = stallsRootObject.transform;
            RemoveChildIfExists(stallsRoot, "Hall_1_Stalls");

            Transform hallRoot = new GameObject("Hall_1_Stalls").transform;
            hallRoot.SetParent(stallsRoot, false);

            Transform normal = CreateGroup("Normal_3x3m", hallRoot);
            Transform silver = CreateGroup("Silver_6x3m", hallRoot);
            Transform special = CreateGroup("SL_And_Special", hallRoot);
            Transform gold = CreateGroup("Gold_6x6m", hallRoot);
            Transform sponsors = CreateGroup("Main_Expo_Co_Sponsors", hallRoot);

            // -----------------------------------------------------------------
            // WEST PERIMETER — NORMAL 3x3 m STALLS 1-5
            // -----------------------------------------------------------------
            // The reference has five compact booths vertically stacked against the west wall.
            // Their centers are kept near the wall with clear gaps matching the drawing.
            float[] westZ = { 11f, 25.5f, 40f, 54.5f, 69f };
            for (int i = 0; i < westZ.Length; i++)
            {
                string label = (i + 1).ToString();
                PlaceStall("PF_Stall_3x3", $"H1-{label}", label, "Hall 1",
                    new Vector3(5.4f, 0f, westZ[i]), 90f, normal);
            }

            // -----------------------------------------------------------------
            // SILVER ISLANDS — EACH S-BOOTH IS 6x3 m
            // -----------------------------------------------------------------
            // Reference image arrangement:
            // upper-left:  S20 S21 S22 / S11 S12 S13
            // upper-right: S23 S24     / S14 S15
            // lower-left:  S10 S9 S8   / S1  S2  S3
            // lower-right: S9  S10     / S4  S5
            //
            // X is frontage (6 m = 19.685 ft), Z is booth depth (3 m = 9.843 ft).
            PlaceSilverRow(new[] { "S-1", "S-2", "S-3" }, 35f, 45.5f, 0f, silver, "H1-SIL-LA-L");
            PlaceSilverRow(new[] { "S-10", "S-9", "S-8" }, 35f, 56f, 180f, silver, "H1-SIL-LA-U");

            PlaceSilverRow(new[] { "S-4", "S-5" }, 108f, 45.5f, 0f, silver, "H1-SIL-LB-L");
            PlaceSilverRow(new[] { "S-9", "S-10" }, 108f, 56f, 180f, silver, "H1-SIL-LB-U");

            PlaceSilverRow(new[] { "S-11", "S-12", "S-13" }, 35f, 83.5f, 0f, silver, "H1-SIL-UA-L");
            PlaceSilverRow(new[] { "S-20", "S-21", "S-22" }, 35f, 94f, 180f, silver, "H1-SIL-UA-U");

            PlaceSilverRow(new[] { "S-14", "S-15" }, 108f, 83.5f, 0f, silver, "H1-SIL-UB-L");
            PlaceSilverRow(new[] { "S-23", "S-24" }, 108f, 94f, 180f, silver, "H1-SIL-UB-U");

            // -----------------------------------------------------------------
            // SL STALLS — kept as 6x3 m special modules from the plan.
            // -----------------------------------------------------------------
            PlaceStall("PF_Stall_3x6", "H1-SL1", "SL 1", "Hall 1", new Vector3(6.2f, 0f, 91f), 90f, special);
            PlaceStall("PF_Stall_3x6", "H1-SL2", "SL 2", "Hall 1", new Vector3(144f, 0f, 89f), 90f, special);
            PlaceStall("PF_Stall_3x6", "H1-SL3", "SL 3", "Hall 1", new Vector3(144f, 0f, 51f), 90f, special);
            PlaceStall("PF_Stall_3x6", "H1-SL4", "SL 4", "Hall 1", new Vector3(157f, 0f, 11f), 0f, special);

            // -----------------------------------------------------------------
            // GOLD ROW — GOLD STALLS ARE 6x6 m
            // -----------------------------------------------------------------
            PlaceStall("PF_Stall_6x6", "H1-GOLD1", "GOLD 1", "Hall 1", new Vector3(36f, 0f, 16f), 0f, gold);
            PlaceStall("PF_Stall_6x6", "H1-GOLD2", "GOLD 2", "Hall 1", new Vector3(57f, 0f, 16f), 0f, gold);
            PlaceStall("PF_Stall_6x6", "H1-GOLD-SPONSOR", "Gold Sponsor", "Hall 1", new Vector3(78f, 0f, 16f), 0f, gold);

            // Main and Expo Sponsor blocks are shown with roughly 6 m frontage in the reference.
            // They remain 6x3 modular placeholders until their final custom models are supplied.
            PlaceStall("PF_Stall_3x6", "H1-MAIN-SPONSOR", "Main Sponsor", "Hall 1", new Vector3(108f, 0f, 16f), 0f, sponsors);
            PlaceStall("PF_Stall_3x6", "H1-EXPO-SPONSOR", "Expo Sponsor", "Hall 1", new Vector3(130f, 0f, 16f), 0f, sponsors);
            PlaceStall("PF_Stall_3x3", "H1-CO-SPONSOR", "Co Sponsor", "Hall 1", new Vector3(162.5f, 0f, 12f), 90f, sponsors);

            // Hall label sits in the open center aisle between upper and lower island groups.
            CreateHallLabel(hallRoot, "HALL 1", new Vector3(84f, 0.2f, 69f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = hallRoot.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorUtility.DisplayDialog(
                "Mera Brand - Hall 1",
                "Hall 1 corrected to the reference layout.\n\n" +
                "Locked sizes:\n" +
                "• Normal numbered stalls = 3x3 m\n" +
                "• Silver S stalls = 6x3 m (6 m frontage)\n" +
                "• Gold stalls = 6x6 m\n\n" +
                "The four silver islands, west perimeter stalls, SL positions and sponsor row were compacted/repositioned to match the supplied plan much more closely.",
                "OK");
        }

        private static void PlaceSilverRow(string[] labels, float startX, float z, float yRotation, Transform parent, string internalPrefix)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                PlaceStall(
                    "PF_Stall_3x6",
                    $"{internalPrefix}-{i + 1}",
                    labels[i],
                    "Hall 1",
                    new Vector3(startX + i * SilverColumnPitch, 0f, z),
                    yRotation,
                    parent);
            }
        }

        private static GameObject PlaceStall(
            string prefabName,
            string internalId,
            string displayLabel,
            string hall,
            Vector3 localPosition,
            float yRotation,
            Transform parent)
        {
            string path = $"{StallPrefabPath}/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"Missing stall prefab: {path}. Run 'Generate Stall Prefabs' first.");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = internalId;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

            StallIdentity identity = instance.GetComponent<StallIdentity>();
            if (identity != null)
            {
                Vector2 metres;
                Vector2 units;
                StallSize size;

                if (prefabName.Contains("6x6"))
                {
                    metres = new Vector2(6f, 6f);
                    units = new Vector2(Booth6mFeet, Booth6mFeet);
                    size = StallSize.SixBySix;
                }
                else if (prefabName.Contains("3x6"))
                {
                    metres = new Vector2(6f, 3f);
                    units = new Vector2(Booth6mFeet, Booth3mFeet);
                    size = StallSize.ThreeBySix;
                }
                else
                {
                    metres = new Vector2(3f, 3f);
                    units = new Vector2(Booth3mFeet, Booth3mFeet);
                    size = StallSize.ThreeByThree;
                }

                identity.EditorConfigure(internalId, displayLabel, hall, size, metres, units, 2.5f);
            }

            CreateStallLabel(instance.transform, displayLabel, prefabName.Contains("6x6") ? 15f : prefabName.Contains("3x6") ? 13f : 8f);
            return instance;
        }

        private static void CreateStallLabel(Transform stall, string label, float width)
        {
            Transform existing = stall.Find("PlanLabel_TMP");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject textObject = new("PlanLabel_TMP");
            textObject.transform.SetParent(stall, false);
            textObject.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            textObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            TextMeshPro tmp = textObject.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 2.2f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 1.2f;
            tmp.fontSizeMax = 2.4f;
            tmp.rectTransform.sizeDelta = new Vector2(width, 4f);
        }

        private static void CreateHallLabel(Transform parent, string label, Vector3 position)
        {
            GameObject textObject = new("Hall_Label_TMP");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position;
            textObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            TextMeshPro tmp = textObject.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 7f;
            tmp.rectTransform.sizeDelta = new Vector2(35f, 10f);
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static void RemoveChildIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }
    }
}
#endif

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
    /// Generates the first-pass Hall 1 internal layout from the supplied expo plan.
    /// Venue coordinates are feet (1 Unity unit = 1 foot).
    /// Stall prefab names remain metric and are already converted to feet by Phase 2 setup.
    ///
    /// Important: Hall dimensions are authoritative. Internal placement is a plan-matched working
    /// layout that can be fine-tuned once additional measured offsets are supplied.
    /// </summary>
    public static class Phase2Hall1InternalLayoutSetup
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/02_Exhibition.unity";
        private const string StallPrefabPath = Root + "/Prefabs/Stalls";

        // Hall 1 authoritative size: 168 x 115 ft.
        private static readonly Vector2 Hall1Feet = new(168f, 115f);

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

            Transform standard = CreateGroup("Standard_Stalls", hallRoot);
            Transform islandA = CreateGroup("Island_A_S1_to_S10", hallRoot);
            Transform islandB = CreateGroup("Island_B_S4_to_S10", hallRoot);
            Transform islandC = CreateGroup("Island_C_S11_to_S22", hallRoot);
            Transform islandD = CreateGroup("Island_D_S14_to_S24", hallRoot);
            Transform special = CreateGroup("Special_And_Sponsor", hallRoot);

            // ---------------------------
            // WEST PERIMETER: 1 - 5
            // ---------------------------
            // Fronts face inward/east. The supplied plan shows these vertically stacked on Hall 1's west wall.
            for (int i = 0; i < 5; i++)
            {
                string label = (i + 1).ToString();
                PlaceStall("PF_Stall_3x3", $"H1-{label}", label, "Hall 1",
                    new Vector3(5.2f, 0f, 17f + i * 12f), 90f, standard);
            }

            // ---------------------------
            // CENTRAL LOWER ISLAND
            // Plan display labels: upper S-10, S-9, S-8 / lower S-1, S-2, S-3
            // ---------------------------
            PlaceHorizontalRow(new[] { "S-1", "S-2", "S-3" }, 47f, 35f, 0f, islandA, "H1-A-L");
            PlaceHorizontalRow(new[] { "S-10", "S-9", "S-8" }, 47f, 47f, 180f, islandA, "H1-A-U");

            // ---------------------------
            // CENTRAL LOWER-RIGHT ISLAND
            // The drawing repeats display labels S-9 and S-10 here. Internal IDs remain unique.
            // ---------------------------
            PlaceHorizontalRow(new[] { "S-4", "S-5" }, 112f, 35f, 0f, islandB, "H1-B-L");
            PlaceHorizontalRow(new[] { "S-9", "S-10" }, 112f, 47f, 180f, islandB, "H1-B-U");

            // ---------------------------
            // CENTRAL UPPER ISLAND
            // upper S-20, S-21, S-22 / lower S-11, S-12, S-13
            // ---------------------------
            PlaceHorizontalRow(new[] { "S-11", "S-12", "S-13" }, 47f, 76f, 0f, islandC, "H1-C-L");
            PlaceHorizontalRow(new[] { "S-20", "S-21", "S-22" }, 47f, 88f, 180f, islandC, "H1-C-U");

            // ---------------------------
            // CENTRAL UPPER-RIGHT ISLAND
            // upper S-23, S-24 / lower S-14, S-15
            // ---------------------------
            PlaceHorizontalRow(new[] { "S-14", "S-15" }, 112f, 76f, 0f, islandD, "H1-D-L");
            PlaceHorizontalRow(new[] { "S-23", "S-24" }, 112f, 88f, 180f, islandD, "H1-D-U");

            // ---------------------------
            // SL / SPECIAL LOCATIONS
            // Using 3x6 metric booth with 6 m frontage. Rotations match the plan orientation.
            // ---------------------------
            PlaceStall("PF_Stall_3x6", "H1-SL1", "SL 1", "Hall 1", new Vector3(7f, 0f, 91f), 90f, special);
            PlaceStall("PF_Stall_3x6", "H1-SL2", "SL 2", "Hall 1", new Vector3(151f, 0f, 88f), 90f, special);
            PlaceStall("PF_Stall_3x6", "H1-SL3", "SL 3", "Hall 1", new Vector3(151f, 0f, 47f), 90f, special);
            PlaceStall("PF_Stall_3x6", "H1-SL4", "SL 4", "Hall 1", new Vector3(160f, 0f, 12f), 0f, special);

            // ---------------------------
            // SOUTH / ENTRANCE SPONSOR ROW
            // These are placeholders using standard modular stall sizes so models can be replaced later.
            // ---------------------------
            PlaceStall("PF_Stall_3x6", "H1-GOLD1", "GOLD 1", "Hall 1", new Vector3(42f, 0f, 12f), 0f, special);
            PlaceStall("PF_Stall_3x6", "H1-GOLD2", "GOLD 2", "Hall 1", new Vector3(64f, 0f, 12f), 0f, special);
            PlaceStall("PF_Stall_3x6", "H1-GOLD-SPONSOR", "Gold Sponsor", "Hall 1", new Vector3(87f, 0f, 12f), 0f, special);
            PlaceStall("PF_Stall_3x6", "H1-MAIN-SPONSOR", "Main Sponsor", "Hall 1", new Vector3(116f, 0f, 12f), 0f, special);
            PlaceStall("PF_Stall_3x6", "H1-EXPO-SPONSOR", "Expo Sponsor", "Hall 1", new Vector3(141f, 0f, 12f), 0f, special);

            // Co-sponsor sits near the Hall 1 -> Hall 2 connection in the supplied plan.
            PlaceStall("PF_Stall_3x3", "H1-CO-SPONSOR", "Co Sponsor", "Hall 1", new Vector3(163f, 0f, 27f), 90f, special);

            CreateHallLabel(hallRoot, "HALL 1", new Vector3(Hall1Feet.x * 0.5f, 0.2f, Hall1Feet.y * 0.56f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = hallRoot.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorUtility.DisplayDialog(
                "Mera Brand - Hall 1",
                "Hall 1 first-pass internal layout generated.\n\n" +
                "Includes:\n" +
                "• west perimeter stalls 1-5\n" +
                "• four central S-stall islands\n" +
                "• SL1-SL4\n" +
                "• Gold/Main/Expo/Co sponsor placeholders\n\n" +
                "Hall dimensions remain exact at 168 x 115 ft. Internal stall offsets are matched to the supplied plan and can be fine-tuned before Hall 2/Hall 3 are populated.",
                "OK");
        }

        private static void PlaceHorizontalRow(string[] displayLabels, float startX, float z, float yRotation, Transform parent, string internalPrefix)
        {
            const float spacing = 11f; // 3 m booth is 9.8425 ft; small practical divider gap retained.

            for (int i = 0; i < displayLabels.Length; i++)
            {
                string display = displayLabels[i];
                PlaceStall("PF_Stall_3x3", $"{internalPrefix}-{i + 1}", display, "Hall 1",
                    new Vector3(startX + i * spacing, 0f, z), yRotation, parent);
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
                if (prefabName.Contains("3x6"))
                {
                    metres = new Vector2(6f, 3f);
                    units = new Vector2(19.68504f, 9.84252f);
                    size = StallSize.ThreeBySix;
                }
                else if (prefabName.Contains("6x6"))
                {
                    metres = new Vector2(6f, 6f);
                    units = new Vector2(19.68504f, 19.68504f);
                    size = StallSize.SixBySix;
                }
                else
                {
                    metres = new Vector2(3f, 3f);
                    units = new Vector2(9.84252f, 9.84252f);
                    size = StallSize.ThreeByThree;
                }

                identity.EditorConfigure(internalId, displayLabel, hall, size, metres, units, 2.5f);
            }

            CreateStallLabel(instance.transform, displayLabel);
            return instance;
        }

        private static void CreateStallLabel(Transform stall, string label)
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
            tmp.enableAutoSizing = false;
            tmp.rectTransform.sizeDelta = new Vector2(9f, 3f);
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

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MeraBrand.Expo.Stalls;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MeraBrand.Expo.Editor
{
    public static class StallPrefabFinalizationTools
    {
        private const string StallPrefabPath = "Assets/_Project/Prefabs/Stalls";
        private const string SelectedMaterialPath = "Assets/_Project/Art/Materials/MAT_Stall_Selected.mat";
        private const string BookedMaterialPath = "Assets/_Project/Art/Materials/MAT_Stall_Booked.mat";
        private const string StallNumberObjectName = "Phase6_ExhibitorNumber_TMP";

        private static readonly string[] PrefabNames =
        {
            "PF_Stall_3x3",
            "PF_Stall_3x6",
            "PF_Stall_6x6"
        };

        [MenuItem("Mera Brand/Stall Tools/Finalize Standard Stall Prefabs")]
        public static void FinalizeStandardStallPrefabs()
        {
            Material selectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SelectedMaterialPath);
            Material bookedMaterial = AssetDatabase.LoadAssetAtPath<Material>(BookedMaterialPath);

            if (selectedMaterial == null || bookedMaterial == null)
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Stall Tools",
                    "Required stall state materials were not found. Run Phase 5 and Phase 6 setup once before finalizing the prefabs.",
                    "OK");
                return;
            }

            int finalized = 0;
            foreach (string prefabName in PrefabNames)
            {
                string path = $"{StallPrefabPath}/{prefabName}.prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                    continue;

                try
                {
                    StallIdentity identity = root.GetComponent<StallIdentity>();
                    if (identity == null)
                    {
                        Debug.LogWarning($"Skipping {prefabName}: StallIdentity is missing.");
                        continue;
                    }

                    StallBookingVisual bookingVisual = root.GetComponent<StallBookingVisual>();
                    if (bookingVisual == null)
                        bookingVisual = root.AddComponent<StallBookingVisual>();

                    CreateOrUpdateSelectionHighlight(root.transform, identity, selectedMaterial);
                    CreateOrUpdateBookedOverlay(root.transform, identity, bookedMaterial);
                    CreateOrUpdateExhibitorName(root.transform, identity);
                    CreateOrUpdateLogo(root.transform, identity);

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    finalized++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Mera Brand - Stall Tools",
                $"Finalized {finalized} standard stall prefabs.\n\nEach prefab now permanently contains:\n" +
                "• StallIdentity\n" +
                "• StallBookingVisual\n" +
                "• HeaderAnchor / VisitPoint / LookTarget\n" +
                "• Phase5_SelectionHighlight\n" +
                "• Phase6_BookedOverlay\n" +
                "• Phase6_ExhibitorName_TMP\n" +
                "• Phase7_ExhibitorLogo\n\n" +
                "You can now manually reposition the exhibitor name and logo inside each prefab before replacing the stall model.",
                "OK");
        }

        [MenuItem("Mera Brand/Stalls/Assign + Validate")]
        public static void AssignAndValidateStalls()
        {
            ValidateAndAssignManualStallIds();
        }

        [MenuItem("Mera Brand/Stall Tools/Validate + Assign IDs To Manually Placed Stalls")]
        public static void ValidateAndAssignManualStallIds()
        {
            StallIdentity[] stalls = Object.FindObjectsByType<StallIdentity>(FindObjectsSortMode.None);
            HashSet<string> usedIds = new(StringComparer.OrdinalIgnoreCase);
            int assigned = 0;
            int duplicatesFixed = 0;
            int nextNumber = 1;

            foreach (StallIdentity stall in stalls)
            {
                if (stall == null)
                    continue;

                string id = stall.StallId?.Trim();
                if (string.IsNullOrWhiteSpace(id) || id == "UNASSIGNED")
                    continue;

                if (!usedIds.Add(id))
                    continue;

                if (TryReadGeneratedNumber(id, out int number))
                    nextNumber = Mathf.Max(nextNumber, number + 1);
            }

            usedIds.Clear();

            foreach (StallIdentity stall in stalls)
            {
                if (stall == null)
                    continue;

                string currentId = stall.StallId?.Trim();
                bool missing = string.IsNullOrWhiteSpace(currentId) || currentId == "UNASSIGNED";
                bool duplicate = !missing && usedIds.Contains(currentId);

                if (missing || duplicate)
                {
                    string newId;
                    do
                    {
                        newId = $"STALL-{nextNumber:000}";
                        nextNumber++;
                    }
                    while (usedIds.Contains(newId));

                    Undo.RecordObject(stall, "Assign Unique Stall ID");
                    stall.EditorConfigure(
                        newId,
                        stall.DisplayName,
                        stall.Hall,
                        stall.Size,
                        stall.FootprintMeters,
                        stall.FootprintUnityUnits,
                        stall.WallHeightMeters);
                    EditorUtility.SetDirty(stall);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(stall);
                    usedIds.Add(newId);
                    assigned++;
                    if (duplicate)
                        duplicatesFixed++;
                }
                else
                {
                    usedIds.Add(currentId);
                }
            }

            StallNumberResult numberResult = AssignDisplayNumbers(stalls);

            if (EditorSceneManager.GetActiveScene().IsValid())
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "Mera Brand - Stalls",
                $"Scanned {stalls.Length} manually placed stall instances.\n\n" +
                $"IDs assigned/reassigned: {assigned}\n" +
                $"Duplicate IDs fixed: {duplicatesFixed}\n\n" +
                $"Normal numbers (S_): {numberResult.normalCount}\n" +
                $"Silver numbers (SL_): {numberResult.silverCount}\n" +
                $"Gold numbers (G_): {numberResult.goldCount}\n" +
                $"Missing {StallNumberObjectName}: {numberResult.missingLabelCount}\n\n" +
                "Existing unique custom IDs were preserved. Stall numbers were written to the existing prefab TMP objects.",
                "OK");
        }

        private static StallNumberResult AssignDisplayNumbers(IEnumerable<StallIdentity> stallCollection)
        {
            StallNumberResult result = new();
            int normal = 1;
            int silver = 1;
            int gold = 1;

            IEnumerable<StallIdentity> ordered = stallCollection
                .Where(stall => stall != null)
                .OrderBy(stall => stall.StallId, StringComparer.OrdinalIgnoreCase);

            foreach (StallIdentity stall in ordered)
            {
                string number;
                switch (ResolveNumberType(stall))
                {
                    case StallNumberType.Silver:
                        number = $"SL_{silver++}";
                        result.silverCount++;
                        break;
                    case StallNumberType.Gold:
                        number = $"G_{gold++}";
                        result.goldCount++;
                        break;
                    default:
                        number = $"S_{normal++}";
                        result.normalCount++;
                        break;
                }

                TMP_Text label = stall
                    .GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(tmp => tmp != null && tmp.gameObject.name == StallNumberObjectName);

                if (label == null)
                {
                    result.missingLabelCount++;
                    Debug.LogWarning($"Stall '{stall.name}' ({stall.StallId}) is missing child TMP '{StallNumberObjectName}'. Number '{number}' was not written.", stall);
                    continue;
                }

                Undo.RecordObject(label, "Assign Stall Display Number");
                label.text = number;
                label.gameObject.SetActive(false);
                EditorUtility.SetDirty(label);
                EditorUtility.SetDirty(label.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                PrefabUtility.RecordPrefabInstancePropertyModifications(label.gameObject);
            }

            return result;
        }

        private static StallNumberType ResolveNumberType(StallIdentity stall)
        {
            if (stall == null)
                return StallNumberType.Normal;

            switch (stall.Size)
            {
                case StallSize.ThreeBySix:
                    return StallNumberType.Silver;
                case StallSize.SixBySix:
                    return StallNumberType.Gold;
                case StallSize.ThreeByThree:
                    return StallNumberType.Normal;
            }

            Vector2 size = stall.FootprintMeters;
            bool x6 = Mathf.Approximately(size.x, 6f);
            bool y6 = Mathf.Approximately(size.y, 6f);
            bool x3 = Mathf.Approximately(size.x, 3f);
            bool y3 = Mathf.Approximately(size.y, 3f);

            if (x6 && y6)
                return StallNumberType.Gold;
            if ((x6 && y3) || (x3 && y6))
                return StallNumberType.Silver;
            return StallNumberType.Normal;
        }

        private static void CreateOrUpdateSelectionHighlight(Transform root, StallIdentity identity, Material material)
        {
            GameObject highlight = GetOrCreateCube(root, "Phase5_SelectionHighlight");
            highlight.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            highlight.transform.localRotation = Quaternion.identity;
            highlight.transform.localScale = new Vector3(
                identity.FootprintUnityUnits.x + 0.5f,
                0.12f,
                identity.FootprintUnityUnits.y + 0.5f);
            RemoveColliderImmediate(highlight);
            SetMaterial(highlight, material);
            highlight.SetActive(false);
        }

        private static void CreateOrUpdateBookedOverlay(Transform root, StallIdentity identity, Material material)
        {
            GameObject overlay = GetOrCreateCube(root, "Phase6_BookedOverlay");
            overlay.transform.localPosition = new Vector3(0f, 0.19f, 0f);
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = new Vector3(
                identity.FootprintUnityUnits.x + 0.2f,
                0.10f,
                identity.FootprintUnityUnits.y + 0.2f);
            RemoveColliderImmediate(overlay);
            SetMaterial(overlay, material);
            overlay.SetActive(false);
        }

        private static void CreateOrUpdateExhibitorName(Transform root, StallIdentity identity)
        {
            Transform existing = root.Find("Phase6_ExhibitorName_TMP");
            GameObject go = existing != null ? existing.gameObject : new GameObject("Phase6_ExhibitorName_TMP");
            if (existing == null)
                go.transform.SetParent(root, false);

            TextMeshPro tmp = go.GetComponent<TextMeshPro>();
            if (tmp == null)
                tmp = go.AddComponent<TextMeshPro>();

            Transform header = root.Find("HeaderAnchor");
            Vector3 headerPosition = header != null
                ? header.localPosition
                : new Vector3(0f, identity.WallHeightMeters * 3.280839895f, identity.FootprintUnityUnits.y * 0.5f);

            if (existing == null)
            {
                go.transform.localPosition = headerPosition + new Vector3(0f, -0.6f, -0.08f);
                go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            tmp.text = "EXHIBITOR NAME";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 1.4f;
            tmp.rectTransform.sizeDelta = new Vector2(Mathf.Max(8f, identity.FootprintUnityUnits.x * 0.85f), 3f);
            go.SetActive(false);
        }

        private static void CreateOrUpdateLogo(Transform root, StallIdentity identity)
        {
            Transform existing = root.Find("Phase7_ExhibitorLogo");
            GameObject go = existing != null ? existing.gameObject : new GameObject("Phase7_ExhibitorLogo");
            if (existing == null)
                go.transform.SetParent(root, false);

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = go.AddComponent<SpriteRenderer>();

            Transform header = root.Find("HeaderAnchor");
            Vector3 headerPosition = header != null
                ? header.localPosition
                : new Vector3(0f, identity.WallHeightMeters * 3.280839895f, identity.FootprintUnityUnits.y * 0.5f);

            if (existing == null)
            {
                go.transform.localPosition = headerPosition + new Vector3(0f, 1.5f, -0.12f);
                go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            renderer.sortingOrder = 5;
            go.SetActive(false);
        }

        private static GameObject GetOrCreateCube(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void RemoveColliderImmediate(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
        }

        private static void SetMaterial(GameObject go, Material material)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        private static bool TryReadGeneratedNumber(string id, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("STALL-", StringComparison.OrdinalIgnoreCase))
                return false;
            return int.TryParse(id.Substring(6), out value);
        }

        private enum StallNumberType
        {
            Normal,
            Silver,
            Gold
        }

        private sealed class StallNumberResult
        {
            public int normalCount;
            public int silverCount;
            public int goldCount;
            public int missingLabelCount;
        }
    }
}
#endif

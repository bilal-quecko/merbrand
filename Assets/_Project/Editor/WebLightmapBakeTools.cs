#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MeraBrand.Expo.Editor
{
    public static class WebLightmapBakeTools
    {
        private const string LightingAssetPath = "Assets/Settings/New Lighting Settings.lighting";

        private static readonly string[] ExcludedHierarchyHints =
        {
            "phase5_selectionhighlight",
            "phase6_bookedoverlay",
            "phase6_exhibitorname_tmp",
            "phase6_exhibitornumber_tmp",
            "phase7_exhibitorlogo",
            "/ui/",
            "/systems/",
            "/navigation/",
            "visitorflycamera",
            "admintopdowncamera",
            "topdown_neutrallight",
            "runtime",
            "dynamic",
            "selection",
            "highlight"
        };

        [MenuItem("Mera Brand/Web/Optimization/Lighting/Configure Web Lighting Bake")]
        public static void ConfigureWebLightingBake()
        {
            LightingSettings settings = GetLightingSettings();
            if (settings == null)
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Web Lighting",
                    $"Lighting Settings asset not found at:\n{LightingAssetPath}",
                    "OK");
                return;
            }

            SerializedObject so = new(settings);

            SetBool(so, "m_EnableBakedLightmaps", true);
            SetBool(so, "m_EnableRealtimeLightmaps", false);
            SetBool(so, "m_RealtimeEnvironmentLighting", false);

            SetFloat(so, "m_BakeResolution", 10f);
            SetInt(so, "m_LightmapMaxSize", 1024);
            SetInt(so, "m_Padding", 4);

            // Non-directional lightmaps are substantially cheaper for Web/mobile.
            SetInt(so, "m_LightmapsBakeMode", 0);

            SetBool(so, "m_AO", true);
            SetFloat(so, "m_AOMaxDistance", 0.5f);
            SetFloat(so, "m_CompAOExponent", 1f);
            SetFloat(so, "m_CompAOExponentDirect", 0f);

            SetInt(so, "m_PVRDirectSampleCount", 32);
            SetInt(so, "m_PVRSampleCount", 128);
            SetInt(so, "m_PVREnvironmentSampleCount", 64);
            SetInt(so, "m_PVRBounces", 2);
            SetInt(so, "m_PVRMinBounces", 1);
            SetInt(so, "m_LightProbeSampleCountMultiplier", 2);

            // Keep compression and filtering enabled. Existing asset uses compressed lightmaps.
            SetInt(so, "m_LightmapCompression", 3);
            SetInt(so, "m_FilterMode", 1);
            SetBool(so, "m_EnableWorkerProcessBaking", true);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Mera Brand - Web Lighting",
                "Web-oriented Lighting Settings configured.\n\n" +
                "Baked GI: ON\n" +
                "Realtime GI: OFF\n" +
                "Resolution: 10 texels/unit\n" +
                "Max Lightmap: 1024\n" +
                "Directional Mode: Non-Directional\n" +
                "Direct Samples: 32\n" +
                "Indirect Samples: 128\n" +
                "Environment Samples: 64\n" +
                "Bounces: 2\n" +
                "AO: ON\n" +
                "Compression: ON\n\n" +
                "No bake was started.",
                "OK");
        }

        [MenuItem("Mera Brand/Web/Optimization/Lighting/Audit Bake Contributors")]
        public static void AuditBakeContributors()
        {
            if (!TryGetScene(out Scene scene))
                return;

            MeshRenderer[] renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            int total = 0;
            int safe = 0;
            int contributors = 0;
            int safeNotContributing = 0;
            int excluded = 0;
            int inactive = 0;

            List<string> excludedSamples = new();

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.scene != scene)
                    continue;

                total++;

                if (!renderer.gameObject.activeInHierarchy || !renderer.enabled)
                {
                    inactive++;
                    continue;
                }

                bool isContributor = IsContributeGI(renderer.gameObject);
                if (isContributor)
                    contributors++;

                if (IsSafeBakeRenderer(renderer, out string reason))
                {
                    safe++;
                    if (!isContributor)
                        safeNotContributing++;
                }
                else
                {
                    excluded++;
                    if (excludedSamples.Count < 25)
                        excludedSamples.Add($"{GetHierarchyPath(renderer.transform)}  [{reason}]");
                }
            }

            StringBuilder report = new();
            report.AppendLine("========== MERA BRAND BAKE CONTRIBUTOR AUDIT ==========");
            report.AppendLine($"Scene: {scene.name}");
            report.AppendLine($"Mesh Renderers: {total:N0}");
            report.AppendLine($"Active/enabled candidates inspected: {total - inactive:N0}");
            report.AppendLine($"Already Contribute GI: {contributors:N0}");
            report.AppendLine($"Safe static bake renderers: {safe:N0}");
            report.AppendLine($"Safe but NOT Contribute GI: {safeNotContributing:N0}");
            report.AppendLine($"Excluded dynamic/runtime renderers: {excluded:N0}");
            report.AppendLine($"Inactive/disabled renderers: {inactive:N0}");
            report.AppendLine();
            report.AppendLine("SAMPLE EXCLUSIONS");
            foreach (string sample in excludedSamples)
                report.AppendLine("- " + sample);
            report.AppendLine("=======================================================");
            Debug.Log(report.ToString());

            EditorUtility.DisplayDialog(
                "Mera Brand - Bake Contributor Audit",
                $"Scene: {scene.name}\n\n" +
                $"Mesh Renderers: {total:N0}\n" +
                $"Already Contribute GI: {contributors:N0}\n" +
                $"Safe static bake renderers: {safe:N0}\n" +
                $"Safe but not contributing: {safeNotContributing:N0}\n" +
                $"Excluded runtime/dynamic: {excluded:N0}\n" +
                $"Inactive/disabled: {inactive:N0}\n\n" +
                "Full audit written to the Unity Console.",
                "OK");
        }

        [MenuItem("Mera Brand/Web/Optimization/Lighting/Prepare Safe Static Geometry For Baking")]
        public static void PrepareSafeStaticGeometryForBaking()
        {
            if (!TryGetScene(out Scene scene))
                return;

            MeshRenderer[] renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            List<MeshRenderer> safe = renderers
                .Where(r => r != null &&
                            r.gameObject.scene == scene &&
                            r.gameObject.activeInHierarchy &&
                            r.enabled &&
                            IsSafeBakeRenderer(r, out _))
                .ToList();

            List<MeshRenderer> needsChange = safe
                .Where(r => !IsContributeGI(r.gameObject))
                .ToList();

            if (needsChange.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Bake Preparation",
                    $"No changes are required.\n\nSafe renderers inspected: {safe.Count:N0}\n" +
                    "All are already marked Contribute GI.",
                    "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Prepare Safe Static Geometry For Baking",
                $"This will mark {needsChange.Count:N0} safe MeshRenderer GameObjects as Contribute GI.\n\n" +
                $"Safe renderers inspected: {safe.Count:N0}\n\n" +
                "It excludes booking overlays, exhibitor text/logo, selection visuals, UI, camera/navigation/system objects, animated hierarchies, and non-kinematic Rigidbody hierarchies.\n\n" +
                "No light bake will start automatically.\n\nProceed?",
                "Prepare",
                "Cancel");

            if (!confirmed)
                return;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Prepare Mera Brand Static Geometry For Baking");

            int changed = 0;

            foreach (MeshRenderer renderer in needsChange)
            {
                if (renderer == null)
                    continue;

                GameObject go = renderer.gameObject;
                Undo.RecordObject(go, "Mark Contribute GI");

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                flags |= StaticEditorFlags.ContributeGI;
                GameObjectUtility.SetStaticEditorFlags(go, flags);

                EditorUtility.SetDirty(go);
                changed++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[Mera Brand Lighting] Marked {changed:N0} safe MeshRenderer objects as Contribute GI.");

            EditorUtility.DisplayDialog(
                "Mera Brand - Bake Preparation",
                $"Preparation complete.\n\nMarked Contribute GI: {changed:N0}\n" +
                $"Safe renderers inspected: {safe.Count:N0}\n\n" +
                "No bake has been started. Run Audit Bake Contributors again before baking.",
                "OK");
        }

        private static LightingSettings GetLightingSettings()
        {
            LightingSettings settings = Lightmapping.lightingSettings;
            if (settings != null)
                return settings;

            settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingAssetPath);
            if (settings != null)
                Lightmapping.lightingSettings = settings;

            return settings;
        }

        private static bool IsSafeBakeRenderer(MeshRenderer renderer, out string reason)
        {
            reason = string.Empty;

            if (renderer == null)
            {
                reason = "missing renderer";
                return false;
            }

            GameObject go = renderer.gameObject;
            string path = GetHierarchyPath(renderer.transform).ToLowerInvariant();

            foreach (string hint in ExcludedHierarchyHints)
            {
                if (path.Contains(hint))
                {
                    reason = "runtime/UI hierarchy";
                    return false;
                }
            }

            if (renderer.GetComponent<TMPro.TMP_Text>() != null)
            {
                reason = "TMP text";
                return false;
            }

            Transform current = renderer.transform;
            int depth = 0;

            while (current != null && depth < 12)
            {
                if (current.GetComponent<Animator>() != null || current.GetComponent<Animation>() != null)
                {
                    reason = "animated hierarchy";
                    return false;
                }

                Rigidbody rb = current.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    reason = "dynamic Rigidbody";
                    return false;
                }

                if (current.GetComponent<Canvas>() != null)
                {
                    reason = "UI Canvas hierarchy";
                    return false;
                }

                current = current.parent;
                depth++;
            }

            return true;
        }

        private static bool IsContributeGI(GameObject go)
        {
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
            return (flags & StaticEditorFlags.ContributeGI) != 0;
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.boolValue = value;
        }

        private static void SetInt(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.floatValue = value;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return "(missing)";

            Stack<string> names = new();
            Transform current = transform;

            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static bool TryGetScene(out Scene scene)
        {
            scene = SceneManager.GetActiveScene();

            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Web Lighting",
                    "Open the Exhibition scene first.",
                    "OK");
                return false;
            }

            return true;
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeraBrand.Expo.Editor
{
    public static class WebLightingOptimizationTools
    {
        private static readonly string[] DynamicNameHints =
        {
            "topdown",
            "runtime",
            "dynamic",
            "selection",
            "highlight",
            "flash",
            "emergency",
            "animated"
        };

        [MenuItem("Mera Brand/Web/Optimization/Lighting/Audit Lights")]
        public static void AuditLights()
        {
            if (!TryGetScene(out Scene scene))
                return;

            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            LightAudit audit = BuildAudit(lights);
            Debug.Log(BuildAuditReport(scene.name, audit, lights));

            EditorUtility.DisplayDialog(
                "Mera Brand - Lighting Audit",
                $"Scene: {scene.name}\n\n" +
                $"Total Lights: {audit.total:N0}\n" +
                $"Active/Enabled: {audit.enabled:N0}\n" +
                $"Realtime: {audit.realtime:N0}\n" +
                $"Mixed: {audit.mixed:N0}\n" +
                $"Baked: {audit.baked:N0}\n\n" +
                $"Point: {audit.point:N0}\n" +
                $"Spot: {audit.spot:N0}\n" +
                $"Directional: {audit.directional:N0}\n" +
                $"Shadow-casting: {audit.shadowCasting:N0}\n\n" +
                $"Safe bake candidates: {audit.safeBakeCandidates:N0}\n" +
                $"Skipped dynamic/special: {audit.dynamicOrSpecial:N0}\n\n" +
                "The complete audit was written to the Unity Console.",
                "OK");
        }

        [MenuItem("Mera Brand/Web/Optimization/Lighting/Prepare Static Lights For Baking")]
        public static void PrepareStaticLightsForBaking()
        {
            if (!TryGetScene(out Scene scene))
                return;

            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            List<Light> candidates = lights
                .Where(IsSafeBakeCandidate)
                .ToList();

            int realtimeCandidates = candidates.Count(l => l.lightmapBakeType == LightmapBakeType.Realtime);
            int mixedCandidates = candidates.Count(l => l.lightmapBakeType == LightmapBakeType.Mixed);

            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Lighting Preparation",
                    "No safe Point/Spot bake candidates were found in the current scene.",
                    "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Prepare Static Lights For Baking",
                $"This will convert {candidates.Count:N0} eligible Point/Spot lights to Baked.\n\n" +
                $"Realtime candidates: {realtimeCandidates:N0}\n" +
                $"Mixed candidates: {mixedCandidates:N0}\n\n" +
                "It will NOT modify Directional lights, the TopDown neutral light, disabled lights, or lights detected as dynamic/special.\n\n" +
                "No bake will start automatically.\n\nProceed?",
                "Prepare",
                "Cancel");

            if (!confirmed)
                return;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Prepare Mera Brand Lights For Baking");

            int converted = 0;
            int shadowsKept = 0;

            foreach (Light light in candidates)
            {
                if (light == null)
                    continue;

                Undo.RecordObject(light, "Convert Light To Baked");
                if (light.shadows != LightShadows.None)
                    shadowsKept++;

                light.lightmapBakeType = LightmapBakeType.Baked;
                EditorUtility.SetDirty(light);
                converted++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            LightAudit after = BuildAudit(
                UnityEngine.Object.FindObjectsByType<Light>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None));

            Debug.Log(
                $"[Mera Brand Lighting] Prepared {converted:N0} lights for baking. " +
                $"Baked now: {after.baked:N0}, Realtime remaining: {after.realtime:N0}, Mixed remaining: {after.mixed:N0}. " +
                $"Converted lights retaining baked shadows: {shadowsKept:N0}.");

            EditorUtility.DisplayDialog(
                "Mera Brand - Lighting Preparation",
                $"Prepared successfully.\n\n" +
                $"Converted to Baked: {converted:N0}\n" +
                $"Realtime remaining: {after.realtime:N0}\n" +
                $"Mixed remaining: {after.mixed:N0}\n" +
                $"Baked total: {after.baked:N0}\n\n" +
                "No light bake has been started yet.\n" +
                "Use the Lighting Audit again to verify the result.",
                "OK");
        }

        [MenuItem("Mera Brand/Web/Optimization/Lighting/Disable Realtime Shadows On Remaining Non-Directional Lights")]
        public static void DisableRealtimeShadowsOnRemainingNonDirectionalLights()
        {
            if (!TryGetScene(out Scene scene))
                return;

            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            List<Light> targets = lights
                .Where(l =>
                    l != null &&
                    l.enabled &&
                    l.gameObject.activeInHierarchy &&
                    l.type != LightType.Directional &&
                    l.lightmapBakeType != LightmapBakeType.Baked &&
                    l.shadows != LightShadows.None)
                .ToList();

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Mera Brand - Lighting Optimization",
                    "No remaining realtime/mixed non-directional shadow lights were found.",
                    "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Disable Remaining Realtime Shadows",
                $"This will disable realtime shadows on {targets.Count:N0} remaining non-directional realtime/mixed lights.\n\n" +
                "Baked lights and Directional lights are not changed.\n\nProceed?",
                "Disable Shadows",
                "Cancel");

            if (!confirmed)
                return;

            Undo.RecordObjects(targets.Cast<UnityEngine.Object>().ToArray(), "Disable Realtime Light Shadows");

            foreach (Light light in targets)
            {
                light.shadows = LightShadows.None;
                EditorUtility.SetDirty(light);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog(
                "Mera Brand - Lighting Optimization",
                $"Realtime shadows disabled on {targets.Count:N0} non-directional lights.",
                "OK");
        }

        private static bool IsSafeBakeCandidate(Light light)
        {
            if (light == null)
                return false;

            if (!light.enabled || !light.gameObject.activeInHierarchy)
                return false;

            if (light.lightmapBakeType == LightmapBakeType.Baked)
                return false;

            if (light.type != LightType.Point && light.type != LightType.Spot)
                return false;

            if (IsDynamicOrSpecial(light))
                return false;

            return true;
        }

        private static bool IsDynamicOrSpecial(Light light)
        {
            if (light == null)
                return true;

            string objectName = light.gameObject.name ?? string.Empty;
            string lower = objectName.ToLowerInvariant();

            if (lower.Contains("topdown_neutrallight"))
                return true;

            foreach (string hint in DynamicNameHints)
            {
                if (lower.Contains(hint))
                    return true;
            }

            Transform current = light.transform;
            int depth = 0;
            while (current != null && depth < 6)
            {
                if (current.GetComponent<Animator>() != null || current.GetComponent<Animation>() != null)
                    return true;

                string currentName = current.name != null ? current.name.ToLowerInvariant() : string.Empty;
                foreach (string hint in DynamicNameHints)
                {
                    if (currentName.Contains(hint))
                        return true;
                }

                current = current.parent;
                depth++;
            }

            return false;
        }

        private static LightAudit BuildAudit(IEnumerable<Light> lights)
        {
            LightAudit audit = new();

            foreach (Light light in lights)
            {
                if (light == null)
                    continue;

                audit.total++;

                if (light.enabled && light.gameObject.activeInHierarchy)
                    audit.enabled++;

                switch (light.lightmapBakeType)
                {
                    case LightmapBakeType.Realtime:
                        audit.realtime++;
                        break;
                    case LightmapBakeType.Mixed:
                        audit.mixed++;
                        break;
                    case LightmapBakeType.Baked:
                        audit.baked++;
                        break;
                }

                switch (light.type)
                {
                    case LightType.Point:
                        audit.point++;
                        break;
                    case LightType.Spot:
                        audit.spot++;
                        break;
                    case LightType.Directional:
                        audit.directional++;
                        break;
                    case LightType.Rectangle:
                        audit.rectangle++;
                        break;
                }

                if (light.shadows != LightShadows.None)
                    audit.shadowCasting++;

                if (IsSafeBakeCandidate(light))
                    audit.safeBakeCandidates++;
                else if (IsDynamicOrSpecial(light))
                    audit.dynamicOrSpecial++;
            }

            return audit;
        }

        private static string BuildAuditReport(string sceneName, LightAudit audit, Light[] lights)
        {
            StringBuilder report = new();
            report.AppendLine("========== MERA BRAND LIGHTING AUDIT ==========");
            report.AppendLine($"Scene: {sceneName}");
            report.AppendLine($"Total Lights: {audit.total:N0}");
            report.AppendLine($"Active / Enabled: {audit.enabled:N0}");
            report.AppendLine();
            report.AppendLine("BAKE MODE");
            report.AppendLine($"Realtime: {audit.realtime:N0}");
            report.AppendLine($"Mixed: {audit.mixed:N0}");
            report.AppendLine($"Baked: {audit.baked:N0}");
            report.AppendLine();
            report.AppendLine("TYPE");
            report.AppendLine($"Directional: {audit.directional:N0}");
            report.AppendLine($"Point: {audit.point:N0}");
            report.AppendLine($"Spot: {audit.spot:N0}");
            report.AppendLine($"Rectangle/Area: {audit.rectangle:N0}");
            report.AppendLine();
            report.AppendLine($"Shadow-casting: {audit.shadowCasting:N0}");
            report.AppendLine($"Safe bake candidates: {audit.safeBakeCandidates:N0}");
            report.AppendLine($"Dynamic/special skipped: {audit.dynamicOrSpecial:N0}");
            report.AppendLine();
            report.AppendLine("REMAINING REALTIME/MIXED LIGHTS");

            foreach (Light light in lights
                         .Where(l => l != null && l.enabled && l.gameObject.activeInHierarchy &&
                                     l.lightmapBakeType != LightmapBakeType.Baked)
                         .OrderBy(l => l.lightmapBakeType)
                         .ThenBy(l => l.type)
                         .ThenBy(l => l.name))
            {
                string path = GetHierarchyPath(light.transform);
                report.AppendLine(
                    $"- {light.lightmapBakeType} | {light.type} | Shadows={light.shadows} | " +
                    $"Range={light.range:0.##} | Intensity={light.intensity:0.##} | {path}");
            }

            report.AppendLine("===============================================");
            return report.ToString();
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
                    "Mera Brand - Lighting Optimization",
                    "Open the Exhibition scene first.",
                    "OK");
                return false;
            }

            return true;
        }

        private sealed class LightAudit
        {
            public int total;
            public int enabled;
            public int realtime;
            public int mixed;
            public int baked;
            public int directional;
            public int point;
            public int spot;
            public int rectangle;
            public int shadowCasting;
            public int safeBakeCandidates;
            public int dynamicOrSpecial;
        }
    }
}
#endif

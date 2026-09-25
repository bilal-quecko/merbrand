#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MeraBrand.Expo.Editor
{
    public static class WebPerformanceAnalyzer
    {
        [MenuItem("Mera Brand/Web/Optimization/Analyze Current Scene")]
        public static void AnalyzeCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("Mera Brand - Web Optimization", "Open the Exhibition scene first.", "OK");
                return;
            }

            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            MeshFilter[] meshFilters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            SkinnedMeshRenderer[] skinned = UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            MeshCollider[] meshColliders = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            ReflectionProbe[] reflectionProbes = UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Terrain[] terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            ParticleSystem[] particles = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Volume[] volumes = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            HashSet<Material> uniqueMaterials = new();
            HashSet<Texture> uniqueTextures = new();
            HashSet<Mesh> uniqueMeshes = new();

            long renderedTriangles = 0;
            long uniqueTriangles = 0;
            long renderedVertices = 0;
            long uniqueVertices = 0;

            foreach (MeshFilter filter in meshFilters)
            {
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null) continue;

                renderedVertices += mesh.vertexCount;
                renderedTriangles += CountTriangles(mesh);

                if (uniqueMeshes.Add(mesh))
                {
                    uniqueVertices += mesh.vertexCount;
                    uniqueTriangles += CountTriangles(mesh);
                }
            }

            foreach (SkinnedMeshRenderer smr in skinned)
            {
                Mesh mesh = smr != null ? smr.sharedMesh : null;
                if (mesh == null) continue;

                renderedVertices += mesh.vertexCount;
                renderedTriangles += CountTriangles(mesh);

                if (uniqueMeshes.Add(mesh))
                {
                    uniqueVertices += mesh.vertexCount;
                    uniqueTriangles += CountTriangles(mesh);
                }
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || !uniqueMaterials.Add(material))
                        continue;

                    foreach (string propertyName in material.GetTexturePropertyNames())
                    {
                        Texture texture = material.GetTexture(propertyName);
                        if (texture != null)
                            uniqueTextures.Add(texture);
                    }
                }
            }

            int texturesOver2K = uniqueTextures.Count(t => Math.Max(t.width, t.height) > 2048);
            int texturesOver4K = uniqueTextures.Count(t => Math.Max(t.width, t.height) > 4096);

            int realtime = lights.Count(l => l != null && l.lightmapBakeType == LightmapBakeType.Realtime);
            int mixed = lights.Count(l => l != null && l.lightmapBakeType == LightmapBakeType.Mixed);
            int baked = lights.Count(l => l != null && l.lightmapBakeType == LightmapBakeType.Baked);
            int shadowLights = lights.Count(l => l != null && l.shadows != LightShadows.None);
            int pointLights = lights.Count(l => l != null && l.type == LightType.Point);
            int spotLights = lights.Count(l => l != null && l.type == LightType.Spot);
            int directionalLights = lights.Count(l => l != null && l.type == LightType.Directional);
            int areaLights = lights.Count(l => l != null && l.type == LightType.Rectangle);

            int enabledRenderers = renderers.Count(r => r != null && r.enabled && r.gameObject.activeInHierarchy);
            int enabledLights = lights.Count(l => l != null && l.enabled && l.gameObject.activeInHierarchy);
            int enabledVolumes = volumes.Count(v => v != null && v.enabled && v.gameObject.activeInHierarchy);

            StringBuilder report = new();
            report.AppendLine("========== MERA BRAND WEB PERFORMANCE REPORT ==========");
            report.AppendLine($"Scene: {scene.name}");
            report.AppendLine();
            report.AppendLine("GEOMETRY");
            report.AppendLine($"Renderers: {renderers.Length:N0} (active/enabled: {enabledRenderers:N0})");
            report.AppendLine($"Mesh Filters: {meshFilters.Length:N0}");
            report.AppendLine($"Skinned Mesh Renderers: {skinned.Length:N0}");
            report.AppendLine($"Unique Mesh Assets Referenced: {uniqueMeshes.Count:N0}");
            report.AppendLine($"Rendered-instance Vertices: {renderedVertices:N0}");
            report.AppendLine($"Rendered-instance Triangles: {renderedTriangles:N0}");
            report.AppendLine($"Unique-mesh Vertices: {uniqueVertices:N0}");
            report.AppendLine($"Unique-mesh Triangles: {uniqueTriangles:N0}");
            report.AppendLine();
            report.AppendLine("MATERIALS / TEXTURES");
            report.AppendLine($"Unique Materials Referenced: {uniqueMaterials.Count:N0}");
            report.AppendLine($"Unique Textures Referenced: {uniqueTextures.Count:N0}");
            report.AppendLine($"Textures larger than 2048: {texturesOver2K:N0}");
            report.AppendLine($"Textures larger than 4096: {texturesOver4K:N0}");
            report.AppendLine();
            report.AppendLine("LIGHTING");
            report.AppendLine($"Lights: {lights.Length:N0} (active/enabled: {enabledLights:N0})");
            report.AppendLine($"Directional: {directionalLights:N0}");
            report.AppendLine($"Point: {pointLights:N0}");
            report.AppendLine($"Spot: {spotLights:N0}");
            report.AppendLine($"Area/Rectangle: {areaLights:N0}");
            report.AppendLine($"Realtime: {realtime:N0}");
            report.AppendLine($"Mixed: {mixed:N0}");
            report.AppendLine($"Baked: {baked:N0}");
            report.AppendLine($"Shadow-casting Lights: {shadowLights:N0}");
            report.AppendLine();
            report.AppendLine("OTHER RUNTIME COST");
            report.AppendLine($"Mesh Colliders: {meshColliders.Length:N0}");
            report.AppendLine($"Reflection Probes: {reflectionProbes.Length:N0}");
            report.AppendLine($"Terrains: {terrains.Length:N0}");
            report.AppendLine($"Particle Systems: {particles.Length:N0}");
            report.AppendLine($"Volumes: {volumes.Length:N0} (active/enabled: {enabledVolumes:N0})");
            report.AppendLine();
            report.AppendLine("CURRENT QUALITY");
            report.AppendLine($"Quality Level: {QualitySettings.GetQualityLevel()} ({QualitySettings.names[QualitySettings.GetQualityLevel()]})");
            report.AppendLine($"Shadows: {QualitySettings.shadows}");
            report.AppendLine($"Shadow Distance: {QualitySettings.shadowDistance:0.##}");
            report.AppendLine($"Shadow Cascades: {QualitySettings.shadowCascades}");
            report.AppendLine($"Realtime Reflection Probes: {QualitySettings.realtimeReflectionProbes}");
            report.AppendLine($"MSAA: {QualitySettings.antiAliasing}");
            report.AppendLine($"LOD Bias: {QualitySettings.lodBias:0.##}");
            report.AppendLine("=======================================================");

            string text = report.ToString();
            Debug.Log(text);

            string summary =
                $"Scene: {scene.name}\n\n" +
                $"Renderers: {renderers.Length:N0}\n" +
                $"Triangles (instances): {renderedTriangles:N0}\n" +
                $"Materials: {uniqueMaterials.Count:N0}\n" +
                $"Textures > 2K: {texturesOver2K:N0}\n" +
                $"Lights: {lights.Length:N0}\n" +
                $"Realtime Lights: {realtime:N0}\n" +
                $"Shadow Lights: {shadowLights:N0}\n" +
                $"Mesh Colliders: {meshColliders.Length:N0}\n\n" +
                "Full report was written to the Unity Console.";

            EditorUtility.DisplayDialog("Mera Brand - Web Performance Analysis", summary, "OK");
        }

        private static long CountTriangles(Mesh mesh)
        {
            if (mesh == null)
                return 0;

            long triangles = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                try
                {
                    if (mesh.GetTopology(i) == MeshTopology.Triangles)
                        triangles += (long)mesh.GetIndexCount(i) / 3L;
                }
                catch
                {
                    // Some imported/runtime meshes can reject index inspection.
                }
            }
            return triangles;
        }
    }
}
#endif

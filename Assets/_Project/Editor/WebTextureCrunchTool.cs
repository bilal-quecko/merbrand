#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MeraBrand.Expo.Editor
{
    /// <summary>
    /// Applies WebGL-only Crunch settings to colour textures in Assets.
    /// Existing Crunch imports are left exactly as they are.
    /// </summary>
    public static class WebTextureCrunchTool
    {
        private const string MenuRoot = "Mera Brand/Web/Optimization/Textures/";
        private const string PlatformName = "WebGL";
        private const int ReducedSize = 1024;
        private const int TargetSize = 2048;
        private const int CrunchQuality = 50;

        private sealed class Candidate
        {
            public string Path;
            public TextureImporterFormat Format;
            public int MaxSize;
        }

        [MenuItem(MenuRoot + "Preview WebGL Crunch Changes")]
        public static void Preview()
        {
            List<Candidate> candidates = FindCandidates(out int alreadyCrunched, out int unsupported);
            var report = new StringBuilder();
            report.AppendLine($"WebGL texture preview: {candidates.Count} to update, " +
                $"{alreadyCrunched} already crunched, {unsupported} unsupported texture types skipped.");
            foreach (Candidate candidate in candidates)
                report.AppendLine($"{candidate.Format}, max {candidate.MaxSize}: {candidate.Path}");

            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("WebGL Texture Crunch Preview",
                $"{candidates.Count} textures would change.\n" +
                $"{alreadyCrunched} already use Crunch and will be untouched.\n" +
                $"{unsupported} non-colour textures will be skipped.\n\n" +
                "See the Console for the full list.", "OK");
        }

        [MenuItem(MenuRoot + "Apply WebGL Crunch Changes")]
        public static void Apply()
        {
            List<Candidate> candidates = FindCandidates(out int alreadyCrunched, out int unsupported);
            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog("WebGL Texture Crunch", "No eligible textures need changes.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Apply WebGL Texture Crunch",
                $"Apply WebGL-only Crunch settings to {candidates.Count} textures?\n\n" +
                "Eligible 2048 textures will be capped at 1024. " +
                "Textures already using Crunch will not be modified. " +
                "This can take several minutes while Unity reimports textures.",
                "Apply", "Cancel"))
                return;

            int changed = 0;
            int failed = 0;
            try
            {
                for (int index = 0; index < candidates.Count; index++)
                {
                    Candidate candidate = candidates[index];
                    if (EditorUtility.DisplayCancelableProgressBar("Applying WebGL texture settings",
                        candidate.Path, (float)index / candidates.Count))
                        break;

                    try
                    {
                        var importer = AssetImporter.GetAtPath(candidate.Path) as TextureImporter;
                        if (importer == null || IsAlreadyCrunched(importer))
                            continue;

                        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(PlatformName);
                        settings.name = PlatformName;
                        settings.overridden = true;
                        settings.maxTextureSize = candidate.MaxSize;
                        settings.format = candidate.Format;
                        settings.textureCompression = TextureImporterCompression.Compressed;
                        settings.crunchedCompression = true;
                        settings.compressionQuality = CrunchQuality;

                        Undo.RecordObject(importer, "Optimize WebGL texture");
                        importer.SetPlatformTextureSettings(settings);
                        importer.SaveAndReimport();
                        changed++;
                    }
                    catch (Exception exception)
                    {
                        failed++;
                        Debug.LogError($"WebGL texture optimization failed for {candidate.Path}: {exception}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"WebGL texture optimization: changed {changed}, failed {failed}, " +
                $"already crunched {alreadyCrunched}, unsupported {unsupported}. " +
                "Run Preview again to see remaining candidates.");
            EditorUtility.DisplayDialog("WebGL Texture Crunch",
                $"Changed: {changed}\nFailed: {failed}\nAlready crunched: {alreadyCrunched}\n" +
                $"Unsupported texture types skipped: {unsupported}\n\n" +
                "Check the Console and compare visuals before making a new WebGL build.", "OK");
        }

        private static List<Candidate> FindCandidates(out int alreadyCrunched, out int unsupported)
        {
            alreadyCrunched = 0;
            unsupported = 0;
            var candidates = new List<Candidate>();
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                    continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                // BC1/BC3 Crunch is appropriate for ordinary RGB/RGBA colour images,
                // not normal maps, lightmaps, single-channel data or HDR/cubemaps.
                if (importer.textureShape != TextureImporterShape.Texture2D ||
                    (importer.textureType != TextureImporterType.Default &&
                     importer.textureType != TextureImporterType.Sprite))
                {
                    unsupported++;
                    continue;
                }

                if (IsAlreadyCrunched(importer))
                {
                    alreadyCrunched++;
                    continue;
                }

                TextureImporterPlatformSettings webSettings = importer.GetPlatformTextureSettings(PlatformName);
                TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
                int currentLimit = webSettings.overridden ? webSettings.maxTextureSize : defaultSettings.maxTextureSize;
                int sourceWidth;
                int sourceHeight;
                try
                {
                    importer.GetSourceTextureWidthAndHeight(out sourceWidth, out sourceHeight);
                }
                catch (Exception exception)
                {
                    unsupported++;
                    Debug.LogWarning($"Could not inspect texture {path}: {exception.Message}");
                    continue;
                }
                int effectiveSize = Mathf.Min(Mathf.Max(sourceWidth, sourceHeight), currentLimit);
                int newLimit = effectiveSize == TargetSize ? ReducedSize : currentLimit;

                bool hasAlpha = importer.alphaSource == TextureImporterAlphaSource.FromGrayScale ||
                    (importer.alphaSource == TextureImporterAlphaSource.FromInput &&
                     importer.DoesSourceTextureHaveAlpha());

                candidates.Add(new Candidate
                {
                    Path = path,
                    Format = hasAlpha ? TextureImporterFormat.DXT5Crunched : TextureImporterFormat.DXT1Crunched,
                    MaxSize = newLimit
                });
            }

            candidates.Sort((left, right) => string.CompareOrdinal(left.Path, right.Path));
            return candidates;
        }

        private static bool IsAlreadyCrunched(TextureImporter importer)
        {
            TextureImporterPlatformSettings webSettings = importer.GetPlatformTextureSettings(PlatformName);
            TextureImporterPlatformSettings effectiveSettings = webSettings.overridden
                ? webSettings
                : importer.GetDefaultPlatformTextureSettings();

            return effectiveSettings.crunchedCompression ||
                effectiveSettings.format == TextureImporterFormat.DXT1Crunched ||
                effectiveSettings.format == TextureImporterFormat.DXT5Crunched;
        }
    }
}
#endif

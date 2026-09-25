using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeraBrand.Expo.Web
{
    /// <summary>
    /// Applies conservative runtime-only quality settings for Web builds.
    /// Windows/PC builds are untouched. Lighting bake changes are intentionally
    /// not part of this pass.
    /// </summary>
    public static class WebPerformanceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            bool mobile = Application.isMobilePlatform;

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.antiAliasing = 0;
            QualitySettings.softParticles = false;
            QualitySettings.softVegetation = false;

            if (mobile)
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.shadowDistance = 0f;
                QualitySettings.shadowCascades = 0;
                QualitySettings.lodBias = 0.55f;
                QualitySettings.globalTextureMipmapLimit = 1;
            }
            else
            {
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.shadowDistance = 25f;
                QualitySettings.shadowCascades = 1;
                QualitySettings.lodBias = 0.85f;
                QualitySettings.globalTextureMipmapLimit = 0;
            }

            ConfigureUrpAsset(mobile);
            ConfigureCameraPostProcessing(mobile);

            Debug.Log($"[Web Performance] Applied {(mobile ? "mobile" : "desktop")} Web quality profile.");
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static void ConfigureUrpAsset(bool mobile)
        {
            RenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline;
            if (asset == null)
                return;

            Type type = asset.GetType();
            SetProperty(type, asset, "shadowDistance", mobile ? 0f : 25f);
            SetProperty(type, asset, "shadowCascadeCount", mobile ? 1 : 1);
            SetProperty(type, asset, "supportsMainLightShadows", !mobile);
            SetProperty(type, asset, "supportsAdditionalLightShadows", false);
            SetProperty(type, asset, "msaaSampleCount", 1);

            PropertyInfo additionalLights = type.GetProperty(
                "additionalLightsRenderingMode",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (additionalLights != null && additionalLights.CanWrite && additionalLights.PropertyType.IsEnum)
            {
                string requested = mobile ? "PerVertex" : "PerPixel";
                try
                {
                    object value = Enum.Parse(additionalLights.PropertyType, requested);
                    additionalLights.SetValue(asset, value);
                }
                catch
                {
                    // Keep the project's configured mode if this URP version uses different enum names.
                }
            }
        }

        private static void ConfigureCameraPostProcessing(bool mobile)
        {
            if (!mobile)
                return;

            Type additionalCameraType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");

            if (additionalCameraType == null)
                return;

            PropertyInfo renderPost = additionalCameraType.GetProperty(
                "renderPostProcessing",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (renderPost == null || !renderPost.CanWrite)
                return;

            Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Camera camera in cameras)
            {
                if (camera == null)
                    continue;

                Component additionalData = camera.GetComponent(additionalCameraType);
                if (additionalData != null)
                    renderPost.SetValue(additionalData, false);
            }
        }

        private static void SetProperty(Type type, object target, string propertyName, object value)
        {
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (property == null || !property.CanWrite)
                return;

            try
            {
                property.SetValue(target, value);
            }
            catch
            {
                // Keep the serialized project value when a URP version exposes
                // a property that cannot accept this runtime assignment.
            }
        }
#endif
    }
}

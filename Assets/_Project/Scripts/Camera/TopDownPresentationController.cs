using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeraBrand.Expo.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class TopDownPresentationController : MonoBehaviour
    {
        [SerializeField] private float topDownLightIntensity = 0.85f;
        [SerializeField] private float topDownAmbientIntensity = 0.45f;
        [SerializeField] private bool disableVolumesInTopDown = true;

        private readonly Dictionary<Light, bool> lightStates = new();
        private readonly Dictionary<Volume, bool> volumeStates = new();
        private Light topDownLight;
        private float previousAmbientIntensity;
        private float previousReflectionIntensity;
        private bool active;

        public void EnterTopDown()
        {
            if (active) return;
            active = true;

            lightStates.Clear();
            foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light == null || light == topDownLight) continue;
                lightStates[light] = light.enabled;
                light.enabled = false;
            }

            if (disableVolumesInTopDown)
            {
                volumeStates.Clear();
                foreach (Volume volume in FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (volume == null) continue;
                    volumeStates[volume] = volume.enabled;
                    volume.enabled = false;
                }
            }

            previousAmbientIntensity = RenderSettings.ambientIntensity;
            previousReflectionIntensity = RenderSettings.reflectionIntensity;
            RenderSettings.ambientIntensity = topDownAmbientIntensity;
            RenderSettings.reflectionIntensity = 0.2f;

            EnsureTopDownLight();
            if (topDownLight != null)
            {
                topDownLight.intensity = topDownLightIntensity;
                topDownLight.enabled = true;
            }
        }

        public void ExitTopDown()
        {
            if (!active) return;
            active = false;

            if (topDownLight != null)
                topDownLight.enabled = false;

            foreach (KeyValuePair<Light, bool> pair in lightStates)
            {
                if (pair.Key != null)
                    pair.Key.enabled = pair.Value;
            }
            lightStates.Clear();

            foreach (KeyValuePair<Volume, bool> pair in volumeStates)
            {
                if (pair.Key != null)
                    pair.Key.enabled = pair.Value;
            }
            volumeStates.Clear();

            RenderSettings.ambientIntensity = previousAmbientIntensity;
            RenderSettings.reflectionIntensity = previousReflectionIntensity;
        }

        private void EnsureTopDownLight()
        {
            if (topDownLight != null) return;

            Transform existing = transform.Find("TopDown_NeutralLight");
            GameObject lightObject;
            if (existing != null)
            {
                lightObject = existing.gameObject;
                topDownLight = lightObject.GetComponent<Light>();
            }
            else
            {
                lightObject = new GameObject("TopDown_NeutralLight");
                lightObject.transform.SetParent(transform, false);
                lightObject.transform.localPosition = Vector3.zero;
                lightObject.transform.localRotation = Quaternion.identity;
                topDownLight = lightObject.AddComponent<Light>();
            }

            topDownLight.type = LightType.Directional;
            topDownLight.shadows = LightShadows.None;
            topDownLight.color = Color.white;
            topDownLight.enabled = false;
        }

        private void OnDisable()
        {
            if (active)
                ExitTopDown();
        }

        private void OnDestroy()
        {
            if (active)
                ExitTopDown();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace MeraBrandPakistan
{
    /// <summary>
    /// Drives the seamless loop clock for MeraBrandPakistan/BackgroundLoop and
    /// MeraBrandPakistan/BackgroundLoopUI.
    ///
    /// Attach to whatever renders the background: a UI Image or RawImage, a
    /// SpriteRenderer, or a MeshRenderer on a quad. It works out which path to take.
    ///
    /// It also pushes _UVRect, the sub-rect of the texture this quad actually shows.
    /// The effect rects (flag, map, logo) are authored in 0..1 across the artwork, so
    /// without this they land in the wrong place the moment the sprite is atlassed.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class MBPBackgroundLoop : MonoBehaviour
    {
        [Tooltip("Seconds for one full loop. The animation returns to its exact starting " +
                 "frame at the end of every cycle, so any value is safe.")]
        [Min(1f)] public float loopDuration = 18f;

        [Tooltip("Start somewhere other than frame zero. Useful when two screens run the " +
                 "same background and you don't want them in lockstep.")]
        [Range(0f, 1f)] public float startOffset = 0f;

        [Tooltip("Unscaled time ignores Time.timeScale, so the background keeps moving " +
                 "while the game is paused.")]
        public bool useUnscaledTime = true;

        [Tooltip("Editor-only. Preview the loop without entering play mode.")]
        public bool animateInEditMode = true;

        static readonly int LoopTID  = Shader.PropertyToID("_LoopT");
        static readonly int UVRectID = Shader.PropertyToID("_UVRect");

        float _elapsed;
        Graphic _graphic;
        Renderer _renderer;
        Material _uiMaterial;               // runtime instance, owned by this component
        MaterialPropertyBlock _mpb;

        void OnEnable()
        {
            _graphic  = GetComponent<Graphic>();
            _renderer = GetComponent<Renderer>();

            if (_graphic != null)
            {
                // Graphic.material is the shared asset. Writing to it directly would edit
                // the material on disk, so instance it and hand the instance back.
                if (Application.isPlaying && _graphic.material != null)
                {
                    _uiMaterial = new Material(_graphic.material);
                    _uiMaterial.name = _graphic.material.name + " (loop instance)";
                    _graphic.material = _uiMaterial;
                }
            }
            else if (_renderer != null)
            {
                _mpb = new MaterialPropertyBlock();
            }
            else
            {
                Debug.LogWarning("[MBPBackgroundLoop] No Renderer or UI Graphic on " + name, this);
            }

            _elapsed = startOffset * loopDuration;
            PushUVRect();
        }

        void OnDisable()
        {
            if (_uiMaterial == null) return;
            if (Application.isPlaying) Destroy(_uiMaterial); else DestroyImmediate(_uiMaterial);
            _uiMaterial = null;
        }

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !animateInEditMode) return;
#endif
            float dt = Application.isPlaying
                ? (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime)
                : 1f / 60f;

            _elapsed += dt;
            if (_elapsed >= loopDuration) _elapsed -= loopDuration;   // exact wrap, no drift

            SetLoopT(_elapsed / loopDuration);
        }

        /// <summary>Scrub the loop manually, e.g. from a Timeline or a cutscene.</summary>
        public void SetLoopT(float t)
        {
            t = Mathf.Repeat(t, 1f);

            if (_uiMaterial != null)                 // UI, play mode
            {
                _uiMaterial.SetFloat(LoopTID, t);
            }
            else if (_graphic != null)               // UI, edit-mode preview
            {
                var m = _graphic.materialForRendering;
                if (m != null) m.SetFloat(LoopTID, t);
            }
            else if (_renderer != null)              // sprite / mesh
            {
                _renderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(LoopTID, t);
                _renderer.SetPropertyBlock(_mpb);
            }
        }

        /// <summary>Call after changing the sprite or the RawImage uvRect at runtime.</summary>
        public void PushUVRect()
        {
            Vector4 rect = new Vector4(0f, 0f, 1f, 1f);

            var raw = GetComponent<RawImage>();
            var img = GetComponent<Image>();
            var spr = GetComponent<SpriteRenderer>();

            if (raw != null)
            {
                var r = raw.uvRect;
                rect = new Vector4(r.x, r.y, r.width, r.height);
            }
            else if (img != null && img.sprite != null)
            {
                rect = NormalisedRect(img.sprite);
                if (img.type != Image.Type.Simple)
                {
                    Debug.LogWarning("[MBPBackgroundLoop] Image type is " + img.type +
                                     ". Use Simple with Full Rect, or the effect rects will not line up.", this);
                }
            }
            else if (spr != null && spr.sprite != null)
            {
                rect = NormalisedRect(spr.sprite);
            }

            ApplyVector(UVRectID, rect);
        }

        static Vector4 NormalisedRect(Sprite sprite)
        {
            if (sprite.packed && sprite.packingRotation != SpritePackingRotation.None)
            {
                Debug.LogWarning("[MBPBackgroundLoop] Sprite is packed with rotation. " +
                                 "Turn rotation off for this sprite in the atlas.");
            }

            Texture t = sprite.texture;
            Rect r = sprite.textureRect;
            return new Vector4(r.x / t.width, r.y / t.height, r.width / t.width, r.height / t.height);
        }

        void ApplyVector(int id, Vector4 value)
        {
            if (_uiMaterial != null) { _uiMaterial.SetVector(id, value); return; }

            if (_graphic != null)
            {
                var m = _graphic.materialForRendering;
                if (m != null) m.SetVector(id, value);
                return;
            }

            if (_renderer != null)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(_mpb);
                _mpb.SetVector(id, value);
                _renderer.SetPropertyBlock(_mpb);
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (isActiveAndEnabled) PushUVRect();
        }
#endif
    }
}

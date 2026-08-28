using System;
using MeraBrand.Expo.Booking;
using TMPro;
using UnityEngine;

namespace MeraBrand.Expo.Stalls
{
    public sealed class StallBookingVisual : MonoBehaviour
    {
        [SerializeField] private Material bookedMaterial;

        private GameObject bookedOverlay;
        private TextMeshPro exhibitorLabel;
        private SpriteRenderer logoRenderer;
        private Texture2D runtimeLogoTexture;
        private Sprite runtimeLogoSprite;
        private StallIdentity identity;

        private void Awake()
        {
            identity = GetComponent<StallIdentity>();
            EnsureVisualObjects();
        }

        public void Configure(Material material)
        {
            bookedMaterial = material;
            EnsureVisualObjects();
            ApplyMaterial();
        }

        public void Apply(StallBookingRecord record)
        {
            EnsureVisualObjects();
            bool booked = record != null && record.isBooked;

            if (bookedOverlay != null) bookedOverlay.SetActive(booked);
            if (exhibitorLabel != null)
            {
                exhibitorLabel.gameObject.SetActive(booked && !string.IsNullOrWhiteSpace(record.exhibitorName));
                exhibitorLabel.text = booked ? record.exhibitorName : string.Empty;
            }

            ApplyLogo(booked ? record?.logoReference : string.Empty);
        }

        private void ApplyLogo(string dataReference)
        {
            ClearRuntimeLogo();
            if (logoRenderer == null) return;
            logoRenderer.gameObject.SetActive(false);
            if (string.IsNullOrWhiteSpace(dataReference)) return;

            try
            {
                string base64 = dataReference;
                int comma = dataReference.IndexOf(',');
                if (dataReference.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
                    base64 = dataReference[(comma + 1)..];

                byte[] bytes = Convert.FromBase64String(base64);
                runtimeLogoTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!runtimeLogoTexture.LoadImage(bytes)) { ClearRuntimeLogo(); return; }

                runtimeLogoSprite = Sprite.Create(runtimeLogoTexture,
                    new Rect(0f, 0f, runtimeLogoTexture.width, runtimeLogoTexture.height),
                    new Vector2(0.5f, 0.5f), 100f);
                logoRenderer.sprite = runtimeLogoSprite;

                float maxWidth = Mathf.Max(4f, identity.FootprintUnityUnits.x * 0.45f);
                float maxHeight = 3.5f;
                Vector2 spriteSize = runtimeLogoSprite.bounds.size;
                float scale = Mathf.Min(maxWidth / Mathf.Max(0.01f, spriteSize.x), maxHeight / Mathf.Max(0.01f, spriteSize.y));
                logoRenderer.transform.localScale = Vector3.one * scale;
                logoRenderer.gameObject.SetActive(true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not display logo for {identity?.StallId}: {ex.Message}");
                ClearRuntimeLogo();
            }
        }

        private void ClearRuntimeLogo()
        {
            if (logoRenderer != null) logoRenderer.sprite = null;
            if (runtimeLogoSprite != null) Destroy(runtimeLogoSprite);
            if (runtimeLogoTexture != null) Destroy(runtimeLogoTexture);
            runtimeLogoSprite = null;
            runtimeLogoTexture = null;
        }

        private void EnsureVisualObjects()
        {
            identity ??= GetComponent<StallIdentity>();
            if (identity == null) return;

            Transform overlayTransform = transform.Find("Phase6_BookedOverlay");
            if (overlayTransform == null)
            {
                bookedOverlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bookedOverlay.name = "Phase6_BookedOverlay";
                bookedOverlay.transform.SetParent(transform, false);
                bookedOverlay.transform.localPosition = new Vector3(0f, 0.19f, 0f);
                bookedOverlay.transform.localScale = new Vector3(identity.FootprintUnityUnits.x + 0.2f, 0.10f, identity.FootprintUnityUnits.y + 0.2f);
                Collider collider = bookedOverlay.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }
            else bookedOverlay = overlayTransform.gameObject;

            Transform header = transform.Find("HeaderAnchor");
            Vector3 headerPosition = header != null
                ? header.localPosition
                : new Vector3(0f, identity.WallHeightMeters * 3.280839895f, identity.FootprintUnityUnits.y * 0.5f);

            Transform labelTransform = transform.Find("Phase6_ExhibitorName_TMP");
            if (labelTransform == null)
            {
                GameObject labelObject = new("Phase6_ExhibitorName_TMP");
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localPosition = headerPosition + new Vector3(0f, -0.6f, -0.08f);
                labelObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                exhibitorLabel = labelObject.AddComponent<TextMeshPro>();
                exhibitorLabel.alignment = TextAlignmentOptions.Center;
                exhibitorLabel.fontSize = 1.4f;
                exhibitorLabel.rectTransform.sizeDelta = new Vector2(Mathf.Max(8f, identity.FootprintUnityUnits.x * 0.85f), 3f);
            }
            else exhibitorLabel = labelTransform.GetComponent<TextMeshPro>();

            Transform logoTransform = transform.Find("Phase7_ExhibitorLogo");
            if (logoTransform == null)
            {
                GameObject logoObject = new("Phase7_ExhibitorLogo");
                logoObject.transform.SetParent(transform, false);
                logoObject.transform.localPosition = headerPosition + new Vector3(0f, 1.5f, -0.12f);
                logoObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                logoRenderer = logoObject.AddComponent<SpriteRenderer>();
                logoRenderer.sortingOrder = 5;
            }
            else logoRenderer = logoTransform.GetComponent<SpriteRenderer>();

            ApplyMaterial();
            bookedOverlay.SetActive(false);
            if (exhibitorLabel != null) exhibitorLabel.gameObject.SetActive(false);
            if (logoRenderer != null) logoRenderer.gameObject.SetActive(false);
        }

        private void ApplyMaterial()
        {
            if (bookedOverlay == null || bookedMaterial == null) return;
            Renderer renderer = bookedOverlay.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = bookedMaterial;
        }

        private void OnDestroy() => ClearRuntimeLogo();
    }
}

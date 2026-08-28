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

            if (bookedOverlay != null)
                bookedOverlay.SetActive(booked);

            if (exhibitorLabel != null)
            {
                exhibitorLabel.gameObject.SetActive(booked && !string.IsNullOrWhiteSpace(record.exhibitorName));
                exhibitorLabel.text = booked ? record.exhibitorName : string.Empty;
            }
        }

        private void EnsureVisualObjects()
        {
            identity ??= GetComponent<StallIdentity>();
            if (identity == null)
                return;

            Transform overlayTransform = transform.Find("Phase6_BookedOverlay");
            if (overlayTransform == null)
            {
                bookedOverlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bookedOverlay.name = "Phase6_BookedOverlay";
                bookedOverlay.transform.SetParent(transform, false);
                bookedOverlay.transform.localPosition = new Vector3(0f, 0.19f, 0f);
                bookedOverlay.transform.localScale = new Vector3(
                    identity.FootprintUnityUnits.x + 0.2f,
                    0.10f,
                    identity.FootprintUnityUnits.y + 0.2f);
                Collider collider = bookedOverlay.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);
            }
            else
            {
                bookedOverlay = overlayTransform.gameObject;
            }

            Transform labelTransform = transform.Find("Phase6_ExhibitorName_TMP");
            if (labelTransform == null)
            {
                GameObject labelObject = new("Phase6_ExhibitorName_TMP");
                labelObject.transform.SetParent(transform, false);
                Transform header = transform.Find("HeaderAnchor");
                labelObject.transform.localPosition = header != null
                    ? header.localPosition + new Vector3(0f, 0.25f, 0f)
                    : new Vector3(0f, identity.WallHeightMeters * 3.280839895f, identity.FootprintUnityUnits.y * 0.5f);
                labelObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                exhibitorLabel = labelObject.AddComponent<TextMeshPro>();
                exhibitorLabel.alignment = TextAlignmentOptions.Center;
                exhibitorLabel.fontSize = 1.4f;
                exhibitorLabel.rectTransform.sizeDelta = new Vector2(Mathf.Max(8f, identity.FootprintUnityUnits.x * 0.85f), 3f);
            }
            else
            {
                exhibitorLabel = labelTransform.GetComponent<TextMeshPro>();
            }

            ApplyMaterial();
            bookedOverlay.SetActive(false);
            if (exhibitorLabel != null)
                exhibitorLabel.gameObject.SetActive(false);
        }

        private void ApplyMaterial()
        {
            if (bookedOverlay == null || bookedMaterial == null)
                return;

            Renderer renderer = bookedOverlay.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = bookedMaterial;
        }
    }
}

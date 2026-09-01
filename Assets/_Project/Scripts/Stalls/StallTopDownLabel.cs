using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MeraBrand.Expo.Stalls
{
    [DisallowMultipleComponent]
    public sealed class StallTopDownLabel : MonoBehaviour
    {
        private static readonly HashSet<StallTopDownLabel> Instances = new();

        private const string LabelObjectName = "TopDown_StallNumber_TMP";

        private StallIdentity identity;
        private TextMeshPro label;

        private void Awake()
        {
            identity = GetComponent<StallIdentity>();
            EnsureLabel();
            SetVisible(false);
        }

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        public void Refresh()
        {
            identity ??= GetComponent<StallIdentity>();
            EnsureLabel();
            if (label == null || identity == null) return;

            label.text = GetNumber(identity.StallId);

            float width = Mathf.Max(4f, identity.FootprintUnityUnits.x * 0.75f);
            label.rectTransform.sizeDelta = new Vector2(width, 4f);

            float y = Mathf.Max(0.6f, identity.WallHeightMeters * 3.280839895f + 0.5f);
            label.transform.localPosition = new Vector3(0f, y, 0f);
        }

        public void SetVisible(bool visible)
        {
            EnsureLabel();
            if (label != null)
                label.gameObject.SetActive(visible);
        }

        public static void SetAllVisible(bool visible)
        {
            foreach (StallTopDownLabel item in Instances)
            {
                if (item != null)
                    item.SetVisible(visible);
            }
        }

        private void EnsureLabel()
        {
            identity ??= GetComponent<StallIdentity>();
            if (identity == null) return;

            Transform existing = transform.Find(LabelObjectName);
            if (existing != null)
            {
                label = existing.GetComponent<TextMeshPro>();
            }
            else
            {
                GameObject go = new(LabelObjectName);
                go.transform.SetParent(transform, false);
                label = go.AddComponent<TextMeshPro>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 2.5f;
                label.fontStyle = FontStyles.Bold;
                label.color = Color.black;
                label.enableAutoSizing = true;
                label.fontSizeMin = 1.4f;
                label.fontSizeMax = 3.2f;
                label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            RefreshTransformOnly();
        }

        private void RefreshTransformOnly()
        {
            if (label == null || identity == null) return;
            float y = Mathf.Max(0.6f, identity.WallHeightMeters * 3.280839895f + 0.5f);
            label.transform.localPosition = new Vector3(0f, y, 0f);
        }

        private static string GetNumber(string stallId)
        {
            if (string.IsNullOrWhiteSpace(stallId)) return "?";

            int dash = stallId.LastIndexOf('-');
            string value = dash >= 0 && dash < stallId.Length - 1
                ? stallId[(dash + 1)..]
                : stallId;

            return string.IsNullOrWhiteSpace(value) ? stallId : value;
        }
    }
}

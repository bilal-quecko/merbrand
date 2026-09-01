using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MeraBrand.Expo.Stalls
{
    public sealed class StallRegistry : MonoBehaviour
    {
        public static StallRegistry Instance { get; private set; }

        private readonly List<StallIdentity> stalls = new();
        private readonly Dictionary<string, StallIdentity> byId = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<StallIdentity> Stalls => stalls;
        public int Count => stalls.Count;
        public bool HasValidationErrors { get; private set; }

        private void Awake()
        {
            Instance = this;
            Rebuild();
        }

        public void Rebuild()
        {
            stalls.Clear();
            byId.Clear();
            HasValidationErrors = false;

            StallIdentity[] found = FindObjectsByType<StallIdentity>(FindObjectsSortMode.None);
            foreach (StallIdentity stall in found.OrderBy(s => s.StallId))
            {
                if (stall == null) continue;
                stalls.Add(stall);

                StallTopDownLabel topDownLabel = stall.GetComponent<StallTopDownLabel>();
                if (topDownLabel == null)
                    topDownLabel = stall.gameObject.AddComponent<StallTopDownLabel>();
                topDownLabel.Refresh();

                string id = stall.StallId;
                if (string.IsNullOrWhiteSpace(id) || id == "UNASSIGNED")
                {
                    Debug.LogError($"Stall Registry: '{stall.name}' has no valid Stall ID.");
                    HasValidationErrors = true;
                    continue;
                }

                if (!byId.TryAdd(id, stall))
                {
                    Debug.LogError($"Stall Registry: duplicate Stall ID '{id}' detected on '{stall.name}'.");
                    HasValidationErrors = true;
                }
            }
        }

        public bool TryGet(string stallId, out StallIdentity stall)
        {
            if (string.IsNullOrWhiteSpace(stallId))
            {
                stall = null;
                return false;
            }
            return byId.TryGetValue(stallId.Trim(), out stall);
        }
    }
}

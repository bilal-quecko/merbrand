using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MeraBrand.Expo.Stalls
{
    /// <summary>
    /// Keeps Unity stall identities aligned with the approved Tulip floor map.
    /// The scene contains legacy/shuffled serialized labels, so the authoritative
    /// map is applied from stall positions when the exhibition scene is loaded.
    /// </summary>
    public static class FloorMapStallNumbering
    {
        private const float PositionTolerance = 0.4f;

        private readonly struct Target
        {
            public readonly string Hall;
            public readonly float X;
            public readonly float Z;
            public readonly string Id;
            public readonly string Label;

            public Target(string hall, float x, float z, string id, string label)
            {
                Hall = hall;
                X = x;
                Z = z;
                Id = id;
                Label = label;
            }
        }

        private static readonly Target[] StandardTargets =
        {
            // Hall 1 - left run.
            S("Hall 1", 9.1f, 13.1f, 1), S("Hall 1", 9.1f, 23.21f, 2),
            S("Hall 1", 9.1f, 33.22f, 3), S("Hall 1", 9.1f, 43.52f, 4),
            S("Hall 1", 9.1f, 53.75f, 5), S("Hall 1", 9.1f, 64.01f, 6),
            S("Hall 1", 9.1f, 74.32f, 7), S("Hall 1", 9.1f, 84.53f, 8),
            S("Hall 1", 9.1f, 94.55f, 9),

            // Hall 1 - top-left row.
            S("Hall 1", 18.88f, 108.7f, 10), S("Hall 1", 29.09f, 108.7f, 11),
            S("Hall 1", 39.29f, 108.7f, 12), S("Hall 1", 49.54f, 108.7f, 13),
            S("Hall 1", 59.86f, 108.7f, 14), S("Hall 1", 70.11f, 108.7f, 15),

            // Hall 1 - top-right row.
            S("Hall 1", 98.5f, 108.7f, 16), S("Hall 1", 108.8f, 108.7f, 17),
            S("Hall 1", 119.13f, 108.7f, 18), S("Hall 1", 129.46f, 108.7f, 19),
            S("Hall 1", 139.68f, 108.7f, 20), S("Hall 1", 149.98f, 108.7f, 21),

            // Hall 1 - right run.
            // The supplied floor drawing contains S-22 twice and omits S-47.
            // The second S-22 physical slot is assigned S-47 so all 67 normal
            // stalls remain unique and the rest of the drawing stays unchanged.
            S("Hall 1", 160.6f, 98.84f, 22), S("Hall 1", 160.6f, 88.59f, 47),
            S("Hall 1", 160.6f, 78.37f, 23), S("Hall 1", 160.6f, 68.1f, 24),
            S("Hall 1", 160.6f, 57.85f, 25), S("Hall 1", 160.6f, 47.62f, 26),
            S("Hall 1", 160.6f, 37.46f, 27),

            // Hall 2 - left run.
            S("Hall 2", 193.91f, 35.72f, 28), S("Hall 2", 193.91f, 45.99f, 29),
            S("Hall 2", 193.91f, 56.31f, 30), S("Hall 2", 193.91f, 66.32f, 31),
            S("Hall 2", 193.91f, 76.45f, 32), S("Hall 2", 193.91f, 86.79f, 33),
            S("Hall 2", 193.91f, 96.9f, 34),

            // Hall 2 - top row.
            S("Hall 2", 211.5f, 103.1f, 35), S("Hall 2", 221.71f, 103.1f, 36),
            S("Hall 2", 232.13f, 103.1f, 37), S("Hall 2", 242.5f, 103.1f, 38),
            S("Hall 2", 252.79f, 103.1f, 39),

            // Hall 2 - right run.
            S("Hall 2", 259.2f, 83.2f, 40), S("Hall 2", 259.2f, 72.6f, 41),
            S("Hall 2", 259.2f, 61.8f, 42), S("Hall 2", 259.2f, 51f, 43),
            S("Hall 2", 259.2f, 40.2f, 44), S("Hall 2", 259.2f, 29.4f, 45),
            S("Hall 2", 259.2f, 18.8f, 46),

            // Hall 3 - bottom row, left to right on the supplied drawing.
            S("Hall 3", 20.63f, 122.2f, 53), S("Hall 3", 30.9f, 122.2f, 52),
            S("Hall 3", 41.16f, 122.2f, 51), S("Hall 3", 51.51f, 122.2f, 50),
            S("Hall 3", 61.75f, 122.2f, 49), S("Hall 3", 72.12f, 122.2f, 48),

            // Hall 3 - left run.
            S("Hall 3", 9.65f, 137.96f, 54),

            // S-55 is reserved for Gold Sponsor, so this normal-stall slot takes S-71.
            S("Hall 3", 9.65f, 148.37f, 71),

            // Hall 3 - inner row, left to right.
            S("Hall 3", 32.7f, 171.05f, 67), S("Hall 3", 42.96f, 171.05f, 66),
            S("Hall 3", 53.17f, 171.05f, 65), S("Hall 3", 63.29f, 171.05f, 64),
            S("Hall 3", 73.53f, 171.05f, 63),

            // Hall 3 - top row. Sponsor-reserved S-57 and S-59 are replaced
            // by the remaining normal IDs S-68 and S-69.
            S("Hall 3", 10.61f, 191.07f, 56), S("Hall 3", 20.71f, 191.07f, 68),
            S("Hall 3", 30.97f, 191.07f, 58), S("Hall 3", 41.16f, 191.07f, 69),
            S("Hall 3", 51.42f, 191.07f, 60), S("Hall 3", 61.81f, 191.07f, 61),
            S("Hall 3", 72.19f, 191.07f, 62)
        };

        private static readonly Target[] SilverTargets =
        {
            SL(31.4326f, 50.63f, 14), SL(50.3245f, 50.67f, 13),
            SL(118.565f, 50.12f, 2), SL(137.63f, 50.19f, 1),
            SL(31.29f, 60.61f, 15), SL(50.22f, 60.77f, 16),
            SL(118.3727f, 60.26f, 3), SL(137.4089f, 60.26f, 4),
            SL(29.7f, 79.7f, 10), SL(49.7f, 79.9f, 9),
            SL(118.9f, 79.7f, 6), SL(138.9905f, 79.7f, 5),
            SL(29.6f, 90.3f, 11), SL(49.6f, 90.3f, 12),
            SL(118.9f, 90.3f, 7), SL(138.7916f, 90.2f, 8)
        };

        private static readonly Target[] GoldTargets =
        {
            G(69.7f, 85.3f, 9), G(98.7f, 84.8f, 8),
            G(69.7f, 55.7f, 10), G(98.7f, 55.2f, 7),
            G(218.69f, 80.28999f, 6), G(238.57f, 80.28999f, 5),
            G(218.69f, 60.170002f, 3), G(238.57f, 60.07f, 4),
            G(218.92f, 40f, 1), G(238.5f, 40f, 2)
        };

        private static readonly Dictionary<string, Target> SponsorTargets =
            new(StringComparer.Ordinal)
            {
                ["H1-EXPO-SPONSOR"] = Sponsor(57),
                ["H1-MAIN-SPONSOR"] = Sponsor(70),
                ["H1-CO-SPONSOR"] = Sponsor(59),
                ["H1-GOLD-SPONSOR"] = Sponsor(55)
            };

        public static void ApplyToLoadedScene()
        {
            StallIdentity[] stalls = FindObjectsByType<StallIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (StallIdentity stall in stalls)
            {
                if (stall == null || stall.name.StartsWith("REF_", StringComparison.Ordinal))
                    continue;

                if (!TryResolve(stall, out Target target))
                    continue;

                stall.ApplyFloorMapIdentity(target.Id);
                ApplyVisibleLabel(stall, target.Label);
                stall.GetComponent<StallTopDownLabel>()?.Refresh();
            }
        }

        private static bool TryResolve(StallIdentity stall, out Target target)
        {
            if (SponsorTargets.TryGetValue(stall.name, out target))
                return true;

            Vector3 p = stall.transform.localPosition;

            if (stall.Size == StallSize.ThreeByThree)
                return TryFind(StandardTargets, stall.Hall, p, out target);

            if (stall.Size == StallSize.ThreeBySix &&
                string.Equals(stall.Hall, "Hall 1", StringComparison.OrdinalIgnoreCase))
                return TryFind(SilverTargets, "Hall 1", p, out target);

            if (stall.Size == StallSize.SixBySix)
                return TryFind(GoldTargets, string.Empty, p, out target, ignoreHall: true);

            target = default;
            return false;
        }

        private static bool TryFind(
            Target[] targets,
            string hall,
            Vector3 position,
            out Target target,
            bool ignoreHall = false)
        {
            foreach (Target item in targets)
            {
                if (!ignoreHall &&
                    !string.Equals(item.Hall, hall, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (Mathf.Abs(item.X - position.x) <= PositionTolerance &&
                    Mathf.Abs(item.Z - position.z) <= PositionTolerance)
                {
                    target = item;
                    return true;
                }
            }

            target = default;
            return false;
        }

        private static void ApplyVisibleLabel(StallIdentity stall, string label)
        {
            TextMeshPro[] labels = stall.GetComponentsInChildren<TextMeshPro>(true);
            foreach (TextMeshPro text in labels)
            {
                if (text == null || !LooksLikeStallCode(text.text))
                    continue;

                text.text = label;
            }
        }

        private static bool LooksLikeStallCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (value.StartsWith("SL_", StringComparison.OrdinalIgnoreCase))
                return int.TryParse(value.AsSpan(3), out _);

            if (value.StartsWith("S_", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("G_", StringComparison.OrdinalIgnoreCase))
                return int.TryParse(value.AsSpan(2), out _);

            return false;
        }

        private static Target S(string hall, float x, float z, int n) =>
            new(hall, x, z, $"S-{n:00}", $"S_{n:00}");

        private static Target SL(float x, float z, int n) =>
            new("Hall 1", x, z, $"SL-{n:00}", $"SL_{n:00}");

        private static Target G(float x, float z, int n) =>
            new(string.Empty, x, z, $"G-{n:00}", $"G_{n:00}");

        private static Target Sponsor(int n) =>
            new("Hall 1", 0f, 0f, $"S-{n:00}", $"S_{n:00}");
    }
}

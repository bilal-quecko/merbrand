using System.Collections.Generic;
using UnityEngine;

namespace MeraBrand.Expo.UI
{
    /// <summary>
    /// Central runtime state for interactive UI panels. While at least one panel owns the lock,
    /// camera controls are suspended and the mouse cursor remains visible/unlocked.
    /// </summary>
    public static class UIInteractionState
    {
        private static readonly HashSet<int> Owners = new();

        public static bool IsBlocked => Owners.Count > 0;

        public static void Acquire(Object owner)
        {
            if (owner == null)
                return;

            Owners.Add(owner.GetInstanceID());
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void Release(Object owner)
        {
            if (owner == null)
                return;

            Owners.Remove(owner.GetInstanceID());
        }

        public static void ClearAll()
        {
            Owners.Clear();
        }
    }
}

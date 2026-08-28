using UnityEngine;

namespace MeraBrand.Expo.Stalls
{
    public sealed class StallIdentity : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string stallId = "UNASSIGNED";
        [SerializeField] private string displayName = "Stall";
        [SerializeField] private string hall = "Hall 1";
        [SerializeField] private StallSize size = StallSize.ThreeByThree;

        [Header("Dimensions (feet / Unity units)")]
        [SerializeField] private Vector2 footprint = new(3f, 3f);

        [Header("Camera")]
        [SerializeField] private Transform visitPoint;
        [SerializeField] private Transform lookTarget;

        public string StallId => stallId;
        public string DisplayName => displayName;
        public string Hall => hall;
        public StallSize Size => size;
        public Vector2 Footprint => footprint;
        public Transform VisitPoint => visitPoint;
        public Transform LookTarget => lookTarget;

#if UNITY_EDITOR
        public void EditorConfigure(string id, string label, string hallName, StallSize stallSize, Vector2 dimensions)
        {
            stallId = id;
            displayName = label;
            hall = hallName;
            size = stallSize;
            footprint = dimensions;
        }

        public void EditorSetCameraAnchors(Transform visit, Transform look)
        {
            visitPoint = visit;
            lookTarget = look;
        }
#endif
    }
}

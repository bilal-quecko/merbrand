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

        [Header("Dimensions")]
        [Tooltip("Metric stall specification. X = frontage, Y = side/depth.")]
        [SerializeField] private Vector2 footprintMeters = new(3f, 3f);

        [Tooltip("Converted footprint used in the Unity scene. Project scale: 1 Unity unit = 1 foot.")]
        [SerializeField] private Vector2 footprintUnityUnits = new(9.84252f, 9.84252f);

        [SerializeField] private float wallHeightMeters = 2.5f;

        [Header("Camera")]
        [SerializeField] private Transform visitPoint;
        [SerializeField] private Transform lookTarget;

        public string StallId => stallId;
        public string DisplayName => displayName;
        public string Hall => hall;
        public StallSize Size => size;
        public Vector2 FootprintMeters => footprintMeters;
        public Vector2 FootprintUnityUnits => footprintUnityUnits;
        public float WallHeightMeters => wallHeightMeters;
        public Transform VisitPoint => visitPoint;
        public Transform LookTarget => lookTarget;

#if UNITY_EDITOR
        public void EditorConfigure(
            string id,
            string label,
            string hallName,
            StallSize stallSize,
            Vector2 dimensionsMeters,
            Vector2 dimensionsUnityUnits,
            float heightMeters)
        {
            stallId = id;
            displayName = label;
            hall = hallName;
            size = stallSize;
            footprintMeters = dimensionsMeters;
            footprintUnityUnits = dimensionsUnityUnits;
            wallHeightMeters = heightMeters;
        }

        public void EditorSetCameraAnchors(Transform visit, Transform look)
        {
            visitPoint = visit;
            lookTarget = look;
        }
#endif
    }
}

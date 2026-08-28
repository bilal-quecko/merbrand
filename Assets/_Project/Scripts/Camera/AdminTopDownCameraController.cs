using UnityEngine;
using UnityEngine.InputSystem;

namespace MeraBrand.Expo.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class AdminTopDownCameraController : MonoBehaviour
    {
        [SerializeField] private float panSpeed = 65f;
        [SerializeField] private float zoomSpeed = 8f;
        [SerializeField] private float minOrthographicSize = 25f;
        [SerializeField] private float maxOrthographicSize = 180f;

        private Camera controlledCamera;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            controlledCamera.orthographic = true;
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                Vector2 input = Vector2.zero;
                if (Keyboard.current.wKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                if (Keyboard.current.dKey.isPressed) input.x += 1f;
                if (Keyboard.current.aKey.isPressed) input.x -= 1f;

                Vector3 move = new(input.x, 0f, input.y);
                if (move.sqrMagnitude > 1f) move.Normalize();
                transform.position += move * panSpeed * Time.unscaledDeltaTime;
            }

            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    controlledCamera.orthographicSize -= Mathf.Sign(scroll) * zoomSpeed;
                    controlledCamera.orthographicSize = Mathf.Clamp(controlledCamera.orthographicSize, minOrthographicSize, maxOrthographicSize);
                }
            }
        }
    }
}

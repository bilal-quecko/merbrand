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
        [SerializeField] private float focusOrthographicSize = 28f;

        private Camera controlledCamera;

        public Camera ControlledCamera => controlledCamera != null ? controlledCamera : GetComponent<Camera>();

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            controlledCamera.orthographic = true;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
                return;

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

        public void FocusOn(Vector3 worldPosition, float orthographicSize = -1f)
        {
            controlledCamera ??= GetComponent<Camera>();
            Vector3 position = transform.position;
            position.x = worldPosition.x;
            position.z = worldPosition.z;
            transform.position = position;

            float targetSize = orthographicSize > 0f ? orthographicSize : focusOrthographicSize;
            controlledCamera.orthographicSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);
        }
    }
}

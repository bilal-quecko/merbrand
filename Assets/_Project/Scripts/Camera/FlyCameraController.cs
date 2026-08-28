using UnityEngine;
using UnityEngine.InputSystem;

namespace MeraBrand.Expo.CameraSystem
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FlyCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 18f;
        [SerializeField] private float boostMultiplier = 2.5f;
        [SerializeField] private float verticalSpeed = 12f;

        [Header("Look")]
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        [Header("Height Limits")]
        [SerializeField] private bool clampHeight = true;
        [SerializeField] private float minHeight = 2f;
        [SerializeField] private float maxHeight = 60f;

        [Header("Cursor")]
        [SerializeField] private bool lockCursorOnStart = true;

        private CharacterController controller;
        private float yaw;
        private float pitch;
        private bool cursorLocked;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        }

        private void Start()
        {
            SetCursorLocked(lockCursorOnStart);
        }

        private void Update()
        {
            HandleCursor();
            if (!cursorLocked)
                return;

            HandleLook();
            HandleMovement();
        }

        private void HandleCursor()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !cursorLocked)
                SetCursorLocked(true);
        }

        private void HandleLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * mouseSensitivity;
            pitch -= delta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void HandleMovement()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            float x = 0f;
            float z = 0f;
            float y = 0f;

            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed) z -= 1f;
            if (keyboard.wKey.isPressed) z += 1f;
            if (keyboard.qKey.isPressed) y += 1f;
            if (keyboard.eKey.isPressed) y -= 1f;

            Vector3 horizontal = (transform.right * x + transform.forward * z);
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude > 1f)
                horizontal.Normalize();

            float speed = moveSpeed;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                speed *= boostMultiplier;

            Vector3 motion = horizontal * speed + Vector3.up * (y * verticalSpeed);
            motion *= Time.deltaTime;

            if (clampHeight)
            {
                float targetY = Mathf.Clamp(transform.position.y + motion.y, minHeight, maxHeight);
                motion.y = targetY - transform.position.y;
            }

            controller.Move(motion);
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}

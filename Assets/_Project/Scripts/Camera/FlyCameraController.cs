using MeraBrand.Expo.UI;
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

        public bool CursorLocked => cursorLocked;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            SyncAnglesFromTransform();
        }

        private void Start()
        {
            SetCursorLocked(lockCursorOnStart && !UIInteractionState.IsBlocked);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || UIInteractionState.IsBlocked)
                return;

            if (!cursorLocked)
                return;

            HandleLook();
            HandleMovement();
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

            Vector3 horizontal = transform.right * x + transform.forward * z;
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

        public void SetCursorLocked(bool locked)
        {
            if (locked && UIInteractionState.IsBlocked)
                locked = false;

            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void SnapToPose(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null && wasEnabled)
                controller.enabled = false;

            transform.SetPositionAndRotation(position, rotation);
            SyncAnglesFromTransform();

            if (controller != null && wasEnabled)
                controller.enabled = true;
        }

        public void SnapToLookAt(Vector3 position, Vector3 target)
        {
            Vector3 direction = target - position;
            Quaternion rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : transform.rotation;

            SnapToPose(position, rotation);
        }

        private void SyncAnglesFromTransform()
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }
}

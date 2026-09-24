using MeraBrand.Expo.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MeraBrand.Expo.CameraSystem
{
    /// <summary>
    /// Runtime-only touch controls for the Web/mobile visitor experience.
    /// They are created only when Unity reports a touch-capable device.
    /// Desktop keyboard/mouse input remains unchanged.
    /// </summary>
    public sealed class MobileFlyControls : MonoBehaviour
    {
        public static MobileFlyControls Instance { get; private set; }

        private CameraModeManager cameraModeManager;
        private Canvas canvas;
        private Vector2 moveInput;
        private Vector2 lookDelta;
        private float verticalInput;

        public Vector2 MoveInput => moveInput;
        public float VerticalInput => verticalInput;
        public bool IsVisible => canvas != null && canvas.enabled && gameObject.activeInHierarchy;

        public static bool IsTouchInterfaceAvailable
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return Application.isMobilePlatform || Touchscreen.current != null;
#else
                return Application.isMobilePlatform || Touchscreen.current != null;
#endif
            }
        }

        public static MobileFlyControls Ensure(CameraModeManager modeManager)
        {
            if (!IsTouchInterfaceAvailable)
                return null;

            if (Instance != null)
            {
                Instance.cameraModeManager = modeManager;
                return Instance;
            }

            GameObject root = new("WebMobileControls");
            MobileFlyControls controls = root.AddComponent<MobileFlyControls>();
            controls.cameraModeManager = modeManager;
            controls.BuildUI();
            return controls;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (canvas == null)
                return;

            bool shouldShow =
                cameraModeManager != null &&
                cameraModeManager.CurrentMode != CameraMode.TopDown &&
                !UIInteractionState.IsBlocked &&
                Time.timeScale > 0f;

            if (canvas.enabled != shouldShow)
            {
                canvas.enabled = shouldShow;
                if (!shouldShow)
                    ResetInput();
            }
        }

        public Vector2 ConsumeLookDelta()
        {
            Vector2 value = lookDelta;
            lookDelta = Vector2.zero;
            return value;
        }

        public void SetMove(Vector2 value)
        {
            moveInput = Vector2.ClampMagnitude(value, 1f);
        }

        public void AddLookDelta(Vector2 delta)
        {
            lookDelta += delta;
        }

        public void SetVertical(float value)
        {
            verticalInput = Mathf.Clamp(value, -1f, 1f);
        }

        private void ResetInput()
        {
            moveInput = Vector2.zero;
            lookDelta = Vector2.zero;
            verticalInput = 0f;
        }

        private void BuildUI()
        {
            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CreateLookArea(canvasObject.transform);
            CreateJoystick(canvasObject.transform);
            CreateHoldButton(canvasObject.transform, "MoveUpButton", "UP", new Vector2(-120f, 205f), 1f);
            CreateHoldButton(canvasObject.transform, "MoveDownButton", "DOWN", new Vector2(-120f, 95f), -1f);
        }

        private void CreateLookArea(Transform parent)
        {
            GameObject go = new("LookArea", typeof(RectTransform), typeof(Image), typeof(MobileLookArea));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(1f, 0.84f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            go.GetComponent<MobileLookArea>().Configure(this);
        }

        private void CreateJoystick(Transform parent)
        {
            GameObject baseObject = new("MoveJoystick", typeof(RectTransform), typeof(Image), typeof(MobileJoystickArea));
            baseObject.transform.SetParent(parent, false);

            RectTransform baseRect = baseObject.GetComponent<RectTransform>();
            baseRect.anchorMin = new Vector2(0f, 0f);
            baseRect.anchorMax = new Vector2(0f, 0f);
            baseRect.pivot = new Vector2(0.5f, 0.5f);
            baseRect.anchoredPosition = new Vector2(170f, 170f);
            baseRect.sizeDelta = new Vector2(230f, 230f);

            Image baseImage = baseObject.GetComponent<Image>();
            baseImage.color = new Color(0.05f, 0.08f, 0.11f, 0.48f);

            GameObject knobObject = new("Knob", typeof(RectTransform), typeof(Image));
            knobObject.transform.SetParent(baseObject.transform, false);

            RectTransform knobRect = knobObject.GetComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.anchoredPosition = Vector2.zero;
            knobRect.sizeDelta = new Vector2(95f, 95f);

            Image knobImage = knobObject.GetComponent<Image>();
            knobImage.color = new Color(0.75f, 0.82f, 0.88f, 0.72f);
            knobImage.raycastTarget = false;

            baseObject.GetComponent<MobileJoystickArea>().Configure(this, knobRect);
        }

        private void CreateHoldButton(Transform parent, string name, string label, Vector2 positionFromBottomRight, float value)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(MobileVerticalButton));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = positionFromBottomRight;
            rect.sizeDelta = new Vector2(150f, 82f);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.05f, 0.08f, 0.11f, 0.58f);

            GameObject textObject = new("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textObject.transform.SetParent(go.transform, false);
            TMPro.TextMeshProUGUI text = textObject.GetComponent<TMPro.TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 28f;
            text.fontStyle = TMPro.FontStyles.Bold;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            go.GetComponent<MobileVerticalButton>().Configure(this, value);
        }
    }

    public sealed class MobileJoystickArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private MobileFlyControls owner;
        private RectTransform knob;
        private RectTransform area;

        public void Configure(MobileFlyControls controls, RectTransform knobTransform)
        {
            owner = controls;
            knob = knobTransform;
            area = transform as RectTransform;
        }

        public void OnPointerDown(PointerEventData eventData) => UpdateJoystick(eventData);
        public void OnDrag(PointerEventData eventData) => UpdateJoystick(eventData);

        public void OnPointerUp(PointerEventData eventData)
        {
            if (knob != null)
                knob.anchoredPosition = Vector2.zero;
            owner?.SetMove(Vector2.zero);
        }

        private void UpdateJoystick(PointerEventData eventData)
        {
            if (owner == null || area == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;

            float radius = Mathf.Max(1f, Mathf.Min(area.rect.width, area.rect.height) * 0.5f);
            Vector2 clamped = Vector2.ClampMagnitude(local, radius);

            if (knob != null)
                knob.anchoredPosition = clamped * 0.62f;

            owner.SetMove(clamped / radius);
        }
    }

    public sealed class MobileLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private MobileFlyControls owner;

        public void Configure(MobileFlyControls controls)
        {
            owner = controls;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Required so drag begins immediately on the first touch.
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner?.AddLookDelta(eventData.delta);
        }
    }

    public sealed class MobileVerticalButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private MobileFlyControls owner;
        private float value;

        public void Configure(MobileFlyControls controls, float direction)
        {
            owner = controls;
            value = direction;
        }

        public void OnPointerDown(PointerEventData eventData) => owner?.SetVertical(value);
        public void OnPointerUp(PointerEventData eventData) => owner?.SetVertical(0f);
        public void OnPointerExit(PointerEventData eventData) => owner?.SetVertical(0f);
    }
}

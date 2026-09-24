using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniShooterGame.UI
{
    /// <summary>
    /// Mobile virtual joystick supporting floating/dynamic touch-down positioning,
    /// handle clamping, deadzone filtering, and normalized 2D direction output.
    /// Implements Unity EventSystems pointer interfaces for multi-touch compatibility.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("UI Rect References")]
        [Tooltip("The parent touch interaction zone (e.g. left half of the screen). If null, defaults to this RectTransform.")]
        [SerializeField] private RectTransform touchZone;

        [Tooltip("The outer circular background/base of the joystick.")]
        [SerializeField] private RectTransform background;

        [Tooltip("The inner movable knob/handle of the joystick.")]
        [SerializeField] private RectTransform handle;

        [Header("Joystick Settings")]
        [Tooltip("Maximum distance (in canvas pixels) the handle can move from the center.")]
        [SerializeField] private float handleRange = 100f;

        [Tooltip("Normalized deadzone threshold below which input is ignored (0.0 to 1.0).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float deadZone = 0.05f;

        [Tooltip("If enabled, the joystick background centers on the exact touch position on pointer down.")]
        [SerializeField] private bool isDynamicFloating = true;

        [Tooltip("If enabled, hides the joystick visuals when not in use.")]
        [SerializeField] private bool hideOnRelease = false;

        [Header("Visual Feedback (Optional)")]
        [Tooltip("Optional CanvasGroup to control fade/visibility of the joystick.")]
        [SerializeField] private CanvasGroup visualCanvasGroup;

        // Runtime state
        private Canvas _parentCanvas;
        private Camera _uiCamera;
        private Vector2 _defaultBackgroundPos;
        private Vector2 _inputVector;

        /// <summary>
        /// Normalized movement direction input (-1 to 1 on both axes).
        /// </summary>
        public Vector2 Input => _inputVector;

        /// <summary>
        /// Horizontal axis input value (-1 to 1).
        /// </summary>
        public float Horizontal => _inputVector.x;

        /// <summary>
        /// Vertical axis input value (-1 to 1).
        /// </summary>
        public float Vertical => _inputVector.y;

        /// <summary>
        /// True if the user is currently touching and controlling the joystick.
        /// </summary>
        public bool IsActive { get; private set; }

        private void Awake()
        {
            if (touchZone == null)
            {
                touchZone = GetComponent<RectTransform>();
            }

            _parentCanvas = GetComponentInParent<Canvas>();
            if (_parentCanvas == null)
            {
                Debug.LogError("[VirtualJoystick] No parent Canvas found! Joystick must be placed within a UI Canvas.", this);
            }

            if (background != null)
            {
                _defaultBackgroundPos = background.anchoredPosition;
            }

            UpdateVisualVisibility(false);
        }

        private void Start()
        {
            CacheUICamera();
        }

        private void CacheUICamera()
        {
            if (_parentCanvas != null)
            {
                _uiCamera = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _parentCanvas.worldCamera;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsActive = true;
            CacheUICamera();

            if (isDynamicFloating && background != null && touchZone != null)
            {
                // Position background at the exact screen touch point converted to touchZone local space
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(touchZone, eventData.position, _uiCamera, out Vector2 localPoint))
                {
                    background.anchoredPosition = localPoint;
                }
            }

            UpdateVisualVisibility(true);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || handle == null) return;

            // Calculate position relative to background center
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, _uiCamera, out Vector2 localPoint))
            {
                float distance = localPoint.magnitude;
                Vector2 direction = distance > 0.0001f ? localPoint / distance : Vector2.zero;

                // Clamp handle visual position
                float clampedDist = Mathf.Min(distance, handleRange);
                handle.anchoredPosition = direction * clampedDist;

                // Calculate normalized input with deadzone
                float normalizedMagnitude = Mathf.Clamp01(distance / handleRange);
                if (normalizedMagnitude < deadZone)
                {
                    _inputVector = Vector2.zero;
                }
                else
                {
                    // Remap (deadZone..1) -> (0..1) for smooth input start
                    float smoothMag = (normalizedMagnitude - deadZone) / (1f - deadZone);
                    _inputVector = direction * smoothMag;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsActive = false;
            _inputVector = Vector2.zero;

            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }

            if (isDynamicFloating && background != null)
            {
                background.anchoredPosition = _defaultBackgroundPos;
            }

            UpdateVisualVisibility(false);
        }

        private void UpdateVisualVisibility(bool isTouching)
        {
            if (visualCanvasGroup != null)
            {
                if (hideOnRelease)
                {
                    visualCanvasGroup.alpha = isTouching ? 1f : 0f;
                    visualCanvasGroup.blocksRaycasts = isTouching;
                }
                else
                {
                    visualCanvasGroup.alpha = isTouching ? 1f : 0.6f;
                }
            }
        }
    }
}

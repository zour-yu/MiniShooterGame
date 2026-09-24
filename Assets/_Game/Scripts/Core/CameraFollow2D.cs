using System.Collections;
using UnityEngine;
using MiniShooterGame.Player;

namespace MiniShooterGame.Core
{
    /// <summary>
    /// Smooth 2D top-down camera controller with target tracking, look-ahead dampening,
    /// optional arena bounding box clamping, and screen shake support for combat impacts.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Zoom & Viewport")]
        [Tooltip("Orthographic camera size. Lower values zoom in closer (e.g. 3.2 to 3.8 for intense mobile action).")]
        [Range(2f, 10f)]
        [SerializeField] private float cameraZoom = 3.5f;

        [Header("Target Tracking")]
        [Tooltip("The target Transform to follow (e.g. Player). If null, automatically locates the Player.")]
        [SerializeField] private Transform target;

        [Tooltip("Positional offset relative to the target.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        [Tooltip("Rough time it takes to reach the target. Lower values make camera follow tighter.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float smoothTime = 0.15f;

        [Header("Look-Ahead (Optional)")]
        [Tooltip("If enabled, shifts camera slightly in the direction the player is moving/facing.")]
        [SerializeField] private bool enableLookAhead = true;

        [Tooltip("Distance the camera looks ahead in the moving direction.")]
        [SerializeField] private float lookAheadDistance = 1.0f;

        [Tooltip("Damping for the look-ahead smoothing.")]
        [SerializeField] private float lookAheadSmoothTime = 0.2f;

        [Header("Arena Boundaries (Optional)")]
        [Tooltip("Clamp camera movement within specified 2D arena bounds.")]
        [SerializeField] private bool clampToBounds = true;

        [SerializeField] private Vector2 minBounds = new Vector2(-2.4f, -5.1f);
        [SerializeField] private Vector2 maxBounds = new Vector2(2.4f, 5.1f);

        // Runtime caching
        private Camera _cam;
        private Vector3 _currentVelocity;
        private Vector3 _lookAheadPos;
        private Vector3 _lookAheadVelocity;
        private Vector3 _shakeOffset;
        private Coroutine _shakeCoroutine;
        private PlayerMovement _playerMovement;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            ApplyZoom();

            if (target == null)
            {
                FindPlayerTarget();
            }
        }

        private void OnValidate()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (_cam != null)
            {
                _cam.orthographic = true;
                _cam.orthographicSize = cameraZoom;
            }
        }

        private void Start()
        {
            if (target != null)
            {
                // Snap camera immediately to target on start
                transform.position = target.position + offset;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindPlayerTarget();
                if (target == null) return;
            }

            Vector3 targetDestination = target.position + offset;

            // Handle look-ahead
            if (enableLookAhead && _playerMovement != null)
            {
                Vector2 moveVelocity = _playerMovement.Velocity;
                Vector3 desiredLookAhead = (Vector3)(moveVelocity.normalized * lookAheadDistance);

                _lookAheadPos = Vector3.SmoothDamp(_lookAheadPos, desiredLookAhead, ref _lookAheadVelocity, lookAheadSmoothTime);
                targetDestination += _lookAheadPos;
            }

            // Smoothly damp towards target position
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetDestination, ref _currentVelocity, smoothTime);

            // Maintain correct Z depth
            smoothedPosition.z = offset.z;

            // Optional bounds clamping
            if (clampToBounds)
            {
                smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
                smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minBounds.y, maxBounds.y);
            }

            // Apply position with shake offset
            transform.position = smoothedPosition + _shakeOffset;
        }

        /// <summary>
        /// Attempts to locate the Player in the scene.
        /// </summary>
        public void FindPlayerTarget()
        {
            _playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
            if (_playerMovement != null)
            {
                target = _playerMovement.transform;
            }
            else
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    target = playerObj.transform;
                    _playerMovement = playerObj.GetComponent<PlayerMovement>();
                }
            }
        }

        /// <summary>
        /// Triggers a screen shake effect (e.g. on player hit, explosion, or recoil).
        /// </summary>
        /// <param name="duration">Duration of shake in seconds.</param>
        /// <param name="magnitude">Intensity of displacement.</param>
        public void TriggerShake(float duration = 0.2f, float magnitude = 0.3f)
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }
            _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);
                _shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * strength,
                    Random.Range(-1f, 1f) * strength,
                    0f
                );

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _shakeOffset = Vector3.zero;
            _shakeCoroutine = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!clampToBounds) return;

            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0f);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}

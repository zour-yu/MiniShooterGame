using System.Collections;
using TMPro;
using UnityEngine;

namespace MiniShooterGame.UI
{
    /// <summary>
    /// World-space floating damage number popup.
    /// Manages upward drift, punch scaling, color coding (normal vs critical),
    /// alpha fading, and automatic destruction.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class FloatingText : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Lifetime of the damage number popup in seconds.")]
        [SerializeField] private float lifetime = 0.7f;

        [Tooltip("Initial upward drift velocity.")]
        [SerializeField] private float upwardSpeed = 1.8f;

        [Tooltip("Random horizontal drift range (+/-).")]
        [SerializeField] private float horizontalSpread = 0.4f;

        [Header("Styling & Colors")]
        [Tooltip("Text color for standard damage hits.")]
        [SerializeField] private Color normalColor = new Color(1f, 0.92f, 0.23f, 1f); // Vibrant Yellow (#FFE082 / #FFEB3B)

        [Tooltip("Text color for critical damage hits.")]
        [SerializeField] private Color criticalColor = new Color(1f, 0.18f, 0.45f, 1f); // Neon Hot Red/Pink (#FF2D55)

        [Tooltip("Scale multiplier for critical hits.")]
        [SerializeField] private float criticalScaleMultiplier = 1.4f;

        [Header("Sorting")]
        [Tooltip("Sorting Order to ensure text renders in front of all 2D sprites.")]
        [SerializeField] private int sortingOrder = 100;

        private TextMeshPro _textMesh;
        private Vector3 _velocity;

        private void Awake()
        {
            _textMesh = GetComponent<TextMeshPro>();

            // Ensure sorting order is high so it draws on top of all sprite renderers
            if (TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                meshRenderer.sortingOrder = sortingOrder;
            }
        }

        /// <summary>
        /// Initializes the floating text with damage amount and critical hit styling.
        /// </summary>
        /// <param name="damageAmount">Damage number to display.</param>
        /// <param name="isCritical">Whether this hit was a critical strike.</param>
        public void Setup(float damageAmount, bool isCritical = false)
        {
            if (_textMesh == null)
            {
                _textMesh = GetComponent<TextMeshPro>();
            }

            // Format damage display (e.g. "10" or "10.5")
            _textMesh.text = damageAmount % 1 == 0 ? damageAmount.ToString("F0") : damageAmount.ToString("F1");

            // Apply color and scale based on hit type
            Color baseColor = isCritical ? criticalColor : normalColor;
            _textMesh.color = baseColor;

            float baseScale = isCritical ? criticalScaleMultiplier : 1.0f;
            transform.localScale = Vector3.one * (baseScale * 0.6f);

            // Compute initial trajectory with random horizontal variance
            float randomX = Random.Range(-horizontalSpread, horizontalSpread);
            _velocity = new Vector3(randomX, upwardSpeed, 0f);

            StartCoroutine(AnimateAndFadeRoutine(baseColor, baseScale));
        }

        private IEnumerator AnimateAndFadeRoutine(Color startColor, float targetScale)
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / lifetime;

                // 1. Position Drift (slows down slightly over time)
                transform.position += _velocity * Time.deltaTime;
                _velocity.y = Mathf.Max(0.2f, _velocity.y - (Time.deltaTime * 1.5f));

                // 2. Punch Scale animation (fast pop to 1.2x target scale in first 15%, then settles)
                if (progress < 0.15f)
                {
                    float punchFactor = Mathf.Lerp(0.5f, 1.2f, progress / 0.15f);
                    transform.localScale = Vector3.one * (targetScale * punchFactor);
                }
                else if (progress < 0.3f)
                {
                    float settleFactor = Mathf.Lerp(1.2f, 1.0f, (progress - 0.15f) / 0.15f);
                    transform.localScale = Vector3.one * (targetScale * settleFactor);
                }

                // 3. Alpha Fadeout during the latter half of the lifetime
                if (progress > 0.4f)
                {
                    float alphaFadeProgress = (progress - 0.4f) / 0.6f;
                    Color fadedColor = startColor;
                    fadedColor.a = Mathf.Lerp(1f, 0f, alphaFadeProgress);
                    _textMesh.color = fadedColor;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

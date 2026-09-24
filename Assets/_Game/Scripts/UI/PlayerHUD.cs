using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MiniShooterGame.Core;
using MiniShooterGame.Player;

namespace MiniShooterGame.UI
{
    /// <summary>
    /// Cyber-styled Player HUD manager.
    /// Manages dynamic health bar fill with smooth catchup, low-health vignette pulsing,
    /// dash cooldown radial sweep, and wave announcement banners.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Health Bar")]
        [Tooltip("Foreground health fill Image (Image Type must be 'Filled').")]
        [SerializeField] private Image healthFillImage;

        [Tooltip("Secondary trailing catchup fill image for visual impact.")]
        [SerializeField] private Image healthCatchupImage;

        [Tooltip("Text displaying current HP numbers (e.g. '85 / 100').")]
        [SerializeField] private TextMeshProUGUI healthText;

        [Tooltip("Gradient for health color (Green -> Yellow -> Red).")]
        [SerializeField] private Gradient healthGradient;

        [Header("Dash Cooldown Indicator")]
        [Tooltip("Radial overlay Image on Dash button (Image Type 'Filled', Fill Method 'Radial 360').")]
        [SerializeField] private Image dashCooldownRadialFill;

        [Header("Wave & Objective Displays")]
        [Tooltip("Header text displaying the active wave (e.g. 'WAVE 1').")]
        [SerializeField] private TextMeshProUGUI waveTitleText;

        [Tooltip("Text displaying enemies remaining in the wave.")]
        [SerializeField] private TextMeshProUGUI enemiesRemainingText;

        [Header("Low Health Warning (Optional)")]
        [Tooltip("Full-screen red vignette overlay that pulses when HP is low.")]
        [SerializeField] private CanvasGroup lowHealthVignette;

        [SerializeField] private float lowHealthThreshold = 0.3f; // 30%

        private PlayerHealth _playerHealth;
        private PlayerDash _playerDash;
        private WaveSpawner _waveSpawner;
        private float _targetHealthPercent = 1f;

        private void Start()
        {
            FindReferences();
            InitializeGradient();
            SubscribeEvents();
        }

        private void FindReferences()
        {
            if (_playerHealth == null) _playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
            if (_playerDash == null) _playerDash = Object.FindFirstObjectByType<PlayerDash>();
            if (_waveSpawner == null) _waveSpawner = Object.FindFirstObjectByType<WaveSpawner>();
        }

        private void InitializeGradient()
        {
            if (healthGradient == null || healthGradient.colorKeys.Length == 0)
            {
                healthGradient = new Gradient();
                var colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.2f, 0.2f), 0.0f), // Red
                    new GradientColorKey(new Color(1f, 0.85f, 0.1f), 0.5f), // Amber
                    new GradientColorKey(new Color(0.1f, 1f, 0.5f), 1.0f)  // Emerald Cyan
                };
                var alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                };
                healthGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        private void SubscribeEvents()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged.AddListener(UpdateHealthUI);
                UpdateHealthUI(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
            }

            if (_waveSpawner != null)
            {
                _waveSpawner.OnWaveStarted.AddListener(OnWaveStarted);
                _waveSpawner.OnEnemyCountChanged.AddListener(OnEnemyCountChanged);
                OnWaveStarted(_waveSpawner.CurrentWaveNumber, $"Wave {_waveSpawner.CurrentWaveNumber}");
            }
        }

        private void Update()
        {
            // Smooth health catchup bar interpolation
            if (healthCatchupImage != null && healthFillImage != null)
            {
                healthCatchupImage.fillAmount = Mathf.Lerp(healthCatchupImage.fillAmount, healthFillImage.fillAmount, Time.deltaTime * 3.5f);
            }

            // Update Dash Cooldown Radial Fill
            if (dashCooldownRadialFill != null && _playerDash != null)
            {
                dashCooldownRadialFill.fillAmount = _playerDash.CooldownProgress;
            }

            // Pulse Low Health Vignette
            if (lowHealthVignette != null)
            {
                if (_targetHealthPercent <= lowHealthThreshold && _targetHealthPercent > 0f)
                {
                    float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
                    lowHealthVignette.alpha = Mathf.Lerp(0.2f, 0.65f, pulse);
                }
                else
                {
                    lowHealthVignette.alpha = 0f;
                }
            }
        }

        private void UpdateHealthUI(float currentHealth, float maxHealth)
        {
            _targetHealthPercent = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = _targetHealthPercent;
                healthFillImage.color = healthGradient.Evaluate(_targetHealthPercent);
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
            }
        }

        private void OnWaveStarted(int waveNumber, string waveName)
        {
            if (waveTitleText != null)
            {
                waveTitleText.text = $"<color=#00E5FF>WAVE {waveNumber}</color>";
            }
        }

        private void OnEnemyCountChanged(int alive, int remainingToSpawn)
        {
            if (enemiesRemainingText != null)
            {
                int totalLeft = alive + remainingToSpawn;
                enemiesRemainingText.text = $"ENEMIES: <color=#FF3366>{totalLeft}</color>";
            }
        }
    }
}

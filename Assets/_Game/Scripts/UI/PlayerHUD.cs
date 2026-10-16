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

        [Header("Weapon & Ammo Display")]
        [Tooltip("Text displaying current weapon name (e.g. 'PISTOL').")]
        [SerializeField] private TextMeshProUGUI weaponNameText;

        [Tooltip("Text displaying remaining ammo (e.g. '24 / 24' or '∞').")]
        [SerializeField] private TextMeshProUGUI ammoCountText;

        private PlayerHealth _playerHealth;
        private PlayerDash _playerDash;
        private WaveSpawner _waveSpawner;
        private PlayerShooter _playerShooter;
        private float _targetHealthPercent = 1f;

        private void Start()
        {
            FindReferences();
            InitializeGradient();
            ApplyConceptArtTheme();
            SubscribeEvents();
        }

        private void FindReferences()
        {
            if (_playerHealth == null) _playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
            if (_playerDash == null) _playerDash = Object.FindFirstObjectByType<PlayerDash>();
            if (_waveSpawner == null) _waveSpawner = WaveSpawner.Instance != null ? WaveSpawner.Instance : Object.FindAnyObjectByType<WaveSpawner>();
            if (_playerShooter == null) _playerShooter = Object.FindFirstObjectByType<PlayerShooter>();
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
            else
            {
                UpdateHealthUI(100f, 100f);
            }

            if (_waveSpawner != null)
            {
                _waveSpawner.OnWaveStarted.AddListener(OnWaveStarted);
                _waveSpawner.OnWaveCompleted.AddListener(OnWaveCompleted);
                _waveSpawner.OnEnemyCountChanged.AddListener(OnEnemyCountChanged);
                OnWaveStarted(_waveSpawner.CurrentWaveNumber, $"Wave {_waveSpawner.CurrentWaveNumber}");
                OnEnemyCountChanged(_waveSpawner.ActiveEnemyCount, _waveSpawner.CurrentWaveTargetTotal);
            }
            else
            {
                OnWaveStarted(1, "Wave 1");
                OnEnemyCountChanged(0, 6);
            }

            if (_playerShooter != null)
            {
                _playerShooter.OnWeaponEquipped += OnWeaponEquipped;
                _playerShooter.OnAmmoUpdated += OnAmmoUpdated;
                if (_playerShooter.CurrentWeapon != null)
                {
                    OnWeaponEquipped(_playerShooter.CurrentWeapon, _playerShooter.CurrentAmmo);
                }
                else
                {
                    OnAmmoUpdated(-1, -1);
                }
            }
            else
            {
                if (weaponNameText != null) weaponNameText.text = "PISTOL";
                OnAmmoUpdated(-1, -1);
            }
        }

        private void OnDestroy()
        {
            if (_waveSpawner != null)
            {
                _waveSpawner.OnWaveStarted.RemoveListener(OnWaveStarted);
                _waveSpawner.OnWaveCompleted.RemoveListener(OnWaveCompleted);
                _waveSpawner.OnEnemyCountChanged.RemoveListener(OnEnemyCountChanged);
            }

            if (_playerShooter != null)
            {
                _playerShooter.OnWeaponEquipped -= OnWeaponEquipped;
                _playerShooter.OnAmmoUpdated -= OnAmmoUpdated;
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

        private void ApplyConceptArtTheme()
        {
            var healthFrame = Resources.Load<Sprite>("UI/Health_Frame_Cyan");
            var healthFill = Resources.Load<Sprite>("UI/Health_Fill_Lime");
            var weaponFrame = Resources.Load<Sprite>("UI/Weapon_Frame_Cyan");

            Transform healthContainer = transform.Find("Health_Container");
            if (healthContainer != null)
            {
                var img = healthContainer.GetComponent<Image>();
                if (img != null && healthFrame != null)
                {
                    img.sprite = healthFrame;
                    img.color = Color.white;
                }
            }

            if (healthFillImage != null && healthFill != null)
            {
                healthFillImage.sprite = healthFill;
                healthFillImage.color = Color.white;
            }

            Transform weaponContainer = transform.Find("Weapon_Container");
            if (weaponContainer != null)
            {
                var img = weaponContainer.GetComponent<Image>();
                if (img != null && weaponFrame != null)
                {
                    img.sprite = weaponFrame;
                    img.color = Color.white;
                }
            }

            Transform waveContainer = transform.Find("Wave_Header_Container");
            if (waveContainer != null)
            {
                Transform sub = waveContainer.Find("Weapon_Drop_Subtitle");
                if (sub == null)
                {
                    GameObject subObj = new GameObject("Weapon_Drop_Subtitle");
                    subObj.transform.SetParent(waveContainer, false);
                    var rt = subObj.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, -0.45f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    var tmp = subObj.AddComponent<TextMeshProUGUI>();
                    tmp.text = "<color=#38BDF8>WEAPON DROP: COMBAT SHOTGUN</color>";
                    tmp.fontSize = 14f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.raycastTarget = false;
                }
            }
        }

        private void UpdateHealthUI(float currentHealth, float maxHealth)
        {
            _targetHealthPercent = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = _targetHealthPercent;
                // If using custom lime gradient sprite, keep color white
                if (healthFillImage.sprite != null && healthFillImage.sprite.name.Contains("Lime"))
                {
                    healthFillImage.color = Color.white;
                }
                else
                {
                    healthFillImage.color = healthGradient.Evaluate(_targetHealthPercent);
                }
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
                waveTitleText.text = $"<color=#00F0FF>WAVE {waveNumber}</color>";
            }
        }

        private void OnWaveCompleted(int waveNumber)
        {
            if (waveTitleText != null)
            {
                waveTitleText.text = "<color=#00F0FF>WAVE CLEARED!</color>";
            }
            if (enemiesRemainingText != null)
            {
                enemiesRemainingText.text = "ENEMIES: <color=#00F0FF>0</color>";
            }
        }

        private void OnEnemyCountChanged(int alive, int remainingToSpawn)
        {
            if (enemiesRemainingText != null)
            {
                int totalLeft = alive + remainingToSpawn;
                enemiesRemainingText.text = $"ENEMIES: <color=#FF2A6D>{totalLeft}</color>";
            }
        }

        private void OnWeaponEquipped(Combat.WeaponDataSO weapon, int currentAmmo)
        {
            if (weaponNameText != null && weapon != null)
            {
                weaponNameText.text = weapon.weaponName.ToUpper();
            }
            OnAmmoUpdated(currentAmmo, weapon != null ? weapon.maxAmmo : -1);
        }

        private void OnAmmoUpdated(int currentAmmo, int maxAmmo)
        {
            if (ammoCountText != null)
            {
                if (maxAmmo <= 0)
                {
                    ammoCountText.text = "<color=#00F0FF>∞</color>";
                }
                else
                {
                    string colorHex = currentAmmo <= 5 ? "#FF2A6D" : "#FFD700";
                    ammoCountText.text = $"<color={colorHex}>{currentAmmo}</color> / {maxAmmo}";
                }
            }
        }
    }
}

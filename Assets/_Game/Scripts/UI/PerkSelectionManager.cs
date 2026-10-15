using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniShooterGame.Core;
using MiniShooterGame.Player;

namespace MiniShooterGame.UI
{
    public enum PerkType
    {
        MaxHealth,
        FireRate,
        MoveSpeed,
        DashCooldown,
        DamageBoost
    }

    [Serializable]
    public class PerkOption
    {
        public PerkType type;
        public string title;
        public string description;
        public string iconSymbol;
        public Color accentColor;
    }

    /// <summary>
    /// Post-wave rogue-lite perk selection system.
    /// Displays 3 distinct perk cards when a wave completes, pausing the game
    /// and allowing the player to select a tactical upgrade before resuming.
    /// </summary>
    public class PerkSelectionManager : MonoBehaviour
    {
        [Header("Perk Pool")]
        [SerializeField] private List<PerkOption> perkPool = new List<PerkOption>();

        [Header("Modal UI Container")]
        [Tooltip("The root UI GameObject of the perk selection modal.")]
        [SerializeField] private GameObject modalRoot;

        [Tooltip("CanvasGroup for smooth fade-in.")]
        [SerializeField] private CanvasGroup modalCanvasGroup;

        [Header("Perk Card Slots (3 Required)")]
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private TextMeshProUGUI[] cardTitleTexts;
        [SerializeField] private TextMeshProUGUI[] cardDescriptionTexts;
        [SerializeField] private TextMeshProUGUI[] cardIconTexts;
        [SerializeField] private Image[] cardBackgrounds;

        private WaveSpawner _waveSpawner;
        private PlayerHealth _playerHealth;
        private PlayerMovement _playerMovement;
        private PlayerShooter _playerShooter;
        private PlayerDash _playerDash;

        private readonly List<PerkOption> _currentOptions = new List<PerkOption>();

        private void Awake()
        {
            InitializeDefaultPool();
            FindPlayerReferences();

            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        private void Start()
        {
            _waveSpawner = UnityEngine.Object.FindFirstObjectByType<WaveSpawner>();
            if (_waveSpawner != null)
            {
                _waveSpawner.OnWaveCompleted.AddListener(OnWaveCompleted);
            }
        }

        private void OnDestroy()
        {
            if (_waveSpawner != null)
            {
                _waveSpawner.OnWaveCompleted.RemoveListener(OnWaveCompleted);
            }
        }

        private void InitializeDefaultPool()
        {
            if (perkPool != null && perkPool.Count > 0) return;

            perkPool = new List<PerkOption>
            {
                new PerkOption
                {
                    type = PerkType.MaxHealth,
                    title = "Vitality Surge",
                    description = "+35 Max Health and full HP restore.",
                    iconSymbol = "♥",
                    accentColor = new Color(0.1f, 0.95f, 0.45f)
                },
                new PerkOption
                {
                    type = PerkType.FireRate,
                    title = "Rapid Trigger",
                    description = "+25% Faster Attack Rate permanently.",
                    iconSymbol = "⚡",
                    accentColor = new Color(1f, 0.85f, 0.1f)
                },
                new PerkOption
                {
                    type = PerkType.MoveSpeed,
                    title = "Swift Stride",
                    description = "+20% Movement Speed across all terrain.",
                    iconSymbol = "👟",
                    accentColor = new Color(0f, 0.9f, 1f)
                },
                new PerkOption
                {
                    type = PerkType.DashCooldown,
                    title = "Flash Step",
                    description = "-0.4s Dash Cooldown for nimble evasions.",
                    iconSymbol = "💨",
                    accentColor = new Color(0.85f, 0.4f, 1f)
                },
                new PerkOption
                {
                    type = PerkType.DamageBoost,
                    title = "High-Caliber",
                    description = "+25% Projectile Damage on all weapons.",
                    iconSymbol = "💥",
                    accentColor = new Color(1f, 0.3f, 0.3f)
                }
            };
        }

        private void FindPlayerReferences()
        {
            if (_playerHealth == null) _playerHealth = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            if (_playerMovement == null) _playerMovement = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
            if (_playerShooter == null) _playerShooter = UnityEngine.Object.FindFirstObjectByType<PlayerShooter>();
            if (_playerDash == null) _playerDash = UnityEngine.Object.FindFirstObjectByType<PlayerDash>();
        }

        private void OnWaveCompleted(int waveNumber)
        {
            // Do not show on the very final wave if game is won
            if (_waveSpawner != null && _waveSpawner.CurrentWaveNumber >= _waveSpawner.TotalWaves) return;

            PresentPerks();
        }

        /// <summary>
        /// Selects 3 distinct perks and displays the modal dialog, pausing gameplay.
        /// </summary>
        public void PresentPerks()
        {
            FindPlayerReferences();

            _currentOptions.Clear();
            List<PerkOption> available = new List<PerkOption>(perkPool);

            int countToPick = Mathf.Min(3, available.Count);
            for (int i = 0; i < countToPick; i++)
            {
                int idx = UnityEngine.Random.Range(0, available.Count);
                _currentOptions.Add(available[idx]);
                available.RemoveAt(idx);
            }

            // Populate Card Slots
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (i < _currentOptions.Count)
                {
                    PerkOption perk = _currentOptions[i];
                    cardButtons[i].gameObject.SetActive(true);

                    if (cardTitleTexts != null && i < cardTitleTexts.Length && cardTitleTexts[i] != null)
                        cardTitleTexts[i].text = perk.title;

                    if (cardDescriptionTexts != null && i < cardDescriptionTexts.Length && cardDescriptionTexts[i] != null)
                        cardDescriptionTexts[i].text = perk.description;

                    if (cardIconTexts != null && i < cardIconTexts.Length && cardIconTexts[i] != null)
                    {
                        cardIconTexts[i].text = perk.iconSymbol;
                        cardIconTexts[i].color = perk.accentColor;
                    }

                    if (cardBackgrounds != null && i < cardBackgrounds.Length && cardBackgrounds[i] != null)
                    {
                        cardBackgrounds[i].color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
                    }

                    int slotIndex = i;
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => SelectPerk(slotIndex));
                }
                else
                {
                    cardButtons[i].gameObject.SetActive(false);
                }
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            // Pause game for selection
            Time.timeScale = 0f;
        }

        private void SelectPerk(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= _currentOptions.Count) return;

            PerkOption chosen = _currentOptions[optionIndex];
            ApplyPerk(chosen);

            // Resume gameplay
            Time.timeScale = 1f;

            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            Vector3 spawnPos = _playerHealth != null ? _playerHealth.transform.position + Vector3.up * 1.0f : Vector3.zero;
            FloatingText.Spawn(spawnPos, $"<color=#{ColorUtility.ToHtmlStringRGB(chosen.accentColor)}>★ {chosen.title.ToUpper()}!</color>", 2.2f);
            Debug.Log($"[PerkSelectionManager] Selected Perk: {chosen.title} ({chosen.type})");
        }

        private void ApplyPerk(PerkOption perk)
        {
            switch (perk.type)
            {
                case PerkType.MaxHealth:
                    if (_playerHealth != null) _playerHealth.IncreaseMaxHealth(35f, true);
                    break;

                case PerkType.FireRate:
                    if (_playerShooter != null) _playerShooter.ModifyBaseFireRate(1.25f);
                    break;

                case PerkType.MoveSpeed:
                    if (_playerMovement != null) _playerMovement.ModifyMoveSpeed(1.20f);
                    break;

                case PerkType.DashCooldown:
                    if (_playerDash != null) _playerDash.ReduceCooldown(0.4f);
                    break;

                case PerkType.DamageBoost:
                    if (_playerShooter != null) _playerShooter.ModifyDamageMultiplier(1.25f);
                    break;
            }
        }
    }
}

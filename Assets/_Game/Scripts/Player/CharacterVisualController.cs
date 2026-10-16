using UnityEngine;
using MiniShooterGame.Combat;

namespace MiniShooterGame.Player
{
    public enum CharacterFacing
    {
        Down, // Front
        Up,   // Back
        Side  // Left / Right
    }

    /// <summary>
    /// Animates 2D chibi top-down characters (Killjoy) across 4 directions
    /// with step cycles, idle breathing, and a modular equippable gun mount.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterVisualController : MonoBehaviour
    {
        [Header("Chibi Sprites - Front / Down")]
        [SerializeField] private Sprite idleFront;
        [SerializeField] private Sprite walkFront1;
        [SerializeField] private Sprite walkFront2;
        [SerializeField] private Sprite holdGunFront;

        [Header("Chibi Sprites - Side")]
        [SerializeField] private Sprite idleSide;
        [SerializeField] private Sprite walkSide;
        [SerializeField] private Sprite holdGunSide;

        [Header("Chibi Sprites - Back / Up")]
        [SerializeField] private Sprite idleBack;
        [SerializeField] private Sprite walkBack;
        [SerializeField] private Sprite holdGunBack;

        [Header("Equippable Weapon Mount")]
        [Tooltip("Transform of the gun mount attached to the avatar.")]
        [SerializeField] private Transform gunMount;

        [Tooltip("SpriteRenderer on the gun mount.")]
        [SerializeField] private SpriteRenderer gunRenderer;

        [Tooltip("Default placeholder gun sprite until new weapon assets are attached.")]
        [SerializeField] private Sprite defaultPlaceholderGun;

        [Header("Animation Tuning")]
        [Tooltip("Walk animation cycle speed in steps per second.")]
        [SerializeField] private float walkStepRate = 7.5f;

        [Tooltip("Idle breathing bob speed.")]
        [SerializeField] private float idleBreathSpeed = 3.5f;

        private SpriteRenderer _bodyRenderer;
        private PlayerMovement _playerMovement;
        private PlayerShooter _playerShooter;

        private CharacterFacing _facing = CharacterFacing.Down;
        private bool _isFacingLeft = false;
        private float _animTimer = 0f;
        private Vector3 _baseGunLocalPos = new Vector3(0.56f, 0.69f, 0f);
        private float _recoilTimer = 0f;
        private float _combatTimer = 0f;

        public CharacterFacing CurrentFacing => _facing;
        public bool IsFacingLeft => _isFacingLeft;

        private void OnValidate()
        {
            EnsureSpritesLoaded();
        }

        private void Awake()
        {
            _bodyRenderer = GetComponent<SpriteRenderer>();
            _playerMovement = GetComponent<PlayerMovement>();
            _playerShooter = GetComponent<PlayerShooter>();

            EnsureSpritesLoaded();

            if (gunMount == null)
            {
                gunMount = transform.Find("Weapon_Mount");
                if (gunMount == null)
                {
                    GameObject gm = new GameObject("Weapon_Mount");
                    gm.transform.SetParent(transform, false);
                    gunMount = gm.transform;
                }
            }

            if (gunMount != null)
            {
                if (gunRenderer == null)
                {
                    gunRenderer = gunMount.GetComponent<SpriteRenderer>();
                    if (gunRenderer == null) gunRenderer = gunMount.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (gunRenderer != null && defaultPlaceholderGun != null)
            {
                gunRenderer.sprite = defaultPlaceholderGun;
            }

            if (_bodyRenderer != null && idleFront != null)
            {
                _bodyRenderer.sprite = idleFront;
                _bodyRenderer.sortingOrder = 150;
                _bodyRenderer.color = Color.white;
            }
        }

        private void EnsureSpritesLoaded()
        {
            bool IsInvalid(Sprite s)
            {
                if (s == null) return true;
                string n = s.name.ToLower();
                return n.Contains("card") || n.Contains("icon") || n.Contains("padlock") || n.Contains("health") || n.Contains("frame") || n.Contains("stride") || n.Contains("flash") || n.Contains("trigger");
            }

            if (IsInvalid(idleFront)) idleFront = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_Idle_Front");
            if (IsInvalid(walkFront1)) walkFront1 = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_Walk_Front_1");
            if (IsInvalid(walkFront2)) walkFront2 = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_Walk_Front_2");
            if (IsInvalid(holdGunFront)) holdGunFront = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_HoldGun_Front");
            if (IsInvalid(idleSide)) idleSide = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_Side_Idle");
            if (IsInvalid(walkSide)) walkSide = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_Side_Walk");
            if (IsInvalid(holdGunSide)) holdGunSide = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_HoldGun_Side");
            if (IsInvalid(idleBack)) idleBack = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_Back_Idle");
            if (IsInvalid(walkBack)) walkBack = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_Back_Walk");
            if (IsInvalid(holdGunBack)) holdGunBack = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_HoldGun_Back");
            if (IsInvalid(defaultPlaceholderGun))
            {
                defaultPlaceholderGun = Resources.Load<Sprite>("Weapons/Weapon_Pistol");
                if (defaultPlaceholderGun == null)
                {
                    defaultPlaceholderGun = Resources.Load<Sprite>("Characters/Killjoy/Gun_Pistol_Placeholder");
                }
            }
        }

        private void Start()
        {
            if (_playerShooter != null)
            {
                _playerShooter.OnWeaponEquipped += HandleWeaponEquipped;
            }
        }

        private void OnDestroy()
        {
            if (_playerShooter != null)
            {
                _playerShooter.OnWeaponEquipped -= HandleWeaponEquipped;
            }
        }

        private void HandleWeaponEquipped(WeaponDataSO weapon, int currentAmmo)
        {
            if (gunRenderer == null) return;

            // If a custom weapon sprite/icon is assigned in the WeaponDataSO, equip it!
            if (weapon != null && weapon.weaponIcon != null)
            {
                gunRenderer.sprite = weapon.weaponIcon;
            }
            else if (defaultPlaceholderGun != null)
            {
                gunRenderer.sprite = defaultPlaceholderGun;
            }
        }

        private void Update()
        {
            if (_combatTimer > 0f)
            {
                _combatTimer -= Time.deltaTime;
            }

            DetermineFacingDirection();
            UpdateBodyAnimation();
            UpdateGunMount();
        }

        private void DetermineFacingDirection()
        {
            Vector2 aimDir = Vector2.zero;

            // 1. If an enemy is actively targeted, aim in target's direction
            if (_playerShooter != null && _playerShooter.CurrentTarget != null)
            {
                aimDir = (Vector2)_playerShooter.CurrentTarget.position - (Vector2)transform.position;
                _combatTimer = 0.8f;
            }
            // 2. Otherwise use movement input direction
            else if (_playerMovement != null && _playerMovement.CurrentInput.sqrMagnitude > 0.05f)
            {
                aimDir = _playerMovement.CurrentInput;
            }

            if (aimDir.sqrMagnitude < 0.001f) return;

            aimDir.Normalize();

            // Determine if horizontal movement dominates or vertical
            if (Mathf.Abs(aimDir.x) > Mathf.Abs(aimDir.y) * 0.75f)
            {
                _facing = CharacterFacing.Side;
                _isFacingLeft = aimDir.x < 0f;
            }
            else
            {
                if (aimDir.y > 0f)
                {
                    _facing = CharacterFacing.Up;
                }
                else
                {
                    _facing = CharacterFacing.Down;
                }
            }
        }

        private void UpdateBodyAnimation()
        {
            bool isMoving = _playerMovement != null && _playerMovement.IsMoving;
            bool isCombatHold = (_playerShooter != null && _playerShooter.CurrentTarget != null) || _combatTimer > 0f;

            if (isMoving)
            {
                _animTimer += Time.deltaTime * walkStepRate;
            }
            else
            {
                _animTimer += Time.deltaTime * idleBreathSpeed;
            }

            int stepFrame = Mathf.FloorToInt(_animTimer) % 2;

            // Sprite selection based on facing, movement, and combat hold
            Sprite selectedSprite = null;

            switch (_facing)
            {
                case CharacterFacing.Down:
                    _bodyRenderer.flipX = false;
                    if (isCombatHold)
                    {
                        selectedSprite = holdGunFront != null ? holdGunFront : idleFront;
                    }
                    else if (isMoving)
                    {
                        selectedSprite = stepFrame == 0 ? walkFront1 : walkFront2;
                    }
                    else
                    {
                        selectedSprite = idleFront;
                    }
                    break;

                case CharacterFacing.Up:
                    _bodyRenderer.flipX = false;
                    if (isCombatHold)
                    {
                        selectedSprite = holdGunBack != null ? holdGunBack : idleBack;
                    }
                    else if (isMoving)
                    {
                        selectedSprite = stepFrame == 0 ? walkBack : idleBack;
                    }
                    else
                    {
                        selectedSprite = idleBack;
                    }
                    break;

                case CharacterFacing.Side:
                    _bodyRenderer.flipX = _isFacingLeft;
                    if (isCombatHold)
                    {
                        selectedSprite = holdGunSide != null ? holdGunSide : idleSide;
                    }
                    else if (isMoving)
                    {
                        selectedSprite = stepFrame == 0 ? walkSide : idleSide;
                    }
                    else
                    {
                        selectedSprite = idleSide;
                    }
                    break;
            }

            if (selectedSprite != null && _bodyRenderer.sprite != selectedSprite)
            {
                _bodyRenderer.sprite = selectedSprite;
            }

            // Subtle, cute breathing bobbing or movement step bobbing
            if (isMoving)
            {
                float stepBob = Mathf.Abs(Mathf.Sin(_animTimer * Mathf.PI)) * 0.03f;
                transform.localScale = new Vector3(1f, 1f + stepBob, 1f);
            }
            else
            {
                float breath = Mathf.Sin(_animTimer) * 0.02f;
                transform.localScale = new Vector3(1f, 1f + breath, 1f);
            }
        }

        private void UpdateGunMount()
        {
            if (gunMount == null || gunRenderer == null) return;

            Vector2 aimDir = Vector2.right;
            bool isCombatHold = (_playerShooter != null && _playerShooter.CurrentTarget != null) || _combatTimer > 0f;

            if (_playerShooter != null && _playerShooter.CurrentTarget != null)
            {
                aimDir = ((Vector2)_playerShooter.CurrentTarget.position - (Vector2)gunMount.position).normalized;
            }
            else if (_playerMovement != null && _playerMovement.CurrentInput.sqrMagnitude > 0.05f)
            {
                aimDir = _playerMovement.CurrentInput.normalized;
            }
            else
            {
                // Idle aim direction matching facing
                switch (_facing)
                {
                    case CharacterFacing.Side:
                        aimDir = _isFacingLeft ? Vector2.left : Vector2.right;
                        break;
                    case CharacterFacing.Up:
                        aimDir = Vector2.up;
                        break;
                    default:
                        aimDir = Vector2.down;
                        break;
                }
            }

            // Aim angle in degrees
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            gunMount.rotation = Quaternion.Euler(0f, 0f, angle);

            // Flip gun vertically when aiming towards the left so it doesn't appear upside down
            bool aimingLeft = Mathf.Abs(angle) > 90f;
            gunRenderer.flipY = aimingLeft;

            // Offset gun mount to sit accurately on Killjoy's hands
            Vector3 targetLocalPos;
            if (_facing == CharacterFacing.Side)
            {
                // Hand in HoldGun_Side is at (0.56, 0.69)
                targetLocalPos = new Vector3(_isFacingLeft ? -0.56f : 0.56f, 0.69f, 0f);
            }
            else if (_facing == CharacterFacing.Down)
            {
                // Hand in HoldGun_Front is centered at (0.0, 0.70)
                targetLocalPos = new Vector3(aimingLeft ? -0.2f : 0.2f, 0.70f, 0f);
            }
            else
            {
                // Hand in HoldGun_Back is centered at (0.0, 0.75)
                targetLocalPos = new Vector3(aimingLeft ? -0.2f : 0.2f, 0.75f, 0f);
            }

            // Micro-recoil kickback
            if (_recoilTimer > 0f)
            {
                _recoilTimer -= Time.deltaTime;
                targetLocalPos -= (Vector3)aimDir * 0.12f;
            }

            gunMount.localPosition = Vector3.Lerp(gunMount.localPosition, targetLocalPos, Time.deltaTime * 24f);

            // Sorting order: behind character body when facing UP, in front when facing DOWN/SIDE
            if (_facing == CharacterFacing.Up)
            {
                gunRenderer.sortingOrder = _bodyRenderer.sortingOrder - 2;
            }
            else
            {
                gunRenderer.sortingOrder = _bodyRenderer.sortingOrder + 2;
            }
        }

        /// <summary>
        /// Triggers visual gun recoil punch upon firing a bullet.
        /// </summary>
        public void TriggerRecoil()
        {
            _recoilTimer = 0.08f;
            _combatTimer = 1.0f;
        }

        /// <summary>
        /// Equips a new gun sprite on the chibi avatar.
        /// </summary>
        public void EquipGun(Sprite newGunSprite)
        {
            if (gunRenderer != null && newGunSprite != null)
            {
                gunRenderer.sprite = newGunSprite;
            }
        }
    }
}

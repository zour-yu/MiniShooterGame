using UnityEngine;
using MiniShooterGame.Combat;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

        [Header("Hand Sockets & Attachment")]
        [Tooltip("Transform of main trigger hand mounted to the weapon.")]
        [SerializeField] private Transform mainHandMount;

        [Tooltip("SpriteRenderer of main trigger hand.")]
        [SerializeField] private SpriteRenderer mainHandRenderer;

        [Tooltip("Sprite for main trigger hand (Killjoy tactical glove with sleeve cuff).")]
        [SerializeField] private Sprite mainHandSprite;

        [Tooltip("Transform of secondary support hand mounted to the weapon (for 2-handed weapons).")]
        [SerializeField] private Transform supportHandMount;

        [Tooltip("SpriteRenderer of secondary support hand.")]
        [SerializeField] private SpriteRenderer supportHandRenderer;

        [Tooltip("Sprite for secondary support hand (Killjoy cupped glove).")]
        [SerializeField] private Sprite supportHandSprite;

        [Tooltip("Whether to display hands holding the gun.")]
        [SerializeField] private bool showHands = true;

        [Header("Animation Tuning")]
        [Tooltip("Walk animation cycle speed in steps per second.")]
        [SerializeField] private float walkStepRate = 7.5f;

        [Tooltip("Idle breathing bob speed.")]
        [SerializeField] private float idleBreathSpeed = 3.5f;

        private SpriteRenderer _bodyRenderer;
        private PlayerMovement _playerMovement;
        private PlayerShooter _playerShooter;
        private PlayerDash _playerDash;

        private CharacterFacing _facing = CharacterFacing.Down;
        private bool _isFacingLeft = false;
        private float _animTimer = 0f;
        private Vector3 _baseGunLocalPos = new Vector3(0.56f, 0.69f, 0f);
        private float _recoilTimer = 0f;
        private float _combatTimer = 0f;

        public CharacterFacing CurrentFacing => _facing;
        public bool IsFacingLeft => _isFacingLeft;
        public SpriteRenderer GunRenderer => gunRenderer;
        public Transform MainHandMount => mainHandMount;
        public Transform SupportHandMount => supportHandMount;

        private void OnValidate()
        {
            EnsureSpritesLoaded();
        }

        private void Awake()
        {
            _bodyRenderer = GetComponent<SpriteRenderer>();
            _playerMovement = GetComponent<PlayerMovement>();
            _playerShooter = GetComponent<PlayerShooter>();
            _playerDash = GetComponent<PlayerDash>();

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
                // Ensure a dedicated Weapon_Visual child exists on the gun mount pivot
                Transform gunVisual = gunMount.Find("Weapon_Visual");
                if (gunVisual == null)
                {
                    GameObject gvObj = new GameObject("Weapon_Visual");
                    gvObj.transform.SetParent(gunMount, false);
                    gunVisual = gvObj.transform;
                }

                SpriteRenderer visualSr = gunVisual.GetComponent<SpriteRenderer>();
                if (visualSr == null) visualSr = gunVisual.gameObject.AddComponent<SpriteRenderer>();

                if (gunRenderer != null && gunRenderer.transform == gunMount)
                {
                    if (gunRenderer.sprite != null) visualSr.sprite = gunRenderer.sprite;
                    visualSr.sortingOrder = gunRenderer.sortingOrder;
                    visualSr.color = gunRenderer.color;
                    gunRenderer.enabled = false;
                }

                gunRenderer = visualSr;

                // Setup Main Hand
                if (mainHandMount == null)
                {
                    mainHandMount = gunMount.Find("Main_Hand");
                    if (mainHandMount == null)
                    {
                        GameObject mhObj = new GameObject("Main_Hand");
                        mhObj.transform.SetParent(gunMount, false);
                        mainHandMount = mhObj.transform;
                    }
                }

                if (mainHandMount != null)
                {
                    if (mainHandRenderer == null) mainHandRenderer = mainHandMount.GetComponent<SpriteRenderer>();
                    if (mainHandRenderer == null) mainHandRenderer = mainHandMount.gameObject.AddComponent<SpriteRenderer>();
                    if (mainHandRenderer != null && mainHandSprite != null) mainHandRenderer.sprite = mainHandSprite;
                }

                // Setup Support Hand
                if (supportHandMount == null)
                {
                    supportHandMount = gunMount.Find("Support_Hand");
                    if (supportHandMount == null)
                    {
                        GameObject shObj = new GameObject("Support_Hand");
                        shObj.transform.SetParent(gunMount, false);
                        supportHandMount = shObj.transform;
                    }
                }

                if (supportHandMount != null)
                {
                    if (supportHandRenderer == null) supportHandRenderer = supportHandMount.GetComponent<SpriteRenderer>();
                    if (supportHandRenderer == null) supportHandRenderer = supportHandMount.gameObject.AddComponent<SpriteRenderer>();
                    if (supportHandRenderer != null && supportHandSprite != null) supportHandRenderer.sprite = supportHandSprite;
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
            if (IsInvalid(mainHandSprite))
            {
                mainHandSprite = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_Hand_Main_WithCuff");
                if (mainHandSprite == null) mainHandSprite = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_Hand_Main");
            }
            if (IsInvalid(supportHandSprite))
            {
                supportHandSprite = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_Hand_Support");
            }
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
                if (_playerShooter.CurrentWeapon != null)
                {
                    HandleWeaponEquipped(_playerShooter.CurrentWeapon, _playerShooter.CurrentAmmo);
                }
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

            if (weapon == null)
            {
                if (defaultPlaceholderGun != null) gunRenderer.sprite = defaultPlaceholderGun;
                if (supportHandMount != null) supportHandMount.gameObject.SetActive(false);
                return;
            }

            Sprite weaponSprite = weapon.weaponIcon;

            // Resilient fallback: attempt loading by name from Resources if weaponIcon reference is missing
            if (weaponSprite == null)
            {
                string cleanName = weapon.weaponName.Replace(" ", "");
                weaponSprite = Resources.Load<Sprite>($"Weapons/Weapon_{cleanName}");

                if (weaponSprite == null)
                {
                    if (weapon.weaponName.IndexOf("Shotgun", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        weaponSprite = Resources.Load<Sprite>("Weapons/Weapon_Shotgun");
                    else if (weapon.weaponName.IndexOf("Rifle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        weaponSprite = Resources.Load<Sprite>("Weapons/Weapon_AssaultRifle");
                    else if (weapon.weaponName.IndexOf("Pistol", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        weaponSprite = Resources.Load<Sprite>("Weapons/Weapon_Pistol");
                }
            }

            if (weaponSprite != null)
            {
                gunRenderer.sprite = weaponSprite;
                Debug.Log($"[CharacterVisualController] Equipped weapon visual: {weapon.weaponName} -> {weaponSprite.name}");
            }
            else if (defaultPlaceholderGun != null)
            {
                gunRenderer.sprite = defaultPlaceholderGun;
            }

            // Configure two-handed support hand visibility
            bool isTwoHanded = weapon.isTwoHanded ||
                               weapon.weaponName.IndexOf("Shotgun", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                               weapon.weaponName.IndexOf("Rifle", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (supportHandMount != null)
            {
                supportHandMount.gameObject.SetActive(showHands && isTwoHanded);
            }

            if (mainHandMount != null)
            {
                mainHandMount.gameObject.SetActive(showHands);
            }
        }


        private void Update()
        {
            if (_combatTimer > 0f)
            {
                _combatTimer -= Time.deltaTime;
            }

            Vector2 aimDir = GetAimDirection();
            DetermineFacingDirection(aimDir);
            UpdateBodyAnimation(aimDir);
            UpdateGunMount(aimDir);
        }

        /// <summary>
        /// Resolves 360-degree aim vector from active target, mouse cursor (Code Monkey style),
        /// movement direction, or natural low-ready rest stance.
        /// </summary>
        public Vector2 GetAimDirection()
        {
            // 1. If actively targeting an enemy in range, lock onto the target
            if (_playerShooter != null && _playerShooter.CurrentTarget != null)
            {
                Vector2 dirToTarget = (Vector2)_playerShooter.CurrentTarget.position - (Vector2)transform.position;
                if (dirToTarget.sqrMagnitude > 0.001f)
                {
                    _combatTimer = 0.8f;
                    return dirToTarget.normalized;
                }
            }

            // 2. Desktop Mouse Aiming (Code Monkey style: 360 aim follows world mouse cursor)
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 mouseScreen = Vector3.zero;
                bool hasMouse = false;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    mouseScreen = Mouse.current.position.ReadValue();
                    hasMouse = true;
                }
#else
                if (Input.mousePresent)
                {
                    mouseScreen = Input.mousePosition;
                    hasMouse = true;
                }
#endif
                if (hasMouse)
                {
                    Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
                    Vector2 dirToMouse = (Vector2)mouseWorld - (Vector2)transform.position;
                    // If mouse is moved away from character origin, track cursor
                    if (dirToMouse.sqrMagnitude > 0.25f)
                    {
                        return dirToMouse.normalized;
                    }
                }
            }

            // 3. Fallback to movement input direction
            if (_playerMovement != null && _playerMovement.CurrentInput.sqrMagnitude > 0.05f)
            {
                return _playerMovement.CurrentInput.normalized;
            }

            // 4. Natural low-ready / rest stance (never aim straight down into chin!)
            return GetIdleRestDirection();
        }

        private Vector2 GetIdleRestDirection()
        {
            switch (_facing)
            {
                case CharacterFacing.Side:
                    return (_isFacingLeft ? new Vector2(-1f, -0.3f) : new Vector2(1f, -0.3f)).normalized;
                case CharacterFacing.Up:
                    return (_isFacingLeft ? new Vector2(-0.5f, 0.85f) : new Vector2(0.5f, 0.85f)).normalized;
                default:
                    return (_isFacingLeft ? new Vector2(-0.7f, -0.7f) : new Vector2(0.7f, -0.7f)).normalized;
            }
        }

        private void DetermineFacingDirection(Vector2 aimDir)
        {
            if (aimDir.sqrMagnitude < 0.001f) return;

            // Decoupled aiming: character faces towards aim angle
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
                _isFacingLeft = aimDir.x < 0f;
            }
        }

        private void UpdateBodyAnimation(Vector2 aimDir)
        {
            bool isDashing = _playerDash != null && _playerDash.IsDashing;
            bool isMoving = _playerMovement != null && _playerMovement.IsMoving;
            bool isCombatHold = (_playerShooter != null && _playerShooter.CurrentTarget != null) || _combatTimer > 0f;

            if (isDashing || isMoving)
            {
                _animTimer += Time.deltaTime * walkStepRate;
            }
            else
            {
                _animTimer += Time.deltaTime * idleBreathSpeed;
            }

            int stepFrame = Mathf.FloorToInt(_animTimer) % 2;

            // Sprite selection based on facing, movement, and strafe
            Sprite selectedSprite = null;

            switch (_facing)
            {
                case CharacterFacing.Down:
                    _bodyRenderer.flipX = false;
                    if (isDashing)
                    {
                        selectedSprite = walkFront1 != null ? walkFront1 : holdGunFront;
                    }
                    else if (isMoving)
                    {
                        selectedSprite = stepFrame == 0 ? walkFront1 : walkFront2;
                    }
                    else if (isCombatHold)
                    {
                        selectedSprite = holdGunFront != null ? holdGunFront : idleFront;
                    }
                    else
                    {
                        selectedSprite = idleFront;
                    }
                    break;

                case CharacterFacing.Up:
                    _bodyRenderer.flipX = false;
                    if (isDashing)
                    {
                        selectedSprite = walkBack != null ? walkBack : holdGunBack;
                    }
                    else if (isMoving)
                    {
                        selectedSprite = stepFrame == 0 ? walkBack : idleBack;
                    }
                    else if (isCombatHold)
                    {
                        selectedSprite = holdGunBack != null ? holdGunBack : idleBack;
                    }
                    else
                    {
                        selectedSprite = idleBack;
                    }
                    break;

                case CharacterFacing.Side:
                    _bodyRenderer.flipX = _isFacingLeft;
                    if (isDashing)
                    {
                        selectedSprite = walkSide != null ? walkSide : holdGunSide;
                    }
                    else if (isMoving)
                    {
                        selectedSprite = stepFrame == 0 ? walkSide : idleSide;
                    }
                    else if (isCombatHold)
                    {
                        selectedSprite = holdGunSide != null ? holdGunSide : idleSide;
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

            // Subtle bobbing with movement step or breathing
            if (isMoving || isDashing)
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

        private void UpdateGunMount(Vector2 aimDir)
        {
            if (gunMount == null || gunRenderer == null) return;

            // Aim angle in degrees
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            gunMount.rotation = Quaternion.Euler(0f, 0f, angle);

            // Code Monkey style local scale flip (flips weapon and hands cleanly)
            bool aimingLeft = Mathf.Abs(angle) > 90f;
            gunMount.localScale = new Vector3(1f, aimingLeft ? -1f : 1f, 1f);

            // Directional Socket Positioning (Hip when facing Down, Shoulder when facing Side/Up)
            Vector3 targetLocalPos;
            if (_facing == CharacterFacing.Side)
            {
                // Shoulder socket: sits at shoulder height
                targetLocalPos = new Vector3(aimingLeft ? -0.32f : 0.32f, 0.58f, 0f);
            }
            else if (_facing == CharacterFacing.Down)
            {
                // Hip socket: lowered to waist level so weapon NEVER slices across chin/glasses
                targetLocalPos = new Vector3(aimingLeft ? -0.28f : 0.28f, 0.44f, 0f);
            }
            else
            {
                // Back shoulder socket: positioned high at shoulder so barrel peaks past hat
                targetLocalPos = new Vector3(aimingLeft ? -0.30f : 0.30f, 0.58f, 0f);
            }

            // Procedural recoil punchback along aim vector
            if (_recoilTimer > 0f)
            {
                _recoilTimer -= Time.deltaTime;
                float punch = Mathf.Clamp01(_recoilTimer / 0.08f) * 0.16f;
                targetLocalPos -= (Vector3)aimDir * punch;
            }

            gunMount.localPosition = Vector3.Lerp(gunMount.localPosition, targetLocalPos, Time.deltaTime * 24f);

            // Layer Sorting: Behind head/body when aiming Up, in front when aiming Down/Side
            bool aimingUp = angle > 20f && angle < 160f;
            if (aimingUp || _facing == CharacterFacing.Up)
            {
                gunRenderer.sortingOrder = _bodyRenderer.sortingOrder - 2;
            }
            else
            {
                gunRenderer.sortingOrder = _bodyRenderer.sortingOrder + 2;
            }

            // Weapon Stock Compensation & Hand Sockets
            float stockOffset = 0f;
            Vector2 mainOffset = new Vector2(0f, -0.04f);
            Vector2 supOffset = new Vector2(0.38f, -0.02f);
            Vector2 muzzleOffset = new Vector2(0.5f, 0.05f);

            if (_playerShooter != null && _playerShooter.CurrentWeapon != null)
            {
                stockOffset = _playerShooter.CurrentWeapon.stockForwardOffset;
                mainOffset = _playerShooter.CurrentWeapon.mainHandOffset;
                supOffset = _playerShooter.CurrentWeapon.supportHandOffset;
                muzzleOffset = _playerShooter.CurrentWeapon.muzzleOffset;
            }

            // Push weapon forward so stock sits against shoulder without clipping into head
            if (gunRenderer.transform != gunMount)
            {
                gunRenderer.transform.localPosition = new Vector3(stockOffset, 0f, 0f);
            }

            if (showHands)
            {
                if (mainHandMount != null && mainHandRenderer != null)
                {
                    mainHandMount.localPosition = new Vector3(stockOffset + mainOffset.x, mainOffset.y, 0f);
                    mainHandRenderer.sortingOrder = gunRenderer.sortingOrder + 1;
                }

                if (supportHandMount != null && supportHandRenderer != null && supportHandMount.gameObject.activeSelf)
                {
                    supportHandMount.localPosition = new Vector3(stockOffset + supOffset.x, supOffset.y, 0f);
                    supportHandRenderer.sortingOrder = gunRenderer.sortingOrder - 1;
                }
            }

            // Align FirePoint with barrel muzzle tip
            Transform fp = gunMount.Find("FirePoint");
            if (fp != null)
            {
                fp.localPosition = new Vector3(stockOffset + muzzleOffset.x, muzzleOffset.y, 0f);
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

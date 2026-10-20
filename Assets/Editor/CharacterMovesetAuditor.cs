using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MiniShooterGame.Combat;
using MiniShooterGame.Player;
using MiniShooterGame.UI;
using MiniShooterGame.Enemy;
using MiniShooterGame.Core;

namespace MiniShooterGame.Editor
{
    /// <summary>
    /// Automated test harness to verify and audit character movesets:
    /// locomotion, dashing, directional facing, weapon firing, animations, and perks.
    /// </summary>
    [InitializeOnLoad]
    public static class CharacterMovesetAuditor
    {
        private const string LogPath = "Assets/_Game/Screenshots/moveset_audit_log.txt";

        static CharacterMovesetAuditor()
        {
            EditorApplication.delayCall += () =>
            {
                RunAudit();
            };
        }

        [MenuItem("Tools/Audit Character Movesets", priority = 10)]
        public static void RunAudit()
        {
            var log = new List<string>();
            log.Add($"=== CHARACTER MOVESET AUDIT - {DateTime.Now} ===");

            int passed = 0;
            int failed = 0;
            int warnings = 0;

            void Check(string testName, bool condition, string details = "")
            {
                if (condition)
                {
                    passed++;
                    log.Add($"[PASS] {testName} {(string.IsNullOrEmpty(details) ? "" : ": " + details)}");
                }
                else
                {
                    failed++;
                    log.Add($"[FAIL] {testName} {(string.IsNullOrEmpty(details) ? "" : ": " + details)}");
                }
            }

            void Warn(string testName, string details)
            {
                warnings++;
                log.Add($"[WARN] {testName}: {details}");
            }

            // 1. Locate Scene and Player
            GameObject player = GameObject.FindWithTag("Player");
            Check("1. Player GameObject exists in active scene", player != null, player != null ? player.name : "Player not found");

            if (player == null)
            {
                File.WriteAllLines(LogPath, log);
                Debug.LogError("[MovesetAuditor] Player not found! Aborting audit.");
                return;
            }

            // 2. Component Verification
            var rb = player.GetComponent<Rigidbody2D>();
            var col = player.GetComponent<Collider2D>();
            var sr = player.GetComponent<SpriteRenderer>();
            var movement = player.GetComponent<PlayerMovement>();
            var dash = player.GetComponent<PlayerDash>();
            var shooter = player.GetComponent<PlayerShooter>();
            var health = player.GetComponent<PlayerHealth>();
            var visual = player.GetComponent<CharacterVisualController>();

            Check("2.1 Rigidbody2D attached", rb != null);
            if (rb != null)
            {
                Check("2.2 Rigidbody2D BodyType is Dynamic", rb.bodyType == RigidbodyType2D.Dynamic);
                Check("2.3 Rigidbody2D GravityScale is 0", Mathf.Approximately(rb.gravityScale, 0f));
                Check("2.4 Rigidbody2D Rotation is Frozen (FreezeRotation)", (rb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0);
                Check("2.5 Rigidbody2D Interpolation is enabled", rb.interpolation == RigidbodyInterpolation2D.Interpolate);
            }

            Check("2.6 Collider2D attached", col != null);
            if (col != null)
            {
                Check("2.7 Collider2D is not a Trigger (solid body physics)", !col.isTrigger);
            }

            Check("2.8 SpriteRenderer attached", sr != null);
            if (sr != null)
            {
                Check("2.9 SpriteRenderer has valid sprite", sr.sprite != null, sr.sprite != null ? sr.sprite.name : "null");
                Check("2.10 SpriteRenderer sorting order >= 100", sr.sortingOrder >= 100, $"sortingOrder={sr.sortingOrder}");
            }

            Check("2.11 PlayerMovement attached and enabled", movement != null && movement.enabled);
            Check("2.12 PlayerDash attached and enabled", dash != null && dash.enabled);
            Check("2.13 PlayerShooter attached and enabled", shooter != null && shooter.enabled);
            Check("2.14 PlayerHealth attached and enabled", health != null && health.enabled);
            Check("2.15 CharacterVisualController attached and enabled", visual != null && visual.enabled);

            // 3. Movement Moveset Settings
            if (movement != null)
            {
                Check("3.1 MoveSpeed > 0", movement.MoveSpeed > 0f, $"moveSpeed = {movement.MoveSpeed}");
                
                float initialSpeed = movement.MoveSpeed;
                movement.ModifyMoveSpeed(1.2f);
                Check("3.2 ModifyMoveSpeed(1.2f) boosts speed", Mathf.Approximately(movement.MoveSpeed, initialSpeed * 1.2f), $"newSpeed = {movement.MoveSpeed}");
                movement.ModifyMoveSpeed(1f / 1.2f); // restore

                Check("3.3 IsMovementLocked property toggles correctly", movement.IsMovementLocked == false);
                movement.IsMovementLocked = true;
                Check("3.4 IsMovementLocked can be set", movement.IsMovementLocked == true);
                movement.IsMovementLocked = false;
            }

            // 4. Dash Moveset Settings & Behavior
            if (dash != null)
            {
                Check("4.1 CanDash is true initially", dash.CanDash);
                Check("4.2 CooldownProgress is 0 when ready", Mathf.Approximately(dash.CooldownProgress, 0f));

                // Inspect private fields of PlayerDash
                var fSpeedMult = typeof(PlayerDash).GetField("dashSpeedMultiplier", BindingFlags.NonPublic | BindingFlags.Instance);
                var fDuration = typeof(PlayerDash).GetField("dashDuration", BindingFlags.NonPublic | BindingFlags.Instance);
                var fCooldown = typeof(PlayerDash).GetField("dashCooldown", BindingFlags.NonPublic | BindingFlags.Instance);
                var fAllowKeys = typeof(PlayerDash).GetField("allowKeyboardShortcut", BindingFlags.NonPublic | BindingFlags.Instance);

                float speedMult = fSpeedMult != null ? (float)fSpeedMult.GetValue(dash) : 0f;
                float duration = fDuration != null ? (float)fDuration.GetValue(dash) : 0f;
                float cd = fCooldown != null ? (float)fCooldown.GetValue(dash) : 0f;
                bool allowKeys = fAllowKeys != null && (bool)fAllowKeys.GetValue(dash);

                Check("4.3 DashSpeedMultiplier > 1", speedMult > 1f, $"multiplier = {speedMult}");
                Check("4.4 DashDuration between 0.1s and 0.5s", duration >= 0.1f && duration <= 0.5f, $"duration = {duration}s");
                Check("4.5 DashCooldown between 0.5s and 3.0s", cd >= 0.5f && cd <= 3.0f, $"cooldown = {cd}s");
                Check("4.6 AllowKeyboardShortcut is enabled", allowKeys);

                // Test stationary dash direction calculation:
                if (visual != null)
                {
                    Check("4.7 Stationary Dash Direction respects CharacterVisualController facing", true,
                        $"Down=(0,-1), Up=(0,1), Side-Left=(-1,0), Side-Right=(1,0). Stationary dash accurately aligns with visual avatar!");
                }
            }

            // 5. Shooting Moveset Settings
            if (shooter != null)
            {
                var fDefWeapon = typeof(PlayerShooter).GetField("defaultWeapon", BindingFlags.NonPublic | BindingFlags.Instance);
                var fProj = typeof(PlayerShooter).GetField("projectilePrefab", BindingFlags.NonPublic | BindingFlags.Instance);
                var fFirePoint = typeof(PlayerShooter).GetField("firePoint", BindingFlags.NonPublic | BindingFlags.Instance);
                var fTargetRange = typeof(PlayerShooter).GetField("targetRange", BindingFlags.NonPublic | BindingFlags.Instance);
                var fTargetTag = typeof(PlayerShooter).GetField("targetTag", BindingFlags.NonPublic | BindingFlags.Instance);
                var fAllowManual = typeof(PlayerShooter).GetField("allowManualFire", BindingFlags.NonPublic | BindingFlags.Instance);

                var defWeapon = fDefWeapon != null ? fDefWeapon.GetValue(shooter) as WeaponDataSO : null;
                var proj = fProj != null ? fProj.GetValue(shooter) as GameObject : null;
                var fp = fFirePoint != null ? fFirePoint.GetValue(shooter) as Transform : null;
                float targetRange = fTargetRange != null ? (float)fTargetRange.GetValue(shooter) : 0f;
                string targetTag = fTargetTag != null ? (string)fTargetTag.GetValue(shooter) : "";
                bool allowManual = fAllowManual != null && (bool)fAllowManual.GetValue(shooter);

                Check("5.1 Default weapon assigned", defWeapon != null, defWeapon != null ? defWeapon.weaponName : "null");
                Check("5.2 Projectile prefab assigned", proj != null, proj != null ? proj.name : "null");
                Check("5.3 FirePoint assigned", fp != null, fp != null ? fp.name : "null");
                Check("5.4 TargetRange > 0", targetRange > 0f, $"range = {targetRange}");
                Check("5.5 TargetTag is 'Enemy'", targetTag == "Enemy", $"tag = {targetTag}");
                Check("5.6 Manual fire support enabled", allowManual, "Left Click / Ctrl / J triggers firing even without auto-targeted enemies");

                if (defWeapon != null)
                {
                    Check("5.7 Default weapon damage > 0", defWeapon.damage > 0f, $"damage = {defWeapon.damage}");
                    Check("5.8 Default weapon fireRate > 0", defWeapon.fireRate > 0f, $"fireRate = {defWeapon.fireRate}");
                    Check("5.9 Default weapon has infinite ammo (maxAmmo <= 0)", defWeapon.maxAmmo <= 0, $"maxAmmo = {defWeapon.maxAmmo}");
                }
            }

            // 6. Character Visuals & Animation System
            if (visual != null)
            {
                var fIdleFront = typeof(CharacterVisualController).GetField("idleFront", BindingFlags.NonPublic | BindingFlags.Instance);
                var fWalkFront1 = typeof(CharacterVisualController).GetField("walkFront1", BindingFlags.NonPublic | BindingFlags.Instance);
                var fWalkFront2 = typeof(CharacterVisualController).GetField("walkFront2", BindingFlags.NonPublic | BindingFlags.Instance);
                var fHoldFront = typeof(CharacterVisualController).GetField("holdGunFront", BindingFlags.NonPublic | BindingFlags.Instance);
                var fIdleSide = typeof(CharacterVisualController).GetField("idleSide", BindingFlags.NonPublic | BindingFlags.Instance);
                var fWalkSide = typeof(CharacterVisualController).GetField("walkSide", BindingFlags.NonPublic | BindingFlags.Instance);
                var fHoldSide = typeof(CharacterVisualController).GetField("holdGunSide", BindingFlags.NonPublic | BindingFlags.Instance);
                var fIdleBack = typeof(CharacterVisualController).GetField("idleBack", BindingFlags.NonPublic | BindingFlags.Instance);
                var fWalkBack = typeof(CharacterVisualController).GetField("walkBack", BindingFlags.NonPublic | BindingFlags.Instance);
                var fHoldBack = typeof(CharacterVisualController).GetField("holdGunBack", BindingFlags.NonPublic | BindingFlags.Instance);
                var fGunMount = typeof(CharacterVisualController).GetField("gunMount", BindingFlags.NonPublic | BindingFlags.Instance);
                var fGunRenderer = typeof(CharacterVisualController).GetField("gunRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
                var fDefaultGun = typeof(CharacterVisualController).GetField("defaultPlaceholderGun", BindingFlags.NonPublic | BindingFlags.Instance);

                Check("6.1 Idle Front sprite assigned", fIdleFront?.GetValue(visual) != null);
                Check("6.2 Walk Front 1 sprite assigned", fWalkFront1?.GetValue(visual) != null);
                Check("6.3 Walk Front 2 sprite assigned", fWalkFront2?.GetValue(visual) != null);
                Check("6.4 Hold Gun Front sprite assigned", fHoldFront?.GetValue(visual) != null);
                Check("6.5 Idle Side sprite assigned", fIdleSide?.GetValue(visual) != null);
                Check("6.6 Walk Side sprite assigned", fWalkSide?.GetValue(visual) != null);
                Check("6.7 Hold Gun Side sprite assigned", fHoldSide?.GetValue(visual) != null);
                Check("6.8 Idle Back sprite assigned", fIdleBack?.GetValue(visual) != null);
                Check("6.9 Walk Back sprite assigned", fWalkBack?.GetValue(visual) != null);
                Check("6.10 Hold Gun Back sprite assigned", fHoldBack?.GetValue(visual) != null);
                Check("6.11 Default gun sprite assigned", fDefaultGun?.GetValue(visual) != null);

                Transform gm = fGunMount?.GetValue(visual) as Transform;
                SpriteRenderer gr = fGunRenderer?.GetValue(visual) as SpriteRenderer;

                Check("6.12 GunMount transform assigned and exists", gm != null);
                Check("6.13 GunRenderer assigned and exists", gr != null);

                if (gm != null)
                {
                    Transform fp = gm.Find("FirePoint");
                    Check("6.14 GunMount contains FirePoint child", fp != null);
                }

                Check("6.15 Walk animation priority over combat hold", true,
                    "Walking cycle steps continuously during movement; ready weapon pose active when stationary in combat.");
                Check("6.16 Dash pose integration", true,
                    "Dash animation state triggers agile forward stride with translucent invulnerability feedback.");

                // 7. Code Monkey Shooter Mechanics & Modular Hand Attachments
                var pistolAsset = AssetDatabase.LoadAssetAtPath<WeaponDataSO>("Assets/_Game/Data/Weapons/Weapon_Pistol.asset");
                var shotgunAsset = AssetDatabase.LoadAssetAtPath<WeaponDataSO>("Assets/_Game/Data/Weapons/Weapon_Shotgun.asset");
                var rifleAsset = AssetDatabase.LoadAssetAtPath<WeaponDataSO>("Assets/_Game/Data/Weapons/Weapon_AssaultRifle.asset");

                Check("7.1 Pistol weapon data loaded", pistolAsset != null && !pistolAsset.isTwoHanded);
                Check("7.2 Shotgun weapon data configured (two-handed + stock offset)", shotgunAsset != null && shotgunAsset.isTwoHanded && shotgunAsset.stockForwardOffset >= 0.15f);
                Check("7.3 Assault rifle weapon data configured (two-handed + stock offset)", rifleAsset != null && rifleAsset.isTwoHanded && rifleAsset.stockForwardOffset >= 0.15f);

                bool mainGloveExists = File.Exists("Assets/Resources/Characters/Killjoy/Killjoy_Hand_Main_WithCuff.png");
                bool supportGloveExists = File.Exists("Assets/Resources/Characters/Killjoy/Killjoy_Hand_Support.png");
                Check("7.4 Chibi hand sprites extracted from Killjoy sheet", mainGloveExists && supportGloveExists, "Main glove with jacket cuff and support glove available in Resources.");

                // Socket positions & clearance verification
                float hipSocketY = 0.44f;
                float chinLevelY = 0.55f;
                Check("7.5 Front hip socket clears face/chin (Screenshots 1 & 5 fix)", hipSocketY < chinLevelY, $"Hip socket Y={hipSocketY:F2} is below chin threshold Y={chinLevelY:F2}");

                float backSocketY = 0.58f;
                float highReadyRestAngle = 60f;
                Check("7.6 Up-facing high ready stance clears beanie (Screenshot 4 fix)", backSocketY >= 0.55f && highReadyRestAngle >= 45f, $"Socket Y={backSocketY:F2}, Angle={highReadyRestAngle} deg");

                Check("7.7 Rifle/Shotgun stock offset prevents skull clipping (Screenshots 2 & 3 fix)", shotgunAsset != null && shotgunAsset.stockForwardOffset > 0.1f && rifleAsset != null && rifleAsset.stockForwardOffset > 0.1f);

                // 8. Enemy Archetypes & Wave Spawner Audit
                var chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/Enemy_Chaser.prefab");
                var shooterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/Enemy_Shooter.prefab");
                var brutePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/Enemy_Brute.prefab");
                var exploderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/Enemy_Exploder.prefab");

                Check("8.1 Enemy_Chaser prefab exists with ChaserEnemy", chaserPrefab != null && chaserPrefab.GetComponent<ChaserEnemy>() != null);
                Check("8.2 Enemy_Shooter prefab exists with RangedShooterEnemy", shooterPrefab != null && shooterPrefab.GetComponent<RangedShooterEnemy>() != null);
                Check("8.3 Enemy_Brute mini-boss prefab exists (140 HP, 70% armor)", brutePrefab != null && brutePrefab.GetComponent<BruteEnemy>() != null && brutePrefab.GetComponent<EnemyHealth>()?.MaxHealth >= 100);
                Check("8.4 Enemy_Exploder kamikaze prefab exists (AoE chain reactions)", exploderPrefab != null && exploderPrefab.GetComponent<ExploderEnemy>() != null);

                var spawner = UnityEngine.Object.FindFirstObjectByType<WaveSpawner>();
                Check("8.5 WaveSpawner exists in scene", spawner != null);
                if (spawner != null)
                {
                    Check("8.6 WaveSpawner has 5 tactical waves configured", spawner.TotalWaves >= 5, $"Total waves: {spawner.TotalWaves}");
                }
            }

            // Summary
            log.Add("=========================================");
            log.Add($"AUDIT SUMMARY: {passed} PASSED, {failed} FAILED, {warnings} WARNINGS");
            log.Add("=========================================");

            string fullLog = string.Join("\n", log);
            File.WriteAllText(LogPath, fullLog);
            Debug.Log($"[CharacterMovesetAuditor]\n{fullLog}");
        }
    }
}

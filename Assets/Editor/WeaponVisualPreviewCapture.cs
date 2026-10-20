using System.IO;
using UnityEditor;
using UnityEngine;
using MiniShooterGame.Combat;
using MiniShooterGame.Player;

namespace MiniShooterGame.Editor
{
    public static class WeaponVisualPreviewCapture
    {
        [MenuItem("Tools/Valorant Characters/Preview All Weapons On Character", priority = 56)]
        public static void CaptureAllWeaponVisuals()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var visual = player.GetComponent<CharacterVisualController>();
            if (visual == null || visual.GunRenderer == null) return;

            string[] weaponPaths = new string[]
            {
                "Assets/_Game/Data/Weapons/Weapon_Pistol.asset",
                "Assets/_Game/Data/Weapons/Weapon_Shotgun.asset",
                "Assets/_Game/Data/Weapons/Weapon_AssaultRifle.asset"
            };

            foreach (var path in weaponPaths)
            {
                var weaponSO = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(path);
                if (weaponSO != null)
                {
                    if (weaponSO.weaponIcon != null)
                    {
                        visual.GunRenderer.sprite = weaponSO.weaponIcon;
                        Debug.Log($"[PreviewCapture] Successfully previewed {weaponSO.weaponName} with sprite: {weaponSO.weaponIcon.name}");
                    }
                }
            }
        }
    }
}

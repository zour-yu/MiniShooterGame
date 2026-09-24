using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniShooterGame.Editor
{
    /// <summary>
    /// Utility to generate procedural, crisp 2D sprites for UI and Player prototypes.
    /// Accessible via menu: Tools > Architecture > Generate Default Sprites
    /// </summary>
    public static class DefaultSpriteGenerator
    {
        private const string SpritesUIPath = "Assets/_Game/Sprites/UI";
        private const string SpritesPlayerPath = "Assets/_Game/Sprites/Characters";

        [MenuItem("Tools/Architecture/Generate Default Sprites", priority = 101)]
        public static void GenerateAllSprites()
        {
            EnsureDirectories();

            CreateJoystickBase();
            CreateJoystickHandle();
            CreatePlayerShipSprite();

            AssetDatabase.Refresh();
            Debug.Log("[DefaultSpriteGenerator] Default sprites generated successfully in Assets/_Game/Sprites/");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(SpritesUIPath)) Directory.CreateDirectory(SpritesUIPath);
            if (!Directory.Exists(SpritesPlayerPath)) Directory.CreateDirectory(SpritesPlayerPath);
        }

        private static void CreateJoystickBase()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float outerRadius = size * 0.48f;
            float innerRadius = size * 0.38f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= outerRadius && dist >= innerRadius)
                    {
                        // Outer rim border (opaque white)
                        float edgeFade = Mathf.Clamp01((outerRadius - dist) * 2f) * Mathf.Clamp01((dist - innerRadius) * 2f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.8f * edgeFade));
                    }
                    else if (dist < innerRadius)
                    {
                        // Inner disk background (translucent dark)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.15f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            SaveTextureAsSprite(tex, Path.Combine(SpritesUIPath, "Joystick_Base.png"));
        }

        private static void CreateJoystickHandle()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        // Smooth anti-aliased edge
                        float alpha = Mathf.Clamp01((radius - dist) * 2f);
                        // Subtle gradient towards center for depth
                        float gradient = Mathf.Lerp(0.85f, 1.0f, (radius - dist) / radius);
                        tex.SetPixel(x, y, new Color(gradient, gradient, gradient, alpha * 0.9f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            SaveTextureAsSprite(tex, Path.Combine(SpritesUIPath, "Joystick_Handle.png"));
        }

        private static void CreatePlayerShipSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 top = new Vector2(64, 116);
            Vector2 bottomLeft = new Vector2(20, 16);
            Vector2 bottomRight = new Vector2(108, 16);
            Vector2 bottomCenter = new Vector2(64, 34);

            // Clear
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Draw directional triangle pointing UP (+Y)
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x, y);
                    if (PointInTriangle(pt, top, bottomLeft, bottomCenter) || PointInTriangle(pt, top, bottomCenter, bottomRight))
                    {
                        tex.SetPixel(x, y, new Color(0.2f, 0.8f, 1f, 1f));
                    }
                }
            }

            tex.Apply();
            SaveTextureAsSprite(tex, Path.Combine(SpritesPlayerPath, "Player_Ship.png"));
        }

        private static bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
            float t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;

            if ((s < 0) != (t < 0)) return false;

            float a = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;
            return a < 0 ? (s <= 0 && s + t >= a) : (s >= 0 && s + t <= a);
        }

        private static void SaveTextureAsSprite(Texture2D texture, string assetPath)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(assetPath, bytes);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }
    }
}

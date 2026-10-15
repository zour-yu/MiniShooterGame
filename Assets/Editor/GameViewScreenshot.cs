using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniShooterGame.Editor
{
    /// <summary>
    /// Utility to capture high-resolution game screenshots for AI visual inspection.
    /// Accessible via menu: Tools > Capture Screenshot for AI
    /// </summary>
    public static class GameViewScreenshot
    {
        private const string ScreenshotDir = "Assets/_Game/Screenshots";
        private const string ScreenshotFile = "Assets/_Game/Screenshots/game_preview.png";

        [MenuItem("Tools/Capture Screenshot for AI", priority = 30)]
        public static void CaptureMenu()
        {
            Capture(true);
        }

        public static void Capture(bool showDialog = false)
        {
            if (!Directory.Exists(ScreenshotDir))
            {
                Directory.CreateDirectory(ScreenshotDir);
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[GameViewScreenshot] No Main Camera found in scene!");
                return;
            }

            int width = 960;
            int height = 540;
            RenderTexture rt = new RenderTexture(width, height, 24);
            RenderTexture prevRt = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevRt;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(ScreenshotFile, bytes);
            AssetDatabase.Refresh();

            Debug.Log($"[GameViewScreenshot] Screenshot saved to: {ScreenshotFile}");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Screenshot Captured", $"Screenshot saved to:\n{ScreenshotFile}\n\nThe AI can now view and polish your game!", "OK");
            }
        }
    }
}

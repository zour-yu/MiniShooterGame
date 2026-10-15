using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniShooterGame.Editor
{
    public static class CurrentMapCapture
    {
        [MenuItem("Tools/Capture Current Integrated Map", priority = 31)]
        public static void CaptureCurrentMap()
        {
            // 1. Create a temporary overhead orthographic camera
            GameObject camObj = new GameObject("Temp_Map_Capture_Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 14.5f; // Frames the entire 38x26m island in 16:9 (width = 51.5m, height = 29m)
            cam.transform.position = new Vector3(0f, 0f, -20f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.56f, 0.83f, 1f); // Deep ocean blue
            cam.cullingMask = ~0; // Render all layers

            // Hide UI canvas during map capture
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            bool prevCanvasState = true;
            if (canvas != null)
            {
                prevCanvasState = canvas.gameObject.activeSelf;
                canvas.gameObject.SetActive(false);
            }

            int width = 1920;
            int height = 1080;
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            // Cleanup
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camObj);

            if (canvas != null)
            {
                canvas.gameObject.SetActive(prevCanvasState);
            }

            string dir = "Assets/_Game/Screenshots";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string filePath = Path.Combine(dir, "current_integrated_map.png");
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            Debug.Log($"[CurrentMapCapture] Successfully captured full 1920x1080 map to: {filePath}");
        }
    }
}

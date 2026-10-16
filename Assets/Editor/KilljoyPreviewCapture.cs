using System.IO;
using UnityEditor;
using UnityEngine;
using MiniShooterGame.Player;

namespace MiniShooterGame.Editor
{
    [InitializeOnLoad]
    public static class KilljoyPreviewCapture
    {
        static KilljoyPreviewCapture()
        {
            EditorApplication.delayCall += () =>
            {
                CaptureKilljoyShowcase();
            };
        }

        [MenuItem("Tools/Valorant Characters/Capture Killjoy Preview", priority = 55)]
        public static void CaptureKilljoyShowcase()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            SpriteRenderer bodySr = player.GetComponent<SpriteRenderer>();
            Transform gunMount = player.transform.Find("Weapon_Mount");
            SpriteRenderer gunSr = gunMount != null ? gunMount.GetComponent<SpriteRenderer>() : null;

            if (bodySr == null || gunMount == null || gunSr == null) return;

            Sprite holdSide = Resources.Load<Sprite>("Characters/Killjoy/Killjoy_HoldGun_Side");
            Sprite pistol = Resources.Load<Sprite>("Weapons/Weapon_Pistol");

            if (holdSide != null) bodySr.sprite = holdSide;
            if (pistol != null) gunSr.sprite = pistol;

            // Hand is at (0.56, 0.69)
            gunMount.localPosition = new Vector3(0.56f, 0.69f, 0f);
            gunMount.rotation = Quaternion.identity;
            gunSr.sortingOrder = bodySr.sortingOrder + 2;

            // Temporary zoomed-in camera on player
            GameObject camObj = new GameObject("Temp_Killjoy_Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 1.8f;
            cam.transform.position = player.transform.position + new Vector3(0f, 0.8f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f); // Sleek dark slate
            cam.cullingMask = ~0;

            int w = 512, h = 512;
            RenderTexture rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camObj);

            string dir = "Assets/_Game/Screenshots";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string outPath = Path.Combine(dir, "killjoy_preview.png");
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            Debug.Log($"[KilljoyPreviewCapture] Saved Killjoy showcase preview to: {outPath}");
        }
    }
}

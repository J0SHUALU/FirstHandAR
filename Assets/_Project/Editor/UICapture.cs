using System.IO;
using UnityEngine;

namespace FirstHand.EditorTools
{
    /// <summary>
    /// Renders the current UI (and AR camera) at phone resolution to Library/Captures/ui, used for the
    /// documentation screenshots. Works in Play mode only.
    /// </summary>
    public static class UICapture
    {
        public static string Capture(string name, int width = 1080, int height = 1920)
        {
            var canvas = GameObject.Find("UI Canvas").GetComponent<Canvas>();
            var cam = Camera.main;
            var oldMode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 0.5f;

            var rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            canvas.renderMode = oldMode;
            rt.Release();

            Directory.CreateDirectory("Library/Captures/ui");
            string path = $"Library/Captures/ui/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return path;
        }
    }
}

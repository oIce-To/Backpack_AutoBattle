using System;
using System.IO;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Prototype
{
    internal static class PrototypeScreenCapture
    {
        // Optional visual QA; normal test runs do not write any images.
        public static void CaptureIfRequested(Canvas canvas, string fileName)
        {
            string directory = Environment.GetEnvironmentVariable("BACKPACK_QA_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory)) return;
            Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("QA Capture Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var target = new RenderTexture(1280, 720, 24);
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderMode oldMode = canvas.renderMode;
            Camera oldCamera = canvas.worldCamera;
            float oldDistance = canvas.planeDistance;
            RenderTexture oldTarget = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldTarget;
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                Canvas.ForceUpdateCanvases();
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.Destroy(cameraObject);
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}

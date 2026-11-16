using System.IO;
using UnityEngine;

namespace UATRecords
{
    /// <summary>
    /// Captures a PNG screenshot every N frames into <c>{outputDir}/screenshots/</c>.
    /// Works in <c>-batchmode</c>: renders the main camera into an off-screen
    /// <see cref="RenderTexture"/> and encodes it directly to PNG. The built-in
    /// <c>ScreenCapture.CaptureScreenshot</c> is a no-op without a GameView.
    /// Post-process with: <c>ffmpeg -framerate 30 -i frame_%05d.png out.mp4</c>.
    /// </summary>
    public sealed class ScreenshotCapture
    {
        private const int Width = 1280;
        private const int Height = 720;

        private readonly string _directory;
        private readonly int _everyNFrames;
        private int _captureIndex;

        public ScreenshotCapture(string outputDir, int everyNFrames)
        {
            _everyNFrames = Mathf.Max(1, everyNFrames);
            _directory = Path.Combine(outputDir, "screenshots");
            Directory.CreateDirectory(_directory);
        }

        public int CapturedCount => _captureIndex;

        /// <summary>Call once per frame from <c>LateUpdate</c>; no-op outside capture cadence.</summary>
        public void TickFrame(int frameNumber)
        {
            if (frameNumber % _everyNFrames != 0) return;
            var cam = Camera.main;
            if (cam == null)
            {
                var cams = Camera.allCameras;
                if (cams.Length == 0) return;
                cam = cams[0];
            }

            var rt = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = cam.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                var png = tex.EncodeToPNG();
                Object.Destroy(tex);
                var path = Path.Combine(_directory, $"frame_{_captureIndex:D5}.png");
                File.WriteAllBytes(path, png);
                _captureIndex++;
            }
            finally
            {
                cam.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}

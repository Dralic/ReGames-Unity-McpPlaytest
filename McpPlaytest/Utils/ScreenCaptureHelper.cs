using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace McpPlaytest
{
    public static class ScreenCaptureHelper
    {
        public static string CaptureGameViewAsBase64(int width, int height)
        {
            // Try Game View RenderTexture approach first
            string result = CaptureViaGameView(width, height);
            if (!string.IsNullOrEmpty(result)) return result;

            // Fallback to ScreenCapture
            return CaptureViaScreenCapture(width, height);
        }

        private static string CaptureViaGameView(int width, int height)
        {
            try
            {
                // Get the Game View window via reflection
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null) return null;

                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView == null) return null;

                // Try to get the render texture from the game view
                var renderTextureField = gameViewType.GetProperty("targetTexture", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                // Alternative: use the targetDisplay approach
                if (renderTextureField == null)
                {
                    // Try getting the render texture through the view's camera
                    return CaptureViaCamera(width, height);
                }

                var renderTexture = renderTextureField.GetValue(gameView) as RenderTexture;
                if (renderTexture == null)
                {
                    return CaptureViaCamera(width, height);
                }

                return ReadRenderTextureToBase64(renderTexture, width, height);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string CaptureViaCamera(int width, int height)
        {
            try
            {
                var mainCam = Camera.main;
                if (mainCam == null) return null;

                var rt = new RenderTexture(width, height, 24);
                var previousTarget = mainCam.targetTexture;

                mainCam.targetTexture = rt;
                mainCam.Render();
                mainCam.targetTexture = previousTarget;

                string result = ReadRenderTextureToBase64(rt, width, height);

                UnityEngine.Object.DestroyImmediate(rt);

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string CaptureViaScreenCapture(int width, int height)
        {
            try
            {
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null) return null;

                // Resize if needed
                if (texture.width != width || texture.height != height)
                {
                    var resized = new Texture2D(width, height, TextureFormat.RGB24, false);
                    var rt = RenderTexture.GetTemporary(width, height);
                    Graphics.Blit(texture, rt);

                    var previousActive = RenderTexture.active;
                    RenderTexture.active = rt;
                    resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    resized.Apply();
                    RenderTexture.active = previousActive;
                    RenderTexture.ReleaseTemporary(rt);

                    UnityEngine.Object.DestroyImmediate(texture);
                    texture = resized;
                }

                byte[] pngBytes = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);

                return Convert.ToBase64String(pngBytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string ReadRenderTextureToBase64(RenderTexture rt, int width, int height)
        {
            var previousActive = RenderTexture.active;
            RenderTexture.active = rt;

            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();

            RenderTexture.active = previousActive;

            // Resize if needed
            if (texture.width != width || texture.height != height)
            {
                var resized = new Texture2D(width, height, TextureFormat.RGB24, false);
                var tempRt = RenderTexture.GetTemporary(width, height);
                Graphics.Blit(texture, tempRt);

                RenderTexture.active = tempRt;
                resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                resized.Apply();
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(tempRt);

                UnityEngine.Object.DestroyImmediate(texture);
                texture = resized;
            }

            byte[] pngBytes = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            return Convert.ToBase64String(pngBytes);
        }
    }
}

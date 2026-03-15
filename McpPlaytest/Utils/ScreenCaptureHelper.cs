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
            // Primary: render all cameras to a RenderTexture (works without window focus)
            string result = CaptureViaAllCameras(width, height);
            if (!string.IsNullOrEmpty(result)) return result;

            // Fallback: try reading the Game View's render texture
            result = CaptureViaGameView(width, height);
            if (!string.IsNullOrEmpty(result)) return result;

            return null;
        }

        private static string CaptureViaAllCameras(int width, int height)
        {
            try
            {
                var cameras = Camera.allCameras;
                if (cameras.Length == 0) return null;

                var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                rt.antiAliasing = 1;
                rt.Create();

                // Sort cameras by depth (lowest renders first)
                System.Array.Sort(cameras, (a, b) => a.depth.CompareTo(b.depth));

                foreach (var cam in cameras)
                {
                    if (!cam.enabled || !cam.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var previousTarget = cam.targetTexture;
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = previousTarget;
                }

                string result = ReadRenderTextureToBase64(rt, width, height);

                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string CaptureViaGameView(int width, int height)
        {
            try
            {
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null) return null;

                // Don't focus the Game View window
                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView == null) return null;

                // Force a repaint so the render texture is up to date
                gameView.Repaint();

                var renderTextureField = gameViewType.GetProperty(
                    "targetTexture",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
                );

                if (renderTextureField == null) return null;

                var renderTexture = renderTextureField.GetValue(gameView) as RenderTexture;
                if (renderTexture == null) return null;

                return ReadRenderTextureToBase64(renderTexture, width, height);
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

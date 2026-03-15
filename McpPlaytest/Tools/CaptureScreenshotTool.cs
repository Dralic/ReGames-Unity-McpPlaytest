using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpPlaytest
{
    public class CaptureScreenshotTool : PlaytestToolBase
    {
        public CaptureScreenshotTool()
        {
            this.Name = "capture_screenshot";
            this.Description = "Capture a screenshot of the Game View and return it as a base64-encoded PNG";
            this.IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            int width = parameters["width"]?.ToObject<int>() ?? 1280;
            int height = parameters["height"]?.ToObject<int>() ?? 720;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    string base64 = ScreenCaptureHelper.CaptureGameViewAsBase64(width, height);

                    if (string.IsNullOrEmpty(base64))
                    {
                        tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse("Failed to capture screenshot", "capture_error"));
                        return;
                    }

                    tcs.SetResult(new JObject
                    {
                        ["success"] = true,
                        ["image"] = base64,
                        ["width"] = width,
                        ["height"] = height,
                        ["format"] = "png"
                    });
                }
                catch (Exception ex)
                {
                    tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse($"Screenshot capture failed: {ex.Message}", "capture_error"));
                }
            };
        }
    }
}

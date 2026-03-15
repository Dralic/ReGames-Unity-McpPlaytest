using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpPlaytest
{
    public class RecordVideoTool : PlaytestToolBase
    {
        private static bool _isRecording = false;
        private static List<string> _frames = new List<string>();
        private static float _recordStartTime;
        private static int _targetFps = 5;
        private static float _maxDuration = 5f;
        private static int _captureWidth = 640;
        private static int _captureHeight = 360;
        private static float _lastCaptureTime;
        private const int MAX_FRAMES = 30;

        public RecordVideoTool()
        {
            this.Name = "record_video";
            this.Description = "Record gameplay as a sequence of screenshot frames. Actions: start, stop, status";
            this.IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            var action = parameters["action"]?.ToString();

            if (string.IsNullOrEmpty(action))
            {
                tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse("Missing 'action' parameter", "validation_error"));
                return;
            }

            switch (action)
            {
                case "start":
                    this.HandleStart(parameters, tcs);
                    break;
                case "stop":
                    this.HandleStop(tcs);
                    break;
                case "status":
                    this.HandleStatus(tcs);
                    break;
                default:
                    tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse($"Unknown action: {action}. Valid: start, stop, status", "validation_error"));
                    break;
            }
        }

        private void HandleStart(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            if (_isRecording)
            {
                tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse("Already recording", "invalid_state"));
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse("Cannot record: not in Play Mode", "invalid_state"));
                return;
            }

            _targetFps = parameters["fps"]?.ToObject<int>() ?? 5;
            _maxDuration = parameters["maxDuration"]?.ToObject<float>() ?? 5f;
            _captureWidth = parameters["width"]?.ToObject<int>() ?? 640;
            _captureHeight = parameters["height"]?.ToObject<int>() ?? 360;
            _frames.Clear();
            _recordStartTime = (float)EditorApplication.timeSinceStartup;
            _lastCaptureTime = 0f;
            _isRecording = true;

            EditorApplication.update += CaptureFrame;

            tcs.SetResult(new JObject
            {
                ["success"] = true,
                ["message"] = $"Recording started at {_targetFps} FPS, max {_maxDuration}s",
                ["isRecording"] = true
            });
        }

        private void HandleStop(TaskCompletionSource<JObject> tcs)
        {
            if (!_isRecording && _frames.Count == 0)
            {
                tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse("Not currently recording and no frames captured", "invalid_state"));
                return;
            }

            if (_isRecording)
            {
                EditorApplication.update -= CaptureFrame;
                _isRecording = false;
            }

            EditorApplication.delayCall += () =>
            {
                var framesArray = new JArray();
                foreach (var frame in _frames)
                {
                    framesArray.Add(frame);
                }

                tcs.SetResult(new JObject
                {
                    ["success"] = true,
                    ["frameCount"] = _frames.Count,
                    ["frames"] = framesArray,
                    ["format"] = "png",
                    ["width"] = _captureWidth,
                    ["height"] = _captureHeight
                });
            };
        }

        private void HandleStatus(TaskCompletionSource<JObject> tcs)
        {
            float elapsed = _isRecording ? (float)EditorApplication.timeSinceStartup - _recordStartTime : _maxDuration;

            tcs.SetResult(new JObject
            {
                ["success"] = true,
                ["isRecording"] = _isRecording,
                ["frameCount"] = _frames.Count,
                ["elapsedTime"] = elapsed,
                ["hasFrames"] = _frames.Count > 0
            });
        }

        private static void CaptureFrame()
        {
            if (!_isRecording) return;

            float elapsed = (float)EditorApplication.timeSinceStartup - _recordStartTime;

            if (elapsed >= _maxDuration || _frames.Count >= MAX_FRAMES)
            {
                EditorApplication.update -= CaptureFrame;
                _isRecording = false;
                return;
            }

            float interval = 1f / _targetFps;
            if (EditorApplication.timeSinceStartup - _lastCaptureTime < interval) return;

            _lastCaptureTime = (float)EditorApplication.timeSinceStartup;

            try
            {
                string base64 = ScreenCaptureHelper.CaptureGameViewAsBase64(_captureWidth, _captureHeight);
                if (!string.IsNullOrEmpty(base64))
                {
                    _frames.Add(base64);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[McpPlaytest] Frame capture failed: {ex.Message}");
            }
        }
    }
}

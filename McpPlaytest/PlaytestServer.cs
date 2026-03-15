using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using WebSocketSharp.Server;

namespace McpPlaytest
{
    [InitializeOnLoad]
    public class PlaytestServer : IDisposable
    {
        private static PlaytestServer _instance;
        private readonly Dictionary<string, PlaytestToolBase> _tools = new Dictionary<string, PlaytestToolBase>();
        private WebSocketServer _webSocketServer;

        private const int DEFAULT_PORT = 8091;
        private const string WS_PATH = "/McpPlaytest";
        private const string SESSION_KEY_WAS_RUNNING = "McpPlaytest_WasRunning";

        public ConcurrentDictionary<string, string> Clients { get; } = new ConcurrentDictionary<string, string>();
        public bool IsListening => _webSocketServer?.IsListening ?? false;

        [DidReloadScripts]
        private static void AfterReload()
        {
            if (Application.isBatchMode) return;

            // Always ensure instance exists after reload
            var inst = Instance;

            // If the server was running before domain reload, restart it
            if (SessionState.GetBool(SESSION_KEY_WAS_RUNNING, false))
            {
                if (!inst.IsListening)
                {
                    inst.StartServer();
                }
            }
        }

        public static PlaytestServer Instance
        {
            get
            {
                if (Application.isBatchMode) return null;
                if (_instance == null)
                {
                    _instance = new PlaytestServer();
                }
                return _instance;
            }
        }

        private PlaytestServer()
        {
            if (Application.isBatchMode) return;

            EditorApplication.quitting -= OnEditorQuitting;
            EditorApplication.quitting += OnEditorQuitting;

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            RegisterTools();
            StartServer();
        }

        public void Dispose()
        {
            StopServer();
            EditorApplication.quitting -= OnEditorQuitting;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            GC.SuppressFinalize(this);
        }

        public void StartServer()
        {
            if (IsListening)
            {
                Debug.Log($"[McpPlaytest] Server already listening on port {DEFAULT_PORT}");
                return;
            }

            try
            {
                _webSocketServer = new WebSocketServer($"ws://localhost:{DEFAULT_PORT}");
                _webSocketServer.ReuseAddress = true;
                _webSocketServer.AddWebSocketService(WS_PATH, () => new PlaytestSocketHandler(this));
                _webSocketServer.Start();
                SessionState.SetBool(SESSION_KEY_WAS_RUNNING, true);
                Debug.Log($"[McpPlaytest] WebSocket server started on port {DEFAULT_PORT}");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Debug.LogError($"[McpPlaytest] Port {DEFAULT_PORT} already in use: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpPlaytest] Failed to start server: {ex.Message}");
            }
        }

        public void StopServer()
        {
            if (!IsListening) return;

            try
            {
                _webSocketServer?.Stop();
                Debug.Log("[McpPlaytest] WebSocket server stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpPlaytest] Error stopping server: {ex.Message}");
            }
            finally
            {
                _webSocketServer = null;
                Clients.Clear();
            }
        }

        public bool TryGetTool(string name, out PlaytestToolBase tool)
        {
            return _tools.TryGetValue(name, out tool);
        }

        private void RegisterTools()
        {
            // Play Mode control
            var playModeControl = new PlayModeControlTool();
            _tools.Add(playModeControl.Name, playModeControl);

            // Screenshot capture
            var captureScreenshot = new CaptureScreenshotTool();
            _tools.Add(captureScreenshot.Name, captureScreenshot);

            // Input simulation
            var simulateInput = new SimulateInputTool();
            _tools.Add(simulateInput.Name, simulateInput);

            // Game state query
            var queryGameState = new QueryGameStateTool();
            _tools.Add(queryGameState.Name, queryGameState);

            // Console logs
            var getConsoleLogs = new GetConsoleLogsTool();
            _tools.Add(getConsoleLogs.Name, getConsoleLogs);

            // Video recording
            var recordVideo = new RecordVideoTool();
            _tools.Add(recordVideo.Name, recordVideo);
        }

        // KEY DIFFERENCE: We do NOT stop on ExitingEditMode!
        // We persist through Play Mode by saving state and restarting after domain reload
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (Application.isBatchMode || _instance == null) return;

            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    // Save that we were running so we restart after domain reload
                    if (_instance.IsListening)
                    {
                        SessionState.SetBool(SESSION_KEY_WAS_RUNNING, true);
                    }
                    // Stop the server before domain reload destroys it
                    _instance.StopServer();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    // After domain reload in Play Mode, restart if we were running
                    // This is handled by [DidReloadScripts] but also check here as a safety net
                    if (SessionState.GetBool(SESSION_KEY_WAS_RUNNING, false) && !_instance.IsListening)
                    {
                        _instance.StartServer();
                    }
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    // Keep running, will restart after domain reload
                    if (_instance.IsListening)
                    {
                        SessionState.SetBool(SESSION_KEY_WAS_RUNNING, true);
                    }
                    _instance.StopServer();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    // Back in edit mode, restart if we were running
                    if (SessionState.GetBool(SESSION_KEY_WAS_RUNNING, false) && !_instance.IsListening)
                    {
                        _instance.StartServer();
                    }
                    break;
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            if (Application.isBatchMode || _instance == null) return;

            if (_instance.IsListening)
            {
                SessionState.SetBool(SESSION_KEY_WAS_RUNNING, true);
                _instance.StopServer();
            }
        }

        private static void OnEditorQuitting()
        {
            if (Application.isBatchMode || _instance == null) return;
            SessionState.SetBool(SESSION_KEY_WAS_RUNNING, false);
            _instance.Dispose();
        }
    }
}

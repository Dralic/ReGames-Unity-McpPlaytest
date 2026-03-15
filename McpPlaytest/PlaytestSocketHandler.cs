using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace McpPlaytest
{
    public class PlaytestSocketHandler : WebSocketBehavior
    {
        private readonly PlaytestServer _server;

        public PlaytestSocketHandler(PlaytestServer server)
        {
            _server = server;
        }

        public static JObject CreateErrorResponse(string message, string errorType)
        {
            return new JObject
            {
                ["error"] = new JObject
                {
                    ["type"] = errorType,
                    ["message"] = message
                }
            };
        }

        protected override async void OnMessage(MessageEventArgs e)
        {
            try
            {
                JObject requestJson;
                try
                {
                    requestJson = JObject.Parse(e.Data);
                }
                catch (JsonReaderException jre)
                {
                    Send(CreateResponse(null, CreateErrorResponse($"Invalid JSON: {jre.Message}", "invalid_json")).ToString(Formatting.None));
                    return;
                }

                var method = requestJson["method"]?.ToString();
                var parameters = requestJson["params"] as JObject ?? new JObject();
                var requestId = requestJson["id"]?.ToString();

                var tcs = new TaskCompletionSource<JObject>();

                if (string.IsNullOrEmpty(method))
                {
                    tcs.SetResult(CreateErrorResponse("Missing method in request", "invalid_request"));
                }
                else if (_server.TryGetTool(method, out var tool))
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteTool(tool, parameters, tcs));
                }
                else
                {
                    tcs.SetResult(CreateErrorResponse($"Unknown method: {method}", "unknown_method"));
                }

                JObject responseJson = await tcs.Task;
                JObject jsonRpcResponse = CreateResponse(requestId, responseJson);
                Send(jsonRpcResponse.ToString(Formatting.None));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpPlaytest] Error processing message: {ex.Message}");
                Send(CreateErrorResponse($"Internal error: {ex.Message}", "internal_error").ToString(Formatting.None));
            }
        }

        protected override void OnOpen()
        {
            var inactiveIds = Sessions.InactiveIDs.ToList();
            foreach (var oldId in inactiveIds)
            {
                _server.Clients.TryRemove(oldId, out _);
                try
                {
                    Sessions.CloseSession(oldId, CloseStatusCode.Normal, "Stale session cleanup");
                }
                catch (Exception) { }
            }

            string clientName = "";
            NameValueCollection headers = Context.Headers;
            if (headers != null && headers.Contains("X-Client-Name"))
            {
                clientName = headers["X-Client-Name"];
            }

            _server.Clients[ID] = clientName;
            Debug.Log($"[McpPlaytest] Client connected (ID: {ID}, Total: {_server.Clients.Count})");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            _server.Clients.TryRemove(ID, out _);
            Debug.Log($"[McpPlaytest] Client disconnected (Remaining: {_server.Clients.Count})");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.LogError($"[McpPlaytest] WebSocket error: {e.Message}");
        }

        private IEnumerator ExecuteTool(PlaytestToolBase tool, JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            try
            {
                if (tool.IsAsync)
                {
                    tool.ExecuteAsync(parameters, tcs);
                }
                else
                {
                    var result = tool.Execute(parameters);
                    tcs.SetResult(result);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpPlaytest] Error executing tool {tool.Name}: {ex.Message}\n{ex.StackTrace}");
                tcs.SetResult(CreateErrorResponse($"Failed to execute {tool.Name}: {ex.Message}", "tool_execution_error"));
            }

            yield return null;
        }

        private JObject CreateResponse(string requestId, JObject result)
        {
            var response = new JObject
            {
                ["id"] = requestId
            };

            if (result.TryGetValue("error", out var errorObj))
            {
                response["error"] = errorObj;
            }
            else
            {
                response["result"] = result;
            }

            return response;
        }
    }
}

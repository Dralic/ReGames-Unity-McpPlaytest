using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpPlaytest
{
    public class SimulateInputTool : PlaytestToolBase
    {
        public SimulateInputTool()
        {
            this.Name = "simulate_input";
            this.Description = "Simulate player input: movement (x,y), or button presses (melee_attack, ranged_attack, throw_attack, special_ability)";
            this.IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            if (!EditorApplication.isPlaying)
            {
                tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse("Cannot simulate input: not in Play Mode", "invalid_state"));
                return;
            }

            var action = parameters["action"]?.ToString();
            int playerIndex = parameters["playerIndex"]?.ToObject<int>() ?? 0;
            float duration = parameters["duration"]?.ToObject<float>() ?? 0.1f;

            if (string.IsNullOrEmpty(action))
            {
                tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse("Missing 'action' parameter", "validation_error"));
                return;
            }

            EditorApplication.delayCall += () =>
            {
                try
                {
                    var result = InputSimulator.SimulateAction(action, playerIndex, parameters["value"] as JObject, duration);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse($"Input simulation failed: {ex.Message}", "simulation_error"));
                }
            };
        }
    }
}

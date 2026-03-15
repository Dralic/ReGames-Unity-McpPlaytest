using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace McpPlaytest
{
    public class SpawnPlayerTool : PlaytestToolBase
    {
        public SpawnPlayerTool()
        {
            this.Name = "spawn_player";
            this.Description = "Spawn a new player with a virtual gamepad device via PlayerInputManager. The spawned player can then be controlled via simulate_input.";
            this.IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            if (!EditorApplication.isPlaying)
            {
                tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse("Cannot spawn player: not in Play Mode", "invalid_state"));
                return;
            }

            EditorApplication.delayCall += () =>
            {
                try
                {
                    var result = SpawnNewPlayer();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetResult(PlaytestSocketHandler.CreateErrorResponse($"Failed to spawn player: {ex.Message}", "spawn_error"));
                }
            };
        }

        private static JObject SpawnNewPlayer()
        {
            var playerInputManager = UnityEngine.Object.FindAnyObjectByType<PlayerInputManager>();

            if (playerInputManager == null)
            {
                return PlaytestSocketHandler.CreateErrorResponse("PlayerInputManager not found in scene", "invalid_state");
            }

            if (playerInputManager.playerPrefab == null)
            {
                return PlaytestSocketHandler.CreateErrorResponse("PlayerInputManager has no player prefab assigned", "invalid_state");
            }

            if (!playerInputManager.joiningEnabled)
            {
                return PlaytestSocketHandler.CreateErrorResponse("PlayerInputManager joining is currently disabled", "invalid_state");
            }

            int maxPlayers = playerInputManager.maxPlayerCount;
            int currentCount = PlayerInput.all.Count;

            if (maxPlayers > 0 && currentCount >= maxPlayers)
            {
                return PlaytestSocketHandler.CreateErrorResponse($"Max player count ({maxPlayers}) reached", "invalid_state");
            }

            // Create a virtual gamepad for the new player
            var virtualGamepad = InputSystem.AddDevice<Gamepad>();

            // Use JoinPlayer which respects PlayerInputManager rules and fires onPlayerJoined
            PlayerInput newPlayer = playerInputManager.JoinPlayer(
                currentCount,
                -1,
                null,
                virtualGamepad
            );

            if (newPlayer == null)
            {
                InputSystem.RemoveDevice(virtualGamepad);
                return PlaytestSocketHandler.CreateErrorResponse("PlayerInputManager.JoinPlayer returned null", "spawn_error");
            }

            int countAfter = PlayerInput.all.Count;

            return new JObject
            {
                ["success"] = true,
                ["message"] = $"Player {currentCount} spawned successfully",
                ["playerIndex"] = currentCount,
                ["totalPlayers"] = countAfter
            };
        }
    }
}

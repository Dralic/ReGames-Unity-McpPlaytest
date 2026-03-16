using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace McpPlaytest
{
    public class QueryGameStateTool : PlaytestToolBase
    {
        public QueryGameStateTool()
        {
            this.Name = "query_game_state";
            this.Description = "Query runtime game state: full, players, round, or game_mode";
        }

        public override JObject Execute(JObject parameters)
        {
            var query = parameters["query"]?.ToString() ?? "full";

            var bridge = PlaytestBridge.GameState;
            if (bridge != null)
            {
                switch (query)
                {
                    case "full":
                        return bridge.QueryFullState();
                    case "players":
                        return bridge.QueryPlayers();
                    case "round":
                        return bridge.QueryRound();
                    case "game_mode":
                        return bridge.QueryGameMode();
                    default:
                        return PlaytestSocketHandler.CreateErrorResponse($"Unknown query: {query}. Valid: full, players, round, game_mode", "validation_error");
                }
            }

            // Generic fallback when no bridge is registered
            switch (query)
            {
                case "full":
                    return this.QueryGenericFullState();
                case "players":
                    return this.QueryGenericPlayers();
                case "round":
                    return new JObject
                    {
                        ["success"] = true,
                        ["bridgeAvailable"] = false,
                        ["round"] = new JObject { ["available"] = false }
                    };
                case "game_mode":
                    return new JObject
                    {
                        ["success"] = true,
                        ["bridgeAvailable"] = false,
                        ["gameMode"] = new JObject { ["available"] = false }
                    };
                default:
                    return PlaytestSocketHandler.CreateErrorResponse($"Unknown query: {query}. Valid: full, players, round, game_mode", "validation_error");
            }
        }

        private JObject QueryGenericFullState()
        {
            bool isPlayMode = UnityEditor.EditorApplication.isPlaying;
            var activeScene = SceneManager.GetActiveScene();
            var players = this.BuildPlayerInputArray();

            return new JObject
            {
                ["success"] = true,
                ["bridgeAvailable"] = false,
                ["isPlayMode"] = isPlayMode,
                ["scene"] = new JObject
                {
                    ["name"] = activeScene.name,
                    ["rootObjectCount"] = activeScene.rootCount
                },
                ["players"] = players
            };
        }

        private JObject QueryGenericPlayers()
        {
            var players = this.BuildPlayerInputArray();

            return new JObject
            {
                ["success"] = true,
                ["bridgeAvailable"] = false,
                ["players"] = players
            };
        }

        private JArray BuildPlayerInputArray()
        {
            var playerInputs = Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
            var players = new JArray();

            foreach (var pi in playerInputs)
            {
                var deviceNames = new JArray();
                foreach (var device in pi.devices)
                {
                    deviceNames.Add(device.displayName);
                }

                var position = pi.transform.position;

                players.Add(new JObject
                {
                    ["name"] = pi.gameObject.name,
                    ["playerIndex"] = pi.playerIndex,
                    ["position"] = new JObject
                    {
                        ["x"] = position.x,
                        ["y"] = position.y,
                        ["z"] = position.z
                    },
                    ["isActive"] = pi.gameObject.activeInHierarchy,
                    ["pairedDevices"] = deviceNames
                });
            }

            return players;
        }
    }
}

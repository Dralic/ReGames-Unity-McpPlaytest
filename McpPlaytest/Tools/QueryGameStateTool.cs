using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

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

            // GameManager is a runtime singleton, accessible from editor code during Play Mode
            var gm = GameManager.instance;

            switch (query)
            {
                case "full":
                    return this.BuildFullState(gm);
                case "players":
                    return this.BuildPlayersState(gm);
                case "round":
                    return this.BuildRoundState(gm);
                case "game_mode":
                    return this.BuildGameModeState(gm);
                default:
                    return PlaytestSocketHandler.CreateErrorResponse($"Unknown query: {query}. Valid: full, players, round, game_mode", "validation_error");
            }
        }

        private JObject BuildFullState(GameManager gm)
        {
            if (gm == null)
            {
                return new JObject
                {
                    ["success"] = true,
                    ["isPlayMode"] = UnityEditor.EditorApplication.isPlaying,
                    ["gameManagerFound"] = false,
                    ["message"] = "GameManager not found. Is a scene with GameManager loaded?"
                };
            }

            var result = new JObject
            {
                ["success"] = true,
                ["isPlayMode"] = UnityEditor.EditorApplication.isPlaying,
                ["gameManagerFound"] = true,
                ["currentState"] = gm.currentState.ToString(),
                ["currentRoundNumber"] = gm.currentRoundNumber,
                ["alivePlayerCount"] = gm.GetAlivePlayerCount(),
                ["totalPlayerCount"] = gm.players?.Count ?? 0
            };

            result["players"] = this.BuildPlayersArray(gm);
            result["round"] = this.BuildRoundInfo(gm);
            result["gameMode"] = this.BuildGameModeInfo(gm);

            return result;
        }

        private JObject BuildPlayersState(GameManager gm)
        {
            if (gm == null)
            {
                return new JObject
                {
                    ["success"] = true,
                    ["gameManagerFound"] = false,
                    ["players"] = new JArray()
                };
            }

            return new JObject
            {
                ["success"] = true,
                ["gameManagerFound"] = true,
                ["players"] = this.BuildPlayersArray(gm)
            };
        }

        private JObject BuildRoundState(GameManager gm)
        {
            if (gm == null)
            {
                return new JObject
                {
                    ["success"] = true,
                    ["gameManagerFound"] = false
                };
            }

            return new JObject
            {
                ["success"] = true,
                ["gameManagerFound"] = true,
                ["round"] = this.BuildRoundInfo(gm)
            };
        }

        private JObject BuildGameModeState(GameManager gm)
        {
            if (gm == null)
            {
                return new JObject
                {
                    ["success"] = true,
                    ["gameManagerFound"] = false
                };
            }

            return new JObject
            {
                ["success"] = true,
                ["gameManagerFound"] = true,
                ["gameMode"] = this.BuildGameModeInfo(gm)
            };
        }

        private JArray BuildPlayersArray(GameManager gm)
        {
            var playersArray = new JArray();

            if (gm.players == null) return playersArray;

            for (int i = 0; i < gm.players.Count; i++)
            {
                var pd = gm.players[i];
                var playerObj = new JObject
                {
                    ["playerIndex"] = pd.playerIndex,
                    ["score"] = pd.score,
                    ["kills"] = pd.kills,
                    ["deaths"] = pd.deaths,
                    ["suicides"] = pd.suicides,
                    ["isReady"] = pd.isReady
                };

                if (pd.controller != null)
                {
                    var pos = pd.controller.transform.position;
                    playerObj["position"] = new JObject
                    {
                        ["x"] = pos.x,
                        ["y"] = pos.y,
                        ["z"] = pos.z
                    };

                    var currentState = pd.controller.stateMachine?.currentState;
                    playerObj["currentState"] = currentState?.GetType().Name ?? "Unknown";
                    playerObj["isAlive"] = currentState != pd.controller.playerDeathState;
                    playerObj["isActive"] = pd.controller.gameObject.activeSelf;
                }
                else
                {
                    playerObj["position"] = null;
                    playerObj["currentState"] = "NoController";
                    playerObj["isAlive"] = false;
                    playerObj["isActive"] = false;
                }

                playersArray.Add(playerObj);
            }

            return playersArray;
        }

        private JObject BuildRoundInfo(GameManager gm)
        {
            var roundObj = new JObject
            {
                ["currentRoundNumber"] = gm.currentRoundNumber,
                ["currentState"] = gm.currentState.ToString()
            };

            if (gm.currentRound != null)
            {
                roundObj["elapsedTime"] = gm.currentRound.elapsedTime;
                roundObj["remainingTime"] = gm.currentRound.remainingTime;
            }

            return roundObj;
        }

        private JObject BuildGameModeInfo(GameManager gm)
        {
            if (gm.gameMode == null)
            {
                return new JObject { ["available"] = false };
            }

            var mode = gm.gameMode;
            return new JObject
            {
                ["available"] = true,
                ["modeName"] = mode.modeName,
                ["description"] = mode.description,
                ["numberOfRounds"] = mode.numberOfRounds,
                ["roundDuration"] = mode.roundDuration,
                ["roundStartDelay"] = mode.roundStartDelay,
                ["roundEndDelay"] = mode.roundEndDelay,
                ["pointsPerKill"] = mode.pointsPerKill,
                ["suicidePenalty"] = mode.suicidePenalty,
                ["respawnDelay"] = mode.respawnDelay,
                ["maxPlayers"] = mode.maxPlayers,
                ["useLastManStanding"] = mode.useLastManStanding,
                ["allowMeleeWeapons"] = mode.allowMeleeWeapons,
                ["allowRangedWeapons"] = mode.allowRangedWeapons,
                ["allowThrowWeapons"] = mode.allowThrowWeapons,
                ["allowSpecialAbility"] = mode.allowSpecialAbility
            };
        }
    }
}

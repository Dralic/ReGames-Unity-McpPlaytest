using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace McpPlaytest
{
    /// <summary>
    /// BomberSquad-specific implementation of the playtest bridge interfaces.
    /// Auto-registers when entering Play Mode via [InitializeOnLoad].
    ///
    /// For other projects: create your own class implementing IPlaytestGameState
    /// and/or IPlaytestInputReceiver and register via PlaytestBridge.RegisterGameState()
    /// and PlaytestBridge.RegisterInputReceiver().
    /// </summary>
    [InitializeOnLoad]
    public class BomberSquadPlaytestBridge : IPlaytestGameState, IPlaytestInputReceiver
    {
        private static BomberSquadPlaytestBridge _instance;

        static BomberSquadPlaytestBridge()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _instance = new BomberSquadPlaytestBridge();
                PlaytestBridge.RegisterGameState(_instance);
                PlaytestBridge.RegisterInputReceiver(_instance);
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                PlaytestBridge.Clear();
                _instance = null;
            }
        }

        // ─────────────────────────────────────────────
        // IPlaytestGameState
        // ─────────────────────────────────────────────

        public JObject QueryFullState()
        {
            var gm = GameManager.instance;
            if (gm == null)
            {
                return new JObject
                {
                    ["success"] = true,
                    ["isPlayMode"] = EditorApplication.isPlaying,
                    ["gameManagerFound"] = false,
                    ["message"] = "GameManager not found. Is a scene with GameManager loaded?"
                };
            }

            var result = new JObject
            {
                ["success"] = true,
                ["isPlayMode"] = EditorApplication.isPlaying,
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

        public JObject QueryPlayers()
        {
            var gm = GameManager.instance;
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

        public JObject QueryRound()
        {
            var gm = GameManager.instance;
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

        public JObject QueryGameMode()
        {
            var gm = GameManager.instance;
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

        // ─────────────────────────────────────────────
        // IPlaytestInputReceiver
        // ─────────────────────────────────────────────

        public PlayerInput FindPlayerInput(int playerIndex)
        {
            var controller = this.FindPlayerController(playerIndex);
            if (controller == null) return null;

            return controller.playerInput;
        }

        public JObject SimulateButtonPress(int playerIndex, string action)
        {
            var controller = this.FindPlayerController(playerIndex);
            if (controller == null) return null;

            string fieldName = action switch
            {
                "melee_attack" => "meleeAttackPressed",
                "ranged_attack" => "rangedAttackPressed",
                "throw_attack" => "throwAttackPressed",
                "special_ability" => "specialAbilityPressed",
                _ => null
            };

            if (fieldName == null) return null;

            var field = typeof(PlayerController).GetField($"<{fieldName}>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(controller, true);
                return new JObject
                {
                    ["success"] = true,
                    ["action"] = action,
                    ["playerIndex"] = playerIndex,
                    ["method"] = "reflection"
                };
            }

            return null;
        }

        private PlayerController FindPlayerController(int playerIndex)
        {
            var gm = GameManager.instance;
            if (gm != null && gm.players != null)
            {
                for (int i = 0; i < gm.players.Count; i++)
                {
                    if (gm.players[i].playerIndex == playerIndex && gm.players[i].controller != null)
                    {
                        return gm.players[i].controller;
                    }
                }
            }

            var controllers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var ctrl in controllers)
            {
                if (ctrl.playerIndex == playerIndex)
                {
                    return ctrl;
                }
            }

            return null;
        }
    }
}

using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace McpPlaytest
{
    public static class InputSimulator
    {
        public static JObject SimulateAction(string action, int playerIndex, JObject value, float duration)
        {
            switch (action)
            {
                case "move":
                    return SimulateMove(playerIndex, value, duration);
                case "melee_attack":
                case "ranged_attack":
                case "throw_attack":
                case "special_ability":
                    return SimulateButton(playerIndex, action);
                case "stop":
                    return SimulateMove(playerIndex, null, 0f);
                default:
                    return PlaytestSocketHandler.CreateErrorResponse($"Unknown action: {action}. Valid: move, melee_attack, ranged_attack, throw_attack, special_ability, stop", "validation_error");
            }
        }

        private static JObject SimulateMove(int playerIndex, JObject value, float duration)
        {
            float x = value?["x"]?.ToObject<float>() ?? 0f;
            float y = value?["y"]?.ToObject<float>() ?? 0f;

            var playerInput = FindPlayerInput(playerIndex);
            if (playerInput == null)
            {
                return PlaytestSocketHandler.CreateErrorResponse($"Player {playerIndex} not found", "player_not_found");
            }

            // Try to find a gamepad among paired devices
            Gamepad gamepad = FindGamepad(playerInput);

            if (gamepad != null)
            {
                InputState.Change(gamepad.leftStick, new Vector2(x, y));

                if (duration > 0f)
                {
                    ScheduleMoveReset(gamepad, playerIndex, duration);
                }

                return new JObject
                {
                    ["success"] = true,
                    ["action"] = "move",
                    ["playerIndex"] = playerIndex,
                    ["appliedValue"] = new JObject { ["x"] = x, ["y"] = y },
                    ["duration"] = duration,
                    ["method"] = "gamepad_stick"
                };
            }

            // No gamepad found - try to find any device with a stick/vector2 control for Move action
            var moveAction = playerInput.actions?.FindAction("Move");
            if (moveAction != null)
            {
                foreach (var control in moveAction.controls)
                {
                    if (control is Vector2Control vec2)
                    {
                        InputState.Change(vec2, new Vector2(x, y));

                        return new JObject
                        {
                            ["success"] = true,
                            ["action"] = "move",
                            ["playerIndex"] = playerIndex,
                            ["appliedValue"] = new JObject { ["x"] = x, ["y"] = y },
                            ["duration"] = duration,
                            ["method"] = "action_control"
                        };
                    }
                }
            }

            return PlaytestSocketHandler.CreateErrorResponse("Could not inject movement input: no suitable device or control found", "input_error");
        }

        private static JObject SimulateButton(int playerIndex, string action)
        {
            // Try the bridge interface first for game-specific button handling
            var bridge = FindInputReceiverBridge();
            if (bridge != null)
            {
                var bridgeResult = bridge.SimulateButtonPress(playerIndex, action);
                if (bridgeResult != null && bridgeResult["success"]?.ToObject<bool>() == true)
                {
                    return bridgeResult;
                }
            }

            // Fall back to generic gamepad button simulation
            var playerInput = FindPlayerInput(playerIndex);
            if (playerInput == null)
            {
                return PlaytestSocketHandler.CreateErrorResponse($"Player {playerIndex} not found", "player_not_found");
            }

            Gamepad gamepad = FindGamepad(playerInput);
            if (gamepad != null)
            {
                InputControl button = MapActionToGamepadButton(gamepad, action);

                if (button is ButtonControl btn)
                {
                    InputState.Change(btn, 1f);

                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        InputState.Change(btn, 0f);
                    };

                    return new JObject
                    {
                        ["success"] = true,
                        ["action"] = action,
                        ["playerIndex"] = playerIndex,
                        ["method"] = "gamepad_button"
                    };
                }
            }

            // Last resort: try to trigger the action directly via its bound controls
            var inputAction = FindInputAction(playerInput, action);
            if (inputAction != null)
            {
                foreach (var control in inputAction.controls)
                {
                    if (control is ButtonControl actionBtn)
                    {
                        InputState.Change(actionBtn, 1f);

                        UnityEditor.EditorApplication.delayCall += () =>
                        {
                            InputState.Change(actionBtn, 0f);
                        };

                        return new JObject
                        {
                            ["success"] = true,
                            ["action"] = action,
                            ["playerIndex"] = playerIndex,
                            ["method"] = "action_control"
                        };
                    }
                }
            }

            return PlaytestSocketHandler.CreateErrorResponse($"Could not simulate {action}: no suitable device, control, or IPlaytestInputReceiver found", "input_error");
        }

        private static InputAction FindInputAction(PlayerInput playerInput, string action)
        {
            if (playerInput.actions == null) return null;

            // Try common naming conventions for the action
            // Capitalize first letter of each segment
            string[] parts = action.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                }
            }
            string joined = string.Join("", parts);

            var found = playerInput.actions.FindAction(joined);
            if (found != null) return found;

            found = playerInput.actions.FindAction(action);
            if (found != null) return found;

            return null;
        }

        private static InputControl MapActionToGamepadButton(Gamepad gamepad, string action)
        {
            switch (action)
            {
                case "melee_attack": return gamepad.buttonEast;
                case "ranged_attack": return gamepad.buttonSouth;
                case "throw_attack": return gamepad.buttonWest;
                case "special_ability": return gamepad.buttonNorth;
                default: return null;
            }
        }

        private static void ScheduleMoveReset(Gamepad gamepad, int playerIndex, float duration)
        {
            float endTime = Time.realtimeSinceStartup + duration;

            void ResetCheck()
            {
                if (Time.realtimeSinceStartup >= endTime)
                {
                    UnityEditor.EditorApplication.update -= ResetCheck;
                    try
                    {
                        InputState.Change(gamepad.leftStick, Vector2.zero);
                    }
                    catch (Exception) { }
                }
            }

            UnityEditor.EditorApplication.update += ResetCheck;
        }

        private static PlayerInput FindPlayerInput(int playerIndex)
        {
            // Try the bridge interface first (game-specific lookup)
            var bridge = FindInputReceiverBridge();
            if (bridge != null)
            {
                var found = bridge.FindPlayerInput(playerIndex);
                if (found != null) return found;
            }

            // Fallback: search joined players via PlayerInput.all matching by playerIndex
            foreach (var pi in PlayerInput.all)
            {
                if (pi.playerIndex == playerIndex)
                {
                    return pi;
                }
            }

            // Last resort: find pre-placed PlayerInput components that haven't "joined"
            // (their playerIndex is -1), match by array index instead
            var allInputs = UnityEngine.Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
            if (playerIndex >= 0 && playerIndex < allInputs.Length)
            {
                return allInputs[playerIndex];
            }

            return null;
        }

        private static Gamepad FindGamepad(PlayerInput playerInput)
        {
            if (playerInput == null) return null;

            var devices = playerInput.devices;
            foreach (var device in devices)
            {
                if (device is Gamepad gp)
                {
                    return gp;
                }
            }

            return null;
        }

        private static IPlaytestInputReceiver FindInputReceiverBridge()
        {
            return PlaytestBridge.InputReceiver;
        }
    }
}

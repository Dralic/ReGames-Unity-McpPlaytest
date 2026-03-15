using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace McpPlaytest
{
    public static class InputSimulator
    {
        private static readonly Dictionary<int, InputSimulationState> _activeSimulations = new Dictionary<int, InputSimulationState>();

        private class InputSimulationState
        {
            public PlayerController controller;
            public string action;
            public float endTime;
            public Vector2 moveValue;
        }

        public static JObject SimulateAction(string action, int playerIndex, JObject value, float duration)
        {
            var controller = FindPlayerController(playerIndex);
            if (controller == null)
            {
                return PlaytestSocketHandler.CreateErrorResponse($"Player {playerIndex} not found", "player_not_found");
            }

            switch (action)
            {
                case "move":
                    return SimulateMove(controller, playerIndex, value, duration);
                case "melee_attack":
                    return SimulateButtonPress(controller, "OnMeleeAttack");
                case "ranged_attack":
                    return SimulateButtonPress(controller, "OnRangedAttack");
                case "throw_attack":
                    return SimulateButtonPress(controller, "OnThrowAttack");
                case "special_ability":
                    return SimulateButtonPress(controller, "OnSpecialAbility");
                case "stop":
                    return SimulateMove(controller, playerIndex, null, 0f);
                default:
                    return PlaytestSocketHandler.CreateErrorResponse($"Unknown action: {action}. Valid: move, melee_attack, ranged_attack, throw_attack, special_ability, stop", "validation_error");
            }
        }

        private static JObject SimulateMove(PlayerController controller, int playerIndex, JObject value, float duration)
        {
            float x = value?["x"]?.ToObject<float>() ?? 0f;
            float y = value?["y"]?.ToObject<float>() ?? 0f;

            // Use the PlayerInput component to inject input via the Input System
            var playerInput = controller.playerInput;
            if (playerInput == null)
            {
                return PlaytestSocketHandler.CreateErrorResponse("PlayerInput component not found", "input_error");
            }

            // Find the device paired to this player
            var devices = playerInput.devices;
            if (devices.Count == 0)
            {
                // If no device is paired, try using InputSystem to queue a virtual gamepad event
                return SimulateMoveViaAction(controller, x, y, duration, playerIndex);
            }

            // Try to find a gamepad among paired devices
            Gamepad gamepad = null;
            foreach (var device in devices)
            {
                if (device is Gamepad gp) { gamepad = gp; break; }
            }

            if (gamepad != null)
            {
                // Change the gamepad's left stick state directly
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

            // Fallback to action-based simulation
            return SimulateMoveViaAction(controller, x, y, duration, playerIndex);
        }

        private static JObject SimulateMoveViaAction(PlayerController controller, float x, float y, float duration, int playerIndex)
        {
            // Directly set the moveInput through the PlayerInput action
            var playerInput = controller.playerInput;
            if (playerInput != null)
            {
                var moveAction = playerInput.actions?.FindAction("Move");
                if (moveAction != null)
                {
                    // We can't easily inject values into actions without a device,
                    // so use reflection to set moveInput directly
                    var moveInputField = typeof(PlayerController).GetProperty("moveInput");
                    if (moveInputField != null && moveInputField.CanWrite)
                    {
                        moveInputField.SetValue(controller, new Vector2(x, y));
                    }
                    else
                    {
                        // Use the private backing field via reflection
                        var field = typeof(PlayerController).GetField("<moveInput>k__BackingField",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(controller, new Vector2(x, y));
                        }
                    }

                    if (duration > 0f)
                    {
                        _activeSimulations[playerIndex] = new InputSimulationState
                        {
                            controller = controller,
                            action = "move",
                            endTime = Time.realtimeSinceStartup + duration,
                            moveValue = new Vector2(x, y)
                        };

                        UnityEditor.EditorApplication.update += CheckSimulationExpiry;
                    }

                    return new JObject
                    {
                        ["success"] = true,
                        ["action"] = "move",
                        ["playerIndex"] = playerIndex,
                        ["appliedValue"] = new JObject { ["x"] = x, ["y"] = y },
                        ["duration"] = duration,
                        ["method"] = "reflection"
                    };
                }
            }

            return PlaytestSocketHandler.CreateErrorResponse("Could not inject movement input", "input_error");
        }

        private static JObject SimulateButtonPress(PlayerController controller, string callbackName)
        {
            // Use the PlayerInput component's device to simulate a button press
            var playerInput = controller.playerInput;
            if (playerInput != null)
            {
                var devices = playerInput.devices;
                Gamepad gamepad = null;
                foreach (var device in devices)
                {
                    if (device is Gamepad gp) { gamepad = gp; break; }
                }

                if (gamepad != null)
                {
                    // Map callback names to gamepad buttons
                    InputControl button = null;
                    switch (callbackName)
                    {
                        case "OnMeleeAttack": button = gamepad.buttonEast; break;
                        case "OnRangedAttack": button = gamepad.buttonSouth; break;
                        case "OnThrowAttack": button = gamepad.buttonWest; break;
                        case "OnSpecialAbility": button = gamepad.buttonNorth; break;
                    }

                    if (button is ButtonControl btn)
                    {
                        // Press then release
                        InputState.Change(btn, 1f);

                        UnityEditor.EditorApplication.delayCall += () =>
                        {
                            InputState.Change(btn, 0f);
                        };

                        return new JObject
                        {
                            ["success"] = true,
                            ["action"] = callbackName.Replace("On", "").ToLower(),
                            ["playerIndex"] = controller.playerIndex,
                            ["method"] = "gamepad_button"
                        };
                    }
                }
            }

            // Fallback: use reflection to set the pressed bool directly
            string fieldName = callbackName switch
            {
                "OnMeleeAttack" => "meleeAttackPressed",
                "OnRangedAttack" => "rangedAttackPressed",
                "OnThrowAttack" => "throwAttackPressed",
                "OnSpecialAbility" => "specialAbilityPressed",
                _ => null
            };

            if (fieldName != null)
            {
                var prop = typeof(PlayerController).GetProperty(fieldName);
                if (prop != null)
                {
                    // These are auto-properties with private set, use backing field
                    var field = typeof(PlayerController).GetField($"<{fieldName}>k__BackingField",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(controller, true);
                        return new JObject
                        {
                            ["success"] = true,
                            ["action"] = callbackName.Replace("On", "").ToLower(),
                            ["playerIndex"] = controller.playerIndex,
                            ["method"] = "reflection"
                        };
                    }
                }
            }

            return PlaytestSocketHandler.CreateErrorResponse($"Could not simulate {callbackName}", "input_error");
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

        private static void CheckSimulationExpiry()
        {
            var expired = new List<int>();

            foreach (var kvp in _activeSimulations)
            {
                if (Time.realtimeSinceStartup >= kvp.Value.endTime)
                {
                    expired.Add(kvp.Key);

                    // Reset the move input
                    var field = typeof(PlayerController).GetField("<moveInput>k__BackingField",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null && kvp.Value.controller != null)
                    {
                        field.SetValue(kvp.Value.controller, Vector2.zero);
                    }
                }
            }

            foreach (var key in expired)
            {
                _activeSimulations.Remove(key);
            }

            if (_activeSimulations.Count == 0)
            {
                UnityEditor.EditorApplication.update -= CheckSimulationExpiry;
            }
        }

        private static PlayerController FindPlayerController(int playerIndex)
        {
            // Try GameManager first
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

            // Fallback: find all PlayerControllers in scene
            var controllers = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
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

using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpPlaytest
{
    public class PlayModeControlTool : PlaytestToolBase
    {
        public PlayModeControlTool()
        {
            this.Name = "play_mode_control";
            this.Description = "Control Unity Play Mode: enter, exit, pause, unpause, or step one frame";
        }

        public override JObject Execute(JObject parameters)
        {
            var action = parameters["action"]?.ToString();

            if (string.IsNullOrEmpty(action))
            {
                return PlaytestSocketHandler.CreateErrorResponse("Missing 'action' parameter", "validation_error");
            }

            switch (action)
            {
                case "enter":
                    if (EditorApplication.isPlaying)
                    {
                        return new JObject
                        {
                            ["success"] = true,
                            ["message"] = "Already in Play Mode",
                            ["isPlaying"] = true,
                            ["isPaused"] = EditorApplication.isPaused
                        };
                    }
                    EditorApplication.isPlaying = true;
                    return new JObject
                    {
                        ["success"] = true,
                        ["message"] = "Entering Play Mode",
                        ["isPlaying"] = true,
                        ["isPaused"] = false
                    };

                case "exit":
                    if (!EditorApplication.isPlaying)
                    {
                        return new JObject
                        {
                            ["success"] = true,
                            ["message"] = "Already in Edit Mode",
                            ["isPlaying"] = false,
                            ["isPaused"] = false
                        };
                    }
                    EditorApplication.isPlaying = false;
                    return new JObject
                    {
                        ["success"] = true,
                        ["message"] = "Exiting Play Mode",
                        ["isPlaying"] = false,
                        ["isPaused"] = false
                    };

                case "pause":
                    if (!EditorApplication.isPlaying)
                    {
                        return PlaytestSocketHandler.CreateErrorResponse("Cannot pause: not in Play Mode", "invalid_state");
                    }
                    EditorApplication.isPaused = true;
                    return new JObject
                    {
                        ["success"] = true,
                        ["message"] = "Play Mode paused",
                        ["isPlaying"] = true,
                        ["isPaused"] = true
                    };

                case "unpause":
                    if (!EditorApplication.isPlaying)
                    {
                        return PlaytestSocketHandler.CreateErrorResponse("Cannot unpause: not in Play Mode", "invalid_state");
                    }
                    EditorApplication.isPaused = false;
                    return new JObject
                    {
                        ["success"] = true,
                        ["message"] = "Play Mode unpaused",
                        ["isPlaying"] = true,
                        ["isPaused"] = false
                    };

                case "step":
                    if (!EditorApplication.isPlaying)
                    {
                        return PlaytestSocketHandler.CreateErrorResponse("Cannot step: not in Play Mode", "invalid_state");
                    }
                    EditorApplication.Step();
                    return new JObject
                    {
                        ["success"] = true,
                        ["message"] = "Stepped one frame",
                        ["isPlaying"] = true,
                        ["isPaused"] = EditorApplication.isPaused
                    };

                default:
                    return PlaytestSocketHandler.CreateErrorResponse($"Unknown action: {action}. Valid: enter, exit, pause, unpause, step", "validation_error");
            }
        }
    }
}

using Newtonsoft.Json.Linq;

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
            if (bridge == null)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = "No IPlaytestGameState registered. Register a bridge via PlaytestBridge.RegisterGameState() to enable game state queries.",
                    ["isPlayMode"] = UnityEditor.EditorApplication.isPlaying
                };
            }

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
    }
}

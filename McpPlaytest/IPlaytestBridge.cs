using UnityEngine.InputSystem;
using Newtonsoft.Json.Linq;

namespace McpPlaytest
{
    /// <summary>
    /// Implement this interface to provide game state information to the MCP Playtest server.
    /// Register your implementation via PlaytestBridge.RegisterGameState().
    /// </summary>
    public interface IPlaytestGameState
    {
        JObject QueryFullState();
        JObject QueryPlayers();
        JObject QueryRound();
        JObject QueryGameMode();
    }

    /// <summary>
    /// Implement this interface to allow the MCP Playtest server to simulate input
    /// on your player controllers. Register via PlaytestBridge.RegisterInputReceiver().
    /// </summary>
    public interface IPlaytestInputReceiver
    {
        PlayerInput FindPlayerInput(int playerIndex);
        JObject SimulateButtonPress(int playerIndex, string action);
    }

    /// <summary>
    /// Static registry for playtest bridge implementations.
    /// Game-specific bridges register themselves here (typically in [InitializeOnLoad] or on play mode enter).
    /// </summary>
    public static class PlaytestBridge
    {
        public static IPlaytestGameState GameState { get; private set; }
        public static IPlaytestInputReceiver InputReceiver { get; private set; }

        public static void RegisterGameState(IPlaytestGameState bridge)
        {
            GameState = bridge;
        }

        public static void RegisterInputReceiver(IPlaytestInputReceiver bridge)
        {
            InputReceiver = bridge;
        }

        public static void Clear()
        {
            GameState = null;
            InputReceiver = null;
        }
    }
}

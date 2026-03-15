# MCP Playtest Server for Unity

A custom [Model Context Protocol](https://modelcontextprotocol.io/) (MCP) server that enables AI agents to control, observe, and test Unity games during Play Mode. Built for [Claude Code](https://docs.anthropic.com/en/docs/claude-code) but compatible with any MCP client.

## Why?

Existing Unity MCP integrations (like [mcp-unity](https://github.com/CoderGamester/mcp-unity)) shut down their WebSocket server when entering Play Mode due to Unity's domain reload. This means no AI tool can interact with your game while it's actually running.

MCP Playtest solves this by persisting through Play Mode transitions using `SessionState` and `[DidReloadScripts]` to automatically restart the WebSocket server after domain reload.

## Tools

| Tool | Description |
|------|-------------|
| `play_mode_control` | Enter, exit, pause, unpause, or step one frame in Play Mode |
| `simulate_input` | Inject player input (movement, attacks) via Unity's Input System |
| `capture_screenshot` | Capture the Game View as a PNG image for AI visual analysis |
| `record_video` | Record gameplay as a sequence of screenshot frames |
| `query_game_state` | Read runtime game state: player positions, scores, round info |
| `get_playtest_logs` | Retrieve Unity console logs with filtering by type |

## Architecture

```
AI Agent <-> (stdio) <-> Node.js MCP Server <-> (WebSocket) <-> Unity Editor (C#)
```

**Two-tier design:**
- **Node.js server** (TypeScript) — speaks MCP over stdio to the AI client, forwards requests to Unity over WebSocket
- **Unity Editor scripts** (C#) — WebSocket server running inside the Unity Editor that executes tool commands and returns results

The WebSocket server runs on port `8091` by default (avoiding conflict with mcp-unity on `8090`).

## Requirements

- **Unity 6** (6000.x) with the Input System package
- **Node.js** 18+
- **WebSocketSharp** — included via the [mcp-unity](https://github.com/CoderGamester/mcp-unity) package (must be installed in your project)

## Installation

### 1. Install the mcp-unity package (dependency)

MCP Playtest uses WebSocketSharp from the mcp-unity package. Install it via Unity Package Manager:

```
https://github.com/CoderGamester/mcp-unity.git
```

### 2. Copy the McpPlaytest folder

Copy the `McpPlaytest/` folder and its `.meta` file into your project's `Assets/Editor/` directory:

```
Assets/
  Editor/
    McpPlaytest/          <-- this folder
    McpPlaytest.meta      <-- and this file
```

### 3. Build the Node.js server

```bash
cd Assets/Editor/McpPlaytest/Server~
npm install
npm run build
```

### 4. Configure your MCP client

Add the server to your `.mcp.json` (or equivalent MCP client config):

```json
{
  "mcpServers": {
    "mcp-playtest": {
      "command": "node",
      "args": [
        "<absolute-path-to-project>/Assets/Editor/McpPlaytest/Server~/build/index.js"
      ]
    }
  }
}
```

### 5. Verify

Open your Unity project. You should see in the Console:
```
[McpPlaytest] WebSocket server started on port 8091
```

## Usage Examples

**Enter Play Mode and take a screenshot:**
```
Use play_mode_control to enter play mode, then capture_screenshot to see the game.
```

**Simulate player movement:**
```
Use simulate_input with action "move" and value {x: 1, y: 0} to move the player right.
```

**Record a short gameplay clip:**
```
Use record_video start with 5 FPS for 3 seconds, wait, then stop to retrieve frames.
```

**Query game state:**
```
Use query_game_state with query "players" to see all player positions and states.
```

## Project Structure

```
McpPlaytest/
├── PlaytestServer.cs              # WebSocket server (persists through Play Mode)
├── PlaytestSocketHandler.cs       # WebSocket message routing
├── PlaytestToolBase.cs            # Base class for tool implementations
├── Tools/
│   ├── PlayModeControlTool.cs     # Enter/exit/pause/unpause/step
│   ├── SimulateInputTool.cs       # Input injection via Input System
│   ├── CaptureScreenshotTool.cs   # Game View screenshot capture
│   ├── RecordVideoTool.cs         # Frame sequence recording
│   ├── QueryGameStateTool.cs      # Runtime game state queries
│   └── GetConsoleLogsTool.cs      # Console log retrieval
├── Utils/
│   ├── InputSimulator.cs          # Input System event injection
│   └── ScreenCaptureHelper.cs     # Game View render texture capture
└── Server~/                       # Node.js MCP server (ignored by Unity)
    ├── package.json
    ├── tsconfig.json
    └── src/
        ├── index.ts               # MCP server entry point
        ├── unity/
        │   └── playtestUnity.ts   # WebSocket client to Unity
        ├── utils/
        │   ├── logger.ts
        │   └── errors.ts
        └── tools/
            ├── playModeControlTool.ts
            ├── simulateInputTool.ts
            ├── captureScreenshotTool.ts
            ├── recordVideoTool.ts
            ├── queryGameStateTool.ts
            └── getConsoleLogsTool.ts
```

## Configuration

Port and host settings are stored in `ProjectSettings/McpPlaytestSettings.json` (auto-generated on first run):

```json
{
  "Port": 8091,
  "Host": "localhost",
  "RequestTimeoutSeconds": 30
}
```

## How It Survives Play Mode

Unity destroys all C# state during domain reload when entering/exiting Play Mode. MCP Playtest handles this by:

1. Saving `WasRunning = true` to `SessionState` before the server is destroyed
2. Using `[DidReloadScripts]` to detect when scripts are reloaded after domain reload
3. Automatically restarting the WebSocket server if it was previously running
4. The Node.js side detects the WebSocket disconnection and reconnects automatically

This creates a seamless experience where the AI agent can enter Play Mode and continue communicating without manual intervention.

## Adapting to Your Game

The `QueryGameStateTool` and `SimulateInputTool` are game-specific and will need modification for your project:

- **QueryGameStateTool** — Update to reference your game's manager/state classes instead of `GameManager`
- **SimulateInputTool** — Update the action names to match your project's Input Action Asset
- **InputSimulator** — Update the device state injection to match your input action bindings

All other tools (play mode control, screenshots, video recording, console logs) are game-agnostic and work with any Unity project.

## License

MIT

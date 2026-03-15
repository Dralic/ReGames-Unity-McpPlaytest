# Claude Code Backup

Backup of the full [Claude Code](https://docs.anthropic.com/en/docs/claude-code) environment — custom hooks, agents, skills, slash commands, output styles, and settings. Designed to restore a working setup on a new machine in minutes.

## What's Included

### `.claude/` — Claude Code Configuration

Copy this folder to your home directory (`~/.claude/` or `C:\Users\<username>\.claude\`).

| Folder/File | Purpose |
|-------------|---------|
| `settings.json` | Global settings: permissions, hooks, status line, effort level |
| `CLAUDE.md` | Global instructions loaded into every conversation |
| `hooks/` | Python lifecycle scripts (pre/post tool use, notifications, session tracking, TTS, LLM utilities) |
| `status_lines/` | Status line scripts showing context window usage |
| `agents/` | Custom sub-agent definitions (builder, validator, crypto, research, TTS) |
| `commands/` | Slash commands (plan, build, cook, prime, git_status, etc.) |
| `skills/` | Skills (defuddle, obsidian-cli, obsidian-markdown, json-canvas, obsidian-bases) |
| `output-styles/` | Output format templates (bullet-points, ultra-concise, TTS, tables, etc.) |
| `projects/` | Per-project persistent memory (will regenerate as you work) |

### `Unity-Requirements.md` — Unity Project Setup Guide

Step-by-step instructions for setting up Claude Code with a Unity 6 project, including:
- MCP server configuration (mcp-unity, mcp-playtest)
- Unity packages to install
- Project-level permissions and settings
- Environment variables for API keys

## Prerequisites

Install these before restoring the backup:

| Tool | Install |
|------|---------|
| **Node.js 18+** | https://nodejs.org |
| **Python 3.11+** | https://www.python.org |
| **uv** | `pip install uv` or https://docs.astral.sh/uv |
| **Git** | https://git-scm.com |

## Restore Steps

### 1. Install Claude Code

```bash
npm install -g @anthropic-ai/claude-code
```

Run `claude` once to log in and let it create the initial `~/.claude/` folder.

### 2. Copy the backup

Copy the contents of this repo's `.claude/` folder into your `~/.claude/` directory, overwriting the defaults:

```bash
# Linux/macOS
cp -r .claude/* ~/.claude/

# Windows (PowerShell)
Copy-Item -Path .\.claude\* -Destination $env:USERPROFILE\.claude\ -Recurse -Force
```

### 3. Install global npm packages

```bash
npm install -g defuddle typescript
```

### 4. Update paths

Open `~/.claude/CLAUDE.md` and set your Obsidian vault path (if using one):

```markdown
A cross-project Obsidian vault exists at `<YOUR_VAULT_PATH>`.
```

### 5. Verify

Start Claude Code in any project directory:

```bash
claude
```

You should see the status line at the bottom and all slash commands available (type `/` to list them).

## For Unity Projects

See [Unity-Requirements.md](Unity-Requirements.md) for the full Unity-specific setup, including MCP servers, project permissions, and the MCP Playtest Server installation.

---

## MCP Playtest Server

A custom two-tier MCP server that enables AI-driven Play Mode testing, input simulation, screenshot/video capture, and game state querying — all while Unity runs as a background window.

### Why It Exists

The standard `mcp-unity` package shuts down during Play Mode domain reloads, so there is no way for an AI agent to interact with the game at runtime. The MCP Playtest Server fills that gap by surviving Play Mode transitions via `SessionState` persistence and `[DidReloadScripts]` restart.

### Architecture

```
Claude Code <-> (stdio) <-> Node.js MCP Server (TypeScript)
                                    |
                             WebSocket (port 8091, path /McpPlaytest)
                                    |
                            Unity Editor (C# PlaytestServer)
```

- Runs on port **8091** (mcp-unity uses 8090)
- WebSocket path: `/McpPlaytest`
- Namespace: `McpPlaytest`

### Tools

| Tool | Description |
|------|-------------|
| `play_mode_control` | Enter, exit, pause, unpause, or step-frame Play Mode |
| `capture_screenshot` | Capture Game View as base64 PNG (works without window focus) |
| `simulate_input` | Inject movement (x,y) or button presses (melee/ranged/throw/special) |
| `query_game_state` | Read player positions, scores, states, round info, game mode |
| `get_playtest_logs` | Retrieve recent console logs with error/warning/log filtering |
| `record_video` | Start/stop frame-sequence recording, returns array of PNG frames |
| `spawn_player` | Spawn a new player with a virtual gamepad device in Lobby state |

### Screenshot and Video Capture

Capture works even when Unity is a background window that is not focused. The primary capture method renders all active cameras directly to a `RenderTexture` (bypassing the Game View window entirely). A fallback reads the Game View's internal render texture via reflection if the camera approach fails.

- **Screenshots**: returned as a single base64 PNG at the requested resolution (default 1280x720)
- **Video**: captures frame sequences at a configurable FPS (default 5) and max duration (default 5s). Frames are returned as individual PNG image blocks, up to 30 frames max.

### File Structure

```
Assets/Editor/McpPlaytest/
├── PlaytestServer.cs              # WebSocket server, tool registry, Play Mode lifecycle
├── PlaytestSocketHandler.cs       # Message routing, JSON-RPC handling
├── PlaytestToolBase.cs            # Base class for all tools
├── Tools/
│   ├── PlayModeControlTool.cs
│   ├── CaptureScreenshotTool.cs
│   ├── SimulateInputTool.cs
│   ├── QueryGameStateTool.cs
│   ├── GetConsoleLogsTool.cs
│   ├── RecordVideoTool.cs
│   └── SpawnPlayerTool.cs
├── Utils/
│   ├── ScreenCaptureHelper.cs     # RenderTexture-based capture (background-safe)
│   └── InputSimulator.cs          # Virtual gamepad input injection
└── Server~/                       # Node.js TypeScript MCP bridge (ignored by Unity)
    ├── package.json
    ├── tsconfig.json
    └── src/
        ├── index.ts               # MCP server entry point, tool registration
        ├── unity/playtestUnity.ts  # WebSocket client to Unity Editor
        ├── utils/
        │   ├── logger.ts
        │   └── errors.ts
        └── tools/                  # One handler per tool, mirrors C# tools
```

### Setup

1. The `Assets/Editor/McpPlaytest/` folder is already in the project.
2. Build the Node.js server:
   ```bash
   cd Assets/Editor/McpPlaytest/Server~
   npm install
   npm run build
   ```
3. Ensure `.mcp.json` in the project root has the `mcp-playtest` entry:
   ```json
   {
     "mcpServers": {
       "mcp-playtest": {
         "command": "node",
         "args": ["<project-path>/Assets/Editor/McpPlaytest/Server~/build/index.js"]
       }
     }
   }
   ```
4. Open Unity — the PlaytestServer starts automatically on port 8091.
5. Restart Claude Code to pick up the new MCP server.

### Configuration

`ProjectSettings/McpPlaytestSettings.json`:
```json
{
  "Port": 8091,
  "Host": "localhost",
  "RequestTimeoutSeconds": 30
}
```

---

## Notes

- The `projects/` folder contains per-project memory tied to directory paths. It will regenerate naturally as you work — no need to restore it on a new machine.
- The `tasks/` folder holds current session tasks and can be safely ignored.
- Hooks use `uv run --script` with inline dependencies — `uv` auto-installs Python packages on first run. No manual `pip install` needed beyond `uv` itself.
- API keys (`ANTHROPIC_API_KEY`, `OPENAI_API_KEY`, `ELEVENLABS_API_KEY`) must be set separately in your environment or a `.env` file if using the LLM/TTS hook utilities.

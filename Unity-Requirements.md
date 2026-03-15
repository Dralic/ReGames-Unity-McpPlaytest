# Claude Code Environment Setup Requirements

This document describes everything needed to recreate the full Claude Code + Unity development environment from scratch. If you are Claude Code reading this, follow each section in order and install/configure everything listed.

---

## 1. Prerequisites (Manual Installs)

These must be installed by the user before Claude Code can set up the rest:

| Tool | Version | Install |
|------|---------|---------|
| **Node.js** | 18+ | https://nodejs.org |
| **Python** | 3.11+ | https://www.python.org |
| **uv** | latest | `pip install uv` or https://docs.astral.sh/uv/ |
| **Git** | latest | https://git-scm.com |
| **Unity Hub + Unity** | 6000.3.10f1 | https://unity.com/download |
| **Claude Code CLI** | latest | `npm install -g @anthropic-ai/claude-code` |

---

## 2. Global npm Packages

```bash
npm install -g defuddle
npm install -g typescript
```

---

## 3. Claude Code Global Settings

File: `~/.claude/settings.json`

```json
{
  "permissions": {
    "allow": [
      "Bash(mkdir:*)",
      "Bash(uv:*)",
      "Bash(find:*)",
      "Bash(mv:*)",
      "Bash(grep:*)",
      "Bash(npm:*)",
      "Bash(ls:*)",
      "Bash(cp:*)",
      "Bash(chmod:*)",
      "Bash(touch:*)"
    ],
    "deny": []
  },
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/pre_tool_use.py"
          }
        ]
      }
    ],
    "PostToolUse": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/post_tool_use.py"
          }
        ]
      }
    ],
    "PostToolUseFailure": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/post_tool_use_failure.py"
          }
        ]
      }
    ],
    "Notification": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/notification.py --notify"
          }
        ]
      }
    ],
    "Stop": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/stop.py --chat"
          }
        ]
      }
    ],
    "SubagentStart": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/subagent_start.py"
          }
        ]
      }
    ],
    "SubagentStop": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/subagent_stop.py --notify"
          }
        ]
      }
    ],
    "UserPromptSubmit": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/user_prompt_submit.py --log-only --store-last-prompt"
          }
        ]
      }
    ],
    "PreCompact": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/pre_compact.py"
          }
        ]
      }
    ],
    "SessionStart": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/session_start.py"
          }
        ]
      }
    ],
    "SessionEnd": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/session_end.py"
          }
        ]
      }
    ],
    "PermissionRequest": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/permission_request.py --log-only"
          }
        ]
      }
    ],
    "Setup": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "uv run ~/.claude/hooks/setup.py"
          }
        ]
      }
    ]
  },
  "statusLine": {
    "type": "command",
    "command": "uv run ~/.claude/status_lines/status_line_v6.py",
    "padding": 0
  },
  "autoUpdatesChannel": "latest",
  "skipDangerousModePermissionPrompt": true,
  "effortLevel": "high"
}
```

---

## 4. Global CLAUDE.md

File: `~/.claude/CLAUDE.md`

```markdown
# Global Instructions

## Obsidian Knowledge Base
A cross-project Obsidian vault exists at `<OBSIDIAN_VAULT_PATH>`. Check its `CLAUDE.md` for vault rules and conventions. Use it for project notes, session logs, and research. Update relevant notes after significant changes.
```

Replace `<OBSIDIAN_VAULT_PATH>` with the actual path to the Obsidian vault on the new machine.

---

## 5. Hooks (`~/.claude/hooks/`)

These are custom Python scripts that run on Claude Code lifecycle events. All use `uv run --script` with inline dependencies (`python-dotenv` is the common one).

### Files to copy/recreate:

```
~/.claude/hooks/
  pre_tool_use.py              # Blocks dangerous rm commands, validates tool usage
  post_tool_use.py             # Post-tool logging/processing
  post_tool_use_failure.py     # Handles failed tool calls
  notification.py              # Desktop notifications on events
  stop.py                      # End-of-turn processing, chat logging
  subagent_start.py            # Tracks subagent launches
  subagent_stop.py             # Notifications when subagents complete
  user_prompt_submit.py        # Logs prompts, stores last prompt
  pre_compact.py               # Pre-compaction processing
  session_start.py             # Session initialization
  session_end.py               # Session cleanup
  permission_request.py        # Logs permission requests
  setup.py                     # Initial setup hook
  utils/
    llm/
      anth.py                  # Anthropic API helper (deps: anthropic, python-dotenv)
      oai.py                   # OpenAI API helper (deps: openai, python-dotenv)
      ollama.py                # Ollama local LLM helper
      task_summarizer.py       # Summarizes tasks using LLM
    tts/
      elevenlabs_tts.py        # ElevenLabs TTS (deps: elevenlabs, python-dotenv)
      openai_tts.py            # OpenAI TTS (deps: openai, python-dotenv)
      pyttsx3_tts.py           # Local TTS fallback (deps: pyttsx3)
      tts_queue.py             # TTS queue manager
  validators/
    ruff_validator.py          # Python linting via ruff
    ty_validator.py            # Type checking
    validate_file_contains.py  # Content validation
    validate_new_file.py       # New file validation
```

Source location on the original machine: `C:\Users\maxim\.claude\hooks\`
These scripts are custom-built. They are NOT available from any public repo. Copy the entire `hooks/` folder from the original machine or a backup.

---

## 6. Status Lines (`~/.claude/status_lines/`)

Active version: `status_line_v6.py` — displays context window usage with progress bar and session ID.

```
~/.claude/status_lines/
  status_line_v6.py            # Active (set in settings.json)
  status_line.py through status_line_v9.py  # Historical versions
```

Source location on the original machine: `C:\Users\maxim\.claude\status_lines\`
Custom scripts, must be copied from backup.

---

## 7. Custom Agents (`~/.claude/agents/`)

### Team Agents
```
~/.claude/agents/team/
  builder.md       # Generic engineering agent, model: opus, executes one task at a time
  validator.md     # Read-only validation agent, model: opus, disallowed: Write, Edit, NotebookEdit
```

### Utility Agents
```
~/.claude/agents/
  hello-world-agent.md              # Greeting agent, tools: WebSearch
  meta-agent.md                     # Creates new agent configs, tools: Write, WebFetch, Firecrawl
  work-completion-summary.md        # TTS summaries, tools: Bash, ElevenLabs MCP
  llm-ai-agents-and-eng-research.md # AI news researcher, tools: Bash, Firecrawl, WebFetch
```

### Crypto Agents
```
~/.claude/agents/crypto/
  crypto-coin-analyzer-{haiku,opus,sonnet}.md
  crypto-investment-plays-{haiku,opus,sonnet}.md
  crypto-market-agent-{haiku,opus,sonnet}.md
  crypto-movers-haiku.md
  macro-crypto-correlation-scanner-{haiku,opus,sonnet}.md
```

Source location on the original machine: `C:\Users\maxim\.claude\agents\`
Agent files are markdown with YAML frontmatter (name, description, model, tools, color). Must be copied from backup.

---

## 8. Custom Skills (`~/.claude/skills/`)

Each skill is a folder with a `skill.md` (or `SKILL.md`) file:

```
~/.claude/skills/
  defuddle/          # Web page to clean markdown (requires: npm -g defuddle)
  json-canvas/       # Obsidian .canvas file editor
  obsidian-bases/    # Obsidian .base file editor
  obsidian-cli/      # Obsidian vault CLI interaction (requires: Obsidian running)
  obsidian-markdown/ # Obsidian-flavored markdown helper
```

Source location on the original machine: `C:\Users\maxim\.claude\skills\`
Must be copied from backup.

---

## 9. Unity Project Setup

### Unity Packages to Install (via Package Manager > Add by Git URL)

```
https://github.com/CoderGamester/mcp-unity.git
https://github.com/boxqkrtm/com.unity.ide.cursor.git
```

Standard packages (via Unity Registry):
- Input System 1.18.0
- Universal RP 17.3.0
- AI Navigation 2.0.10
- Timeline 1.8.10

### MCP Playtest Server (Custom)

```
https://github.com/Dralic/ReGames-Unity-McpPlaytest
```

1. Copy `Assets/Editor/McpPlaytest/` folder + `McpPlaytest.meta` into the project
2. Build the Node.js server:
```bash
cd Assets/Editor/McpPlaytest/Server~
npm install
npm run build
```

---

## 10. Project-Level Claude Code Config

### `.mcp.json` (project root)

```json
{
  "mcpServers": {
    "mcp-unity": {
      "command": "node",
      "args": [
        "<project-path>/Library/PackageCache/com.gamelovers.mcp-unity@<hash>/Server~/build/index.js"
      ]
    },
    "mcp-playtest": {
      "command": "node",
      "args": [
        "<project-path>/Assets/Editor/McpPlaytest/Server~/build/index.js"
      ]
    }
  }
}
```

NOTE: The `mcp-unity` path includes a git hash that changes on update. After installing the package, find the actual path in `Library/PackageCache/com.gamelovers.mcp-unity@*/Server~/build/index.js`.

### `.claude/settings.json` (project level)

```json
{
  "permissions": {
    "allow": [
      "Bash(dotnet build:*)",
      "mcp__unity-docs__*",
      "mcp__context7__*",
      "mcp__mcp-unity__*"
    ],
    "deny": []
  }
}
```

---

## 11. Obsidian Knowledge Base

Create an Obsidian vault for Claude Code knowledge management. The vault can be located anywhere — just update the path in `~/.claude/CLAUDE.md` (Section 4) to point to the chosen location.

The vault should contain:
- A `CLAUDE.md` at its root with vault-specific rules and conventions
- Cross-project notes, session logs, and research documentation

This vault is referenced by the global `~/.claude/CLAUDE.md` so Claude Code can read/write notes across projects.

---

## 12. Environment Variables

The hook utilities expect these in a `.env` file or system environment (only needed if using the corresponding features):

| Variable | Used By |
|----------|---------|
| `ANTHROPIC_API_KEY` | `hooks/utils/llm/anth.py` |
| `OPENAI_API_KEY` | `hooks/utils/llm/oai.py`, `hooks/utils/tts/openai_tts.py` |
| `ELEVENLABS_API_KEY` | `hooks/utils/tts/elevenlabs_tts.py` |

---

## Summary Checklist

- [ ] Node.js, Python 3.11+, uv, Git installed
- [ ] Unity 6000.3.10f1 installed via Unity Hub
- [ ] Claude Code CLI installed globally (`npm install -g @anthropic-ai/claude-code`)
- [ ] `defuddle` and `typescript` installed globally via npm
- [ ] `~/.claude/settings.json` configured (Section 3)
- [ ] `~/.claude/CLAUDE.md` created (Section 4)
- [ ] `~/.claude/hooks/` copied from backup (Section 5)
- [ ] `~/.claude/status_lines/` copied from backup (Section 6)
- [ ] `~/.claude/agents/` copied from backup (Section 7)
- [ ] `~/.claude/skills/` copied from backup (Section 8)
- [ ] Unity packages installed: mcp-unity, cursor IDE (Section 9)
- [ ] McpPlaytest folder copied + Server~ built (Section 9)
- [ ] `.mcp.json` configured with correct paths (Section 10)
- [ ] `.claude/settings.json` project permissions set (Section 10)
- [ ] Obsidian vault created and path set in `~/.claude/CLAUDE.md` (Section 11)
- [ ] API keys set in environment if using LLM/TTS hooks (Section 12)

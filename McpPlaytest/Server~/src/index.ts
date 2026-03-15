import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { PlaytestUnity } from './unity/playtestUnity.js';
import { Logger, LogLevel } from './utils/logger.js';
import { registerPlayModeControlTool } from './tools/playModeControlTool.js';
import { registerCaptureScreenshotTool } from './tools/captureScreenshotTool.js';
import { registerSimulateInputTool } from './tools/simulateInputTool.js';
import { registerQueryGameStateTool } from './tools/queryGameStateTool.js';
import { registerGetConsoleLogsTool } from './tools/getConsoleLogsTool.js';
import { registerRecordVideoTool } from './tools/recordVideoTool.js';

// Initialize loggers
const serverLogger = new Logger('Server', LogLevel.INFO);
const unityLogger = new Logger('Unity', LogLevel.INFO);
const toolLogger = new Logger('Tools', LogLevel.INFO);

// Initialize the MCP server
const server = new McpServer(
  {
    name: "MCP Playtest Server",
    version: "1.0.0"
  },
  {
    capabilities: {
      tools: {}
    }
  }
);

// Initialize the Unity bridge
const playtestUnity = new PlaytestUnity(unityLogger);

// Register all tools
registerPlayModeControlTool(server, playtestUnity, toolLogger);
registerCaptureScreenshotTool(server, playtestUnity, toolLogger);
registerSimulateInputTool(server, playtestUnity, toolLogger);
registerQueryGameStateTool(server, playtestUnity, toolLogger);
registerGetConsoleLogsTool(server, playtestUnity, toolLogger);
registerRecordVideoTool(server, playtestUnity, toolLogger);

// Server startup
async function startServer() {
  try {
    const stdioTransport = new StdioServerTransport();
    await server.connect(stdioTransport);
    serverLogger.info('MCP Playtest Server started');

    const clientName = server.server.getClientVersion()?.name || 'Unknown MCP Client';
    serverLogger.info(`Connected MCP client: ${clientName}`);

    await playtestUnity.start(clientName);
  } catch (error) {
    serverLogger.error('Failed to start server', error);
    process.exit(1);
  }
}

// Graceful shutdown
let isShuttingDown = false;
async function shutdown() {
  if (isShuttingDown) return;
  isShuttingDown = true;

  try {
    serverLogger.info('Shutting down...');
    await playtestUnity.stop();
    await server.close();
  } catch (error) {
    // Ignore errors during shutdown
  }
  process.exit(0);
}

// Start
startServer();

// Handle shutdown signals
process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);
process.on('SIGHUP', shutdown);
process.stdin.on('close', shutdown);
process.stdin.on('end', shutdown);
process.stdin.on('error', shutdown);

process.on('uncaughtException', (error: NodeJS.ErrnoException) => {
  if (error.code === 'EPIPE' || error.code === 'EOF' || error.code === 'ERR_USE_AFTER_CLOSE') {
    shutdown();
    return;
  }
  serverLogger.error('Uncaught exception', error);
  process.exit(1);
});

process.on('unhandledRejection', (reason) => {
  serverLogger.error('Unhandled rejection', reason);
  process.exit(1);
});

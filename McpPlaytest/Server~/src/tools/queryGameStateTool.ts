import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { PlaytestUnity } from '../unity/playtestUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpPlaytestError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'query_game_state';
const toolDescription = 'Query runtime game state: player positions, scores, states, round info, and game mode settings';
const paramsSchema = z.object({
  query: z.enum(['full', 'players', 'round', 'game_mode']).optional().default('full').describe('What to query: full (everything), players (positions/states/scores), round (timing/number), game_mode (settings)')
});

export function registerQueryGameStateTool(server: McpServer, playtestUnity: PlaytestUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);

  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${toolName}`, params);
        const result = await toolHandler(playtestUnity, params);
        logger.info(`Tool execution successful: ${toolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${toolName}`, error);
        throw error;
      }
    }
  );
}

async function toolHandler(playtestUnity: PlaytestUnity, params: any): Promise<CallToolResult> {
  const response = await playtestUnity.sendRequest({
    method: toolName,
    params
  });

  if (!response.success) {
    throw new McpPlaytestError(ErrorType.TOOL_EXECUTION, response.message || 'Failed to query game state');
  }

  return {
    content: [{
      type: 'text',
      text: JSON.stringify(response, null, 2)
    }]
  };
}

import * as z from 'zod';
import { McpPlaytestError, ErrorType } from '../utils/errors.js';
const toolName = 'spawn_player';
const toolDescription = 'Spawn a new player with a virtual gamepad device. Works in Play Mode when GameManager is in Lobby state. The spawned player can then be controlled via simulate_input.';
const paramsSchema = z.object({});
export function registerSpawnPlayerTool(server, playtestUnity, logger) {
    logger.info(`Registering tool: ${toolName}`);
    server.tool(toolName, toolDescription, paramsSchema.shape, async (params) => {
        try {
            logger.info(`Executing tool: ${toolName}`, params);
            const result = await toolHandler(playtestUnity, params);
            logger.info(`Tool execution successful: ${toolName}`);
            return result;
        }
        catch (error) {
            logger.error(`Tool execution failed: ${toolName}`, error);
            throw error;
        }
    });
}
async function toolHandler(playtestUnity, params) {
    const response = await playtestUnity.sendRequest({
        method: toolName,
        params
    });
    if (!response.success) {
        throw new McpPlaytestError(ErrorType.TOOL_EXECUTION, response.message || 'Failed to spawn player');
    }
    return {
        content: [{
                type: 'text',
                text: JSON.stringify(response, null, 2)
            }]
    };
}

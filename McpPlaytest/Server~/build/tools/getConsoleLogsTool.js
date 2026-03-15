import * as z from 'zod';
import { McpPlaytestError, ErrorType } from '../utils/errors.js';
const toolName = 'get_playtest_logs';
const toolDescription = 'Get recent Unity console log entries including errors, warnings, and info messages during Play Mode';
const paramsSchema = z.object({
    count: z.number().optional().default(50).describe('Number of log entries to return (default: 50, max: 500)'),
    filter: z.enum(['all', 'error', 'warning', 'log']).optional().default('all').describe('Filter by log type (default: all)')
});
export function registerGetConsoleLogsTool(server, playtestUnity, logger) {
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
        throw new McpPlaytestError(ErrorType.TOOL_EXECUTION, response.message || 'Failed to get console logs');
    }
    return {
        content: [{
                type: 'text',
                text: JSON.stringify(response, null, 2)
            }]
    };
}

import * as z from 'zod';
import { McpPlaytestError, ErrorType } from '../utils/errors.js';
const toolName = 'simulate_input';
const toolDescription = 'Simulate player input: movement (x,y values from -1 to 1), or button presses (melee_attack, ranged_attack, throw_attack, special_ability)';
const paramsSchema = z.object({
    action: z.enum(['move', 'melee_attack', 'ranged_attack', 'throw_attack', 'special_ability', 'stop']).describe('The input action to simulate'),
    playerIndex: z.number().optional().default(0).describe('Player index to target (default: 0)'),
    value: z.object({
        x: z.number().optional().describe('X-axis value for movement (-1 to 1)'),
        y: z.number().optional().describe('Y-axis value for movement (-1 to 1)'),
        pressed: z.boolean().optional().describe('Whether the button is pressed (for button actions)')
    }).optional().describe('Input value - for move: {x, y}, for buttons: {pressed}'),
    duration: z.number().optional().default(0.1).describe('Duration to hold the input in seconds (default: 0.1)')
});
export function registerSimulateInputTool(server, playtestUnity, logger) {
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
        throw new McpPlaytestError(ErrorType.TOOL_EXECUTION, response.message || 'Failed to simulate input');
    }
    return {
        content: [{
                type: 'text',
                text: JSON.stringify(response, null, 2)
            }]
    };
}

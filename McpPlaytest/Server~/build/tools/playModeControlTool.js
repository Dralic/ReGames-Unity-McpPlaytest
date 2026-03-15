import * as z from 'zod';
import { McpPlaytestError, ErrorType } from '../utils/errors.js';
const toolName = 'play_mode_control';
const toolDescription = 'Control Unity Play Mode: enter, exit, pause, unpause, or step one frame';
const paramsSchema = z.object({
    action: z.enum(['enter', 'exit', 'pause', 'unpause', 'step']).describe('The play mode action to perform')
});
export function registerPlayModeControlTool(server, playtestUnity, logger) {
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
    const action = params.action;
    const causesReload = action === 'enter' || action === 'exit';
    try {
        const response = await playtestUnity.sendRequest({
            method: toolName,
            params
        });
        if (!response.success) {
            throw new McpPlaytestError(ErrorType.TOOL_EXECUTION, response.message || 'Failed to control play mode');
        }
        return {
            content: [{
                    type: 'text',
                    text: JSON.stringify(response, null, 2)
                }]
        };
    }
    catch (error) {
        // Enter/exit triggers a Unity domain reload which kills the WebSocket.
        // A disconnection or timeout during these actions means the action succeeded.
        if (causesReload && error instanceof McpPlaytestError &&
            (error.type === ErrorType.TIMEOUT || error.type === ErrorType.CONNECTION)) {
            const entering = action === 'enter';
            return {
                content: [{
                        type: 'text',
                        text: JSON.stringify({
                            success: true,
                            message: entering ? 'Entering Play Mode (domain reload in progress)' : 'Exiting Play Mode (domain reload in progress)',
                            isPlaying: entering,
                            isPaused: false,
                            note: 'Unity is reloading. Wait a moment before sending the next command.'
                        }, null, 2)
                    }]
            };
        }
        throw error;
    }
}

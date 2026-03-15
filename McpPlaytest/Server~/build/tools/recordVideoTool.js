import * as z from 'zod';
import { McpPlaytestError, ErrorType } from '../utils/errors.js';
const toolName = 'record_video';
const toolDescription = 'Record gameplay as a sequence of screenshot frames. Use start to begin recording, stop to end and retrieve frames, status to check progress';
const paramsSchema = z.object({
    action: z.enum(['start', 'stop', 'status']).describe('Recording action: start, stop, or status'),
    fps: z.number().optional().default(5).describe('Frames per second for recording (default: 5)'),
    maxDuration: z.number().optional().default(5).describe('Maximum recording duration in seconds (default: 5)'),
    width: z.number().optional().default(640).describe('Frame width in pixels (default: 640)')
});
export function registerRecordVideoTool(server, playtestUnity, logger) {
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
        throw new McpPlaytestError(ErrorType.TOOL_EXECUTION, response.message || 'Failed to record video');
    }
    // If stop action returned frames, send them as image content blocks
    if (params.action === 'stop' && response.frames && Array.isArray(response.frames)) {
        const content = [];
        // Add summary text
        content.push({
            type: 'text',
            text: `Recorded ${response.frameCount} frames at ${response.width}x${response.height}`
        });
        // Add each frame as an image
        for (const frame of response.frames) {
            content.push({
                type: 'image',
                data: frame,
                mimeType: 'image/png'
            });
        }
        return { content };
    }
    // For start/status actions, return text
    return {
        content: [{
                type: 'text',
                text: JSON.stringify(response, null, 2)
            }]
    };
}

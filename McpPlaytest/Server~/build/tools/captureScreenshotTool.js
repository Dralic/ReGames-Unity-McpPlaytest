import * as z from 'zod';
import { McpPlaytestError, ErrorType } from '../utils/errors.js';
const toolName = 'capture_screenshot';
const toolDescription = 'Capture a screenshot of the Game View and return it as a base64-encoded PNG image for visual analysis';
const paramsSchema = z.object({
    width: z.number().optional().default(1280).describe('Screenshot width in pixels (default: 1280)'),
    height: z.number().optional().default(720).describe('Screenshot height in pixels (default: 720)')
});
export function registerCaptureScreenshotTool(server, playtestUnity, logger) {
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
        throw new McpPlaytestError(ErrorType.TOOL_EXECUTION, response.message || 'Failed to capture screenshot');
    }
    // Return the image as an MCP image content block for Claude's vision analysis
    return {
        content: [{
                type: 'image',
                data: response.image,
                mimeType: 'image/png'
            }]
    };
}

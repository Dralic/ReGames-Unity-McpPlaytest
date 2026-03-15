export declare enum ErrorType {
    CONNECTION = "connection_error",
    TOOL_EXECUTION = "tool_execution_error",
    VALIDATION = "validation_error",
    INTERNAL = "internal_error",
    TIMEOUT = "timeout_error"
}
export declare class McpPlaytestError extends Error {
    type: ErrorType;
    details?: any;
    constructor(type: ErrorType, message: string, details?: any);
    toJSON(): {
        type: ErrorType;
        message: string;
        details: any;
    };
}

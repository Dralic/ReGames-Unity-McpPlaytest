export var ErrorType;
(function (ErrorType) {
    ErrorType["CONNECTION"] = "connection_error";
    ErrorType["TOOL_EXECUTION"] = "tool_execution_error";
    ErrorType["VALIDATION"] = "validation_error";
    ErrorType["INTERNAL"] = "internal_error";
    ErrorType["TIMEOUT"] = "timeout_error";
})(ErrorType || (ErrorType = {}));
export class McpPlaytestError extends Error {
    type;
    details;
    constructor(type, message, details) {
        super(message);
        this.type = type;
        this.details = details;
        this.name = 'McpPlaytestError';
    }
    toJSON() {
        return {
            type: this.type,
            message: this.message,
            details: this.details
        };
    }
}

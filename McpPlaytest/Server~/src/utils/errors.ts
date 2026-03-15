export enum ErrorType {
  CONNECTION = 'connection_error',
  TOOL_EXECUTION = 'tool_execution_error',
  VALIDATION = 'validation_error',
  INTERNAL = 'internal_error',
  TIMEOUT = 'timeout_error'
}

export class McpPlaytestError extends Error {
  type: ErrorType;
  details?: any;

  constructor(type: ErrorType, message: string, details?: any) {
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

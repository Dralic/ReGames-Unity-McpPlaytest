import { appendFileSync } from 'fs';
export var LogLevel;
(function (LogLevel) {
    LogLevel[LogLevel["DEBUG"] = 0] = "DEBUG";
    LogLevel[LogLevel["INFO"] = 1] = "INFO";
    LogLevel[LogLevel["WARN"] = 2] = "WARN";
    LogLevel[LogLevel["ERROR"] = 3] = "ERROR";
})(LogLevel || (LogLevel = {}));
const isLoggingEnabled = process.env.LOGGING === 'true';
const isLoggingFileEnabled = process.env.LOGGING_FILE === 'true';
export class Logger {
    level;
    prefix;
    constructor(prefix, level = LogLevel.INFO) {
        this.prefix = prefix;
        this.level = level;
    }
    debug(message, data) {
        this.log(LogLevel.DEBUG, message, data);
    }
    info(message, data) {
        this.log(LogLevel.INFO, message, data);
    }
    warn(message, data) {
        this.log(LogLevel.WARN, message, data);
    }
    error(message, error) {
        this.log(LogLevel.ERROR, message, error);
    }
    log(level, message, data) {
        if (level < this.level)
            return;
        const timestamp = new Date().toISOString();
        const levelStr = LogLevel[level];
        const logMessage = `[${timestamp}] [${levelStr}] [${this.prefix}] ${message}`;
        if (isLoggingFileEnabled) {
            try {
                appendFileSync('mcp-playtest.log', logMessage + '\n');
                if (data) {
                    appendFileSync('mcp-playtest.log', JSON.stringify(data, null, 2) + '\n');
                }
            }
            catch (error) {
                // Ignore file write errors
            }
        }
        if (isLoggingEnabled) {
            if (data) {
                console.error(logMessage, data);
            }
            else {
                console.error(logMessage);
            }
        }
    }
}

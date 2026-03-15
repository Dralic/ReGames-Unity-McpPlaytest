import { Logger } from '../utils/logger.js';
interface UnityRequest {
    id?: string;
    method: string;
    params: any;
}
export declare class PlaytestUnity {
    private logger;
    private port;
    private host;
    private wsPath;
    private requestTimeout;
    private ws;
    private pendingRequests;
    private reconnectTimer;
    private isConnected;
    private clientName;
    private maxReconnectAttempts;
    private reconnectAttempt;
    private baseReconnectDelay;
    private maxReconnectDelay;
    constructor(logger: Logger);
    start(clientName?: string): Promise<void>;
    private connect;
    private handleMessage;
    private scheduleReconnect;
    private rejectAllPending;
    sendRequest(request: UnityRequest): Promise<any>;
    stop(): Promise<void>;
}
export {};

import { v4 as uuidv4 } from 'uuid';
import WebSocket from 'ws';
import { Logger } from '../utils/logger.js';
import { McpPlaytestError, ErrorType } from '../utils/errors.js';

interface PendingRequest {
  resolve: (value: any) => void;
  reject: (reason: any) => void;
  timeout: NodeJS.Timeout;
}

interface UnityRequest {
  id?: string;
  method: string;
  params: any;
}

interface UnityResponse {
  id: string;
  result?: any;
  error?: {
    message: string;
    type: string;
  };
}

export class PlaytestUnity {
  private logger: Logger;
  private port: number = 8091;
  private host: string = 'localhost';
  private wsPath: string = '/McpPlaytest';
  private requestTimeout: number = 30000; // 30s default — longer for Play Mode operations
  private ws: WebSocket | null = null;
  private pendingRequests: Map<string, PendingRequest> = new Map();
  private reconnectTimer: NodeJS.Timeout | null = null;
  private isConnected: boolean = false;
  private clientName: string = '';
  private maxReconnectAttempts: number = 50;
  private reconnectAttempt: number = 0;
  private baseReconnectDelay: number = 1000;
  private maxReconnectDelay: number = 10000;

  constructor(logger: Logger) {
    this.logger = logger;
  }

  public async start(clientName?: string): Promise<void> {
    this.clientName = clientName || '';

    // Try reading port from config
    try {
      const fs = await import('fs');
      const path = await import('path');
      const settingsPath = path.resolve(process.cwd(), './ProjectSettings/McpPlaytestSettings.json');
      const content = fs.readFileSync(settingsPath, 'utf-8');
      const config = JSON.parse(content);
      if (config.Port) this.port = config.Port;
      if (config.Host) this.host = config.Host;
      if (config.RequestTimeoutSeconds) this.requestTimeout = config.RequestTimeoutSeconds * 1000;
    } catch {
      this.logger.debug('No McpPlaytestSettings.json found, using defaults');
    }

    this.logger.info(`Connecting to Unity PlaytestServer at ws://${this.host}:${this.port}${this.wsPath}`);

    try {
      await this.connect();
    } catch (error) {
      this.logger.warn(`Initial connection failed: ${error instanceof Error ? error.message : String(error)}`);
      this.logger.warn('Will retry on next request');
    }
  }

  private connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      const url = `ws://${this.host}:${this.port}${this.wsPath}`;
      const headers: Record<string, string> = {};
      if (this.clientName) {
        headers['X-Client-Name'] = this.clientName;
      }

      this.ws = new WebSocket(url, { headers });

      const connectTimeout = setTimeout(() => {
        if (this.ws && this.ws.readyState === WebSocket.CONNECTING) {
          this.ws.terminate();
          reject(new McpPlaytestError(ErrorType.CONNECTION, 'Connection timeout'));
        }
      }, 5000);

      this.ws.on('open', () => {
        clearTimeout(connectTimeout);
        this.isConnected = true;
        this.reconnectAttempt = 0;
        this.logger.info('Connected to Unity PlaytestServer');
        resolve();
      });

      this.ws.on('message', (data: WebSocket.Data) => {
        this.handleMessage(data.toString());
      });

      this.ws.on('close', (code: number, reason: Buffer) => {
        clearTimeout(connectTimeout);
        this.isConnected = false;
        this.logger.info(`WebSocket closed: ${code} ${reason.toString()}`);
        this.scheduleReconnect();
      });

      this.ws.on('error', (error: Error) => {
        clearTimeout(connectTimeout);
        this.isConnected = false;
        this.logger.error(`WebSocket error: ${error.message}`);
        if (!this.isConnected) {
          reject(new McpPlaytestError(ErrorType.CONNECTION, error.message));
        }
      });
    });
  }

  private handleMessage(data: string): void {
    try {
      const response = JSON.parse(data) as UnityResponse;

      if (response.id && this.pendingRequests.has(response.id)) {
        const request = this.pendingRequests.get(response.id)!;
        clearTimeout(request.timeout);
        this.pendingRequests.delete(response.id);

        if (response.error) {
          request.reject(new McpPlaytestError(
            ErrorType.TOOL_EXECUTION,
            response.error.message || 'Unknown error'
          ));
        } else {
          request.resolve(response.result);
        }
      }
    } catch (e) {
      this.logger.error(`Error parsing message: ${e instanceof Error ? e.message : String(e)}`);
    }
  }

  private scheduleReconnect(): void {
    if (this.reconnectTimer) return;
    if (this.reconnectAttempt >= this.maxReconnectAttempts) {
      this.logger.error('Max reconnection attempts reached');
      this.rejectAllPending(new McpPlaytestError(ErrorType.CONNECTION, 'Max reconnection attempts reached'));
      return;
    }

    const delay = Math.min(
      this.baseReconnectDelay * Math.pow(1.5, this.reconnectAttempt),
      this.maxReconnectDelay
    );
    // Add jitter
    const jitter = delay * (0.8 + Math.random() * 0.4);

    this.reconnectAttempt++;
    this.logger.info(`Reconnecting in ${Math.round(jitter)}ms (attempt ${this.reconnectAttempt}/${this.maxReconnectAttempts})`);

    this.reconnectTimer = setTimeout(async () => {
      this.reconnectTimer = null;
      try {
        await this.connect();
      } catch {
        // Will schedule another reconnect via the close handler
      }
    }, jitter);
  }

  private rejectAllPending(error: McpPlaytestError): void {
    for (const [id, request] of this.pendingRequests.entries()) {
      clearTimeout(request.timeout);
      request.reject(error);
      this.pendingRequests.delete(id);
    }
  }

  public async sendRequest(request: UnityRequest): Promise<any> {
    const requestId = request.id || uuidv4();
    const message: UnityRequest = { ...request, id: requestId };

    // If not connected, try to connect
    if (!this.isConnected || !this.ws || this.ws.readyState !== WebSocket.OPEN) {
      this.logger.info('Not connected, attempting to connect...');
      try {
        await this.connect();
      } catch (error) {
        throw new McpPlaytestError(
          ErrorType.CONNECTION,
          `Not connected to Unity: ${error instanceof Error ? error.message : String(error)}`
        );
      }
    }

    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        if (this.pendingRequests.has(requestId)) {
          this.pendingRequests.delete(requestId);
          reject(new McpPlaytestError(ErrorType.TIMEOUT, `Request timed out after ${this.requestTimeout}ms`));
        }
      }, this.requestTimeout);

      this.pendingRequests.set(requestId, { resolve, reject, timeout });

      try {
        this.ws!.send(JSON.stringify(message));
        this.logger.debug(`Request sent: ${requestId} (${request.method})`);
      } catch (err) {
        clearTimeout(timeout);
        this.pendingRequests.delete(requestId);
        reject(new McpPlaytestError(ErrorType.CONNECTION, `Send failed: ${err instanceof Error ? err.message : String(err)}`));
      }
    });
  }

  public async stop(): Promise<void> {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
    this.rejectAllPending(new McpPlaytestError(ErrorType.CONNECTION, 'Server stopping'));
    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
    this.isConnected = false;
    this.logger.info('PlaytestUnity client stopped');
  }
}

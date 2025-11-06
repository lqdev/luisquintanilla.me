/**
 * Common type definitions for Azure Functions API
 */

export interface Context {
    log: {
        (message: string): void;
        error(message: string): void;
        warn(message: string): void;
        info(message: string): void;
        verbose(message: string): void;
    };
    res?: {
        status?: number;
        headers?: { [key: string]: string };
        body?: any;
    };
    bindingData: { [key: string]: any };
}

export interface HttpRequest {
    method?: string;
    url?: string;
    headers?: { [key: string]: string };
    query?: { [key: string]: string };
    params?: { [key: string]: string };
    body?: any;
}

export type AzureFunction = (context: Context, req?: HttpRequest) => Promise<void>;

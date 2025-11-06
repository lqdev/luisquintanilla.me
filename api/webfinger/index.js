"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
const fs_1 = require("fs");
const path = __importStar(require("path"));
const httpTrigger = async function (context, req) {
    try {
        // Get resource parameter
        const resource = req?.query?.['resource'];
        if (!resource || resource !== 'acct:lqdev@www.lqdev.me') {
            context.res = {
                status: 404,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: { error: 'Resource not found' }
            };
            return;
        }
        // Read webfinger data
        const webfingerPath = path.join(__dirname, '../data/webfinger.json');
        const webfingerData = await fs_1.promises.readFile(webfingerPath, 'utf8');
        const parsedWebfinger = JSON.parse(webfingerData);
        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/jrd+json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
                'Access-Control-Allow-Headers': 'Accept, Content-Type',
                'Cache-Control': 'public, max-age=3600'
            },
            body: parsedWebfinger
        };
    }
    catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        context.log.error(`WebFinger error: ${errorMessage}`);
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Internal server error' }
        };
    }
};
exports.default = httpTrigger;
//# sourceMappingURL=index.js.map
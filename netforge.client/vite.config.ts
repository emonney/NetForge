import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import generouted from '@generouted/react-router/plugin';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "netforge.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7000';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => ({
    // react-draggable (a dependency of react-grid-layout) reads process.env.* inside its drag-start
    // path. Vite doesn't shim `process` in the browser, so without these a mousedown throws
    // "process is not defined" and widget drag/resize silently die. Replace the refs at build time.
    define: {
        'process.env.DRAGGABLE_DEBUG': 'false',
        'process.env.NODE_ENV': JSON.stringify(mode),
    },
    plugins: [
        plugin(),
        tailwindcss(),
        // File-system routes live under src/pages (generouted's required convention —
        // its core hardcodes that base). Underscore-prefixed files/dirs (e.g. _template)
        // are ignored: copy-source scaffolding, mirroring the backend's _Template rule.
        generouted(),
    ],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        // Everything here lives on the backend, not this dev server — forward it so it opens from the
        // same origin you browse (and so the auth cookie, sent only to this https origin, rides along).
        proxy: {
            '^/api': {
                target,
                secure: false
            },
            // SignalR notifications hub — needs WebSocket upgrade proxied through to the API.
            '^/hubs': {
                target,
                secure: false,
                ws: true
            },
            // API docs (the Scalar UI + the OpenAPI document it reads) and the Hangfire dashboard.
            '^/scalar': { target, secure: false },
            '^/openapi': { target, secure: false },
            '^/hangfire': { target, secure: false }
        },
        port: parseInt(env.DEV_SERVER_PORT || '3000'),
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        }
    }
}))

import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

// ==========================================================
// Dev HTTPS certificate — only when a dev server is possible
// ==========================================================
const isDev = env.NODE_ENV !== 'production' || env.VITE_DEV === 'true';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = 'diba_school_portal.client';
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

// Check if `dotnet` exists in PATH
const hasDotnet =
    child_process.spawnSync('dotnet', ['--version'], { stdio: 'ignore' }).status === 0;

let httpsConfig: { key: Buffer; cert: Buffer } | undefined = undefined;

if (hasDotnet) {
    // Only try to create / read certs when dotnet is available
    if (!fs.existsSync(baseFolder)) {
        fs.mkdirSync(baseFolder, { recursive: true });
    }

    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        if (
            0 !==
            child_process.spawnSync(
                'dotnet',
                [
                    'dev-certs',
                    'https',
                    '--export-path',
                    certFilePath,
                    '--format',
                    'Pem',
                    '--no-password',
                ],
                { stdio: 'inherit' }
            ).status
        ) {
            // Non-fatal — dev will run without HTTPS, warning only
            console.warn('⚠️  Could not create dev certificate. Running without HTTPS.');
        }
    }

    if (fs.existsSync(certFilePath) && fs.existsSync(keyFilePath)) {
        httpsConfig = {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        };
    }
}

// ==========================================================
// Backend proxy target
// ==========================================================
const target =
    env['services__diba_school_portal-server__https__0'] ?? 'https://localhost:7137';

// ==========================================================
// Vite config
// ==========================================================
export default defineConfig({
    plugins: [plugin(), tailwindcss()],

    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
        },
    },

    // ======================================================
    // Production build — split heavy chunks
    // ======================================================
    build: {
        // Raise from default 500 to 800 — good enough for a school portal
        // after splitting. If still warning, add React.lazy() for routes.
        chunkSizeWarningLimit: 800,

        // Source maps off in prod (faster builds, smaller images)
        sourcemap: false,

        rollupOptions: {
            output: {
                // Function form = no risk of "package X not found" errors
                // if you remove a dependency later.
                manualChunks(id) {
                    if (!id.includes('node_modules')) return;

                    // React core + router
                    if (
                        id.includes('/react/') ||
                        id.includes('/react-dom/') ||
                        id.includes('/react-router') ||
                        id.includes('/scheduler/')
                    ) {
                        return 'react-vendor';
                    }

                    // PDF-heavy libraries (usually the biggest single chunk)
                    if (
                        id.includes('jspdf') ||
                        id.includes('html2canvas') ||
                        id.includes('pdf-lib') ||
                        id.includes('/pdfjs-dist/')
                    ) {
                        return 'pdf-vendor';
                    }

                    // Chart libraries
                    if (
                        id.includes('recharts') ||
                        id.includes('chart.js') ||
                        id.includes('d3-')
                    ) {
                        return 'charts-vendor';
                    }

                    // Everything else from node_modules
                    return 'vendor';
                },
            },
        },
    },

    // ======================================================
    // Dev server (unchanged, except conditional HTTPS)
    // ======================================================
    server: {
        proxy: {
            '/api': {
                target,
                secure: false,
                changeOrigin: true,
            },
            '/dashboardHub': {
                target,
                secure: false,
                changeOrigin: true,
                ws: true,
            },
        },
        port: parseInt(env.DEV_SERVER_PORT || '12050'),
        https: httpsConfig,
    },
});
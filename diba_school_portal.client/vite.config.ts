import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = 'diba_school_portal.client';
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

const target =
    env['services__diba_school_portal-server__https__0'] ?? 'https://localhost:7137';

export default defineConfig(({ command }) => {
    const isBuild = command === 'build';

    // ---------- Dev HTTPS cert (skipped on build) ----------
    let httpsConfig: { key: Buffer; cert: Buffer } | undefined = undefined;

    if (!isBuild) {
        const hasDotnet =
            child_process.spawnSync('dotnet', ['--version'], { stdio: 'ignore' })
                .status === 0;

        if (hasDotnet) {
            if (!fs.existsSync(baseFolder)) {
                fs.mkdirSync(baseFolder, { recursive: true });
            }

            if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
                const result = child_process.spawnSync(
                    'dotnet',
                    [
                        'dev-certs', 'https',
                        '--export-path', certFilePath,
                        '--format', 'Pem',
                        '--no-password',
                    ],
                    { stdio: 'inherit' }
                );
                if (result.status !== 0) {
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
    }

    // ---------- Config ----------
    return {
        plugins: [plugin(), tailwindcss()],

        resolve: {
            alias: {
                '@': fileURLToPath(new URL('./src', import.meta.url)),
            },
        },

        build: {
            chunkSizeWarningLimit: 800,
            sourcemap: false,
            rollupOptions: {
                output: {
                    manualChunks(id) {
                        if (!id.includes('node_modules')) return;
                        if (id.includes('/react/') || id.includes('/react-dom/') ||
                            id.includes('/react-router') || id.includes('/scheduler/')) {
                            return 'react-vendor';
                        }
                        if (id.includes('jspdf') || id.includes('html2canvas') ||
                            id.includes('pdf-lib') || id.includes('/pdfjs-dist/')) {
                            return 'pdf-vendor';
                        }
                        if (id.includes('recharts') || id.includes('chart.js') ||
                            id.includes('d3-')) {
                            return 'charts-vendor';
                        }
                        return 'vendor';
                    },
                },
            },
        },

        server: {
            proxy: {
                '/api': { target, secure: false, changeOrigin: true },
                '/dashboardHub': { target, secure: false, changeOrigin: true, ws: true },
            },
            port: parseInt(env.DEV_SERVER_PORT || '12050'),
            https: httpsConfig,
        },
    };
});
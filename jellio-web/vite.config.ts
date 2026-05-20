import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { readFileSync } from 'fs';
import path from 'path';
import { defineConfig, type Plugin } from 'vite';
import { viteSingleFile } from 'vite-plugin-singlefile';

const serveMswWorker = (): Plugin => ({
  name: 'serve-msw-worker',
  apply: 'serve',
  configureServer(server) {
    server.middlewares.use('/mockServiceWorker.js', (_req, res) => {
      const workerPath = path.resolve(
        import.meta.dirname,
        'node_modules/msw/lib/mockServiceWorker.js',
      );
      res.setHeader('Content-Type', 'application/javascript');
      res.end(readFileSync(workerPath));
    });
  },
});

export default defineConfig({
  base: '/jelliopp/',
  plugins: [react(), tailwindcss(), viteSingleFile(), serveMswWorker()],
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
  define: {
    __APP_VERSION__: JSON.stringify(process.env.npm_package_version),
  },
});

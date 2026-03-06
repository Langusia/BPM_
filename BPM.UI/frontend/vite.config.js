import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  base: '/',
  server: {
    port: 5173,
    proxy: {
      '/bpm/graph': {
        target: 'http://localhost:5100',
        changeOrigin: true,
      },
      '/bpm/schema': {
        target: 'http://localhost:5100',
        changeOrigin: true,
      },
      '/bpm/execute': {
        target: 'http://localhost:5100',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
  },
});

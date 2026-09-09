import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// API base for the dev proxy; overridable for docker via VITE_API_TARGET.
const apiTarget = process.env.VITE_API_TARGET ?? 'http://localhost:5080';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
    },
  },
});

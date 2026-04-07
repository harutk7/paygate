import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: parseInt(process.env.PORT || '18050'),
    proxy: {
      '/api/auth': {
        target: process.env.services__identity_api__http__0 || 'http://localhost:18011',
        changeOrigin: true,
      },
      '/api/plans': {
        target: process.env.services__billing_api__http__0 || 'http://localhost:18021',
        changeOrigin: true,
      },
    },
  },
});

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: parseInt(process.env.PORT || '18052'),
    proxy: {
      '/api/auth': {
        target: process.env.services__identity_api__http__0 || 'http://localhost:18011',
        changeOrigin: true,
      },
      '/api/users': {
        target: process.env.services__identity_api__http__0 || 'http://localhost:18011',
        changeOrigin: true,
      },
      '/api/admin': {
        target: process.env.services__backoffice_api__http__0 || 'http://localhost:18041',
        changeOrigin: true,
      },
    },
  },
});

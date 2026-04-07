import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: parseInt(process.env.PORT || '18051'),
    proxy: {
      '/api/auth': {
        target: process.env.services__identity_api__http__0 || 'http://localhost:18011',
        changeOrigin: true,
      },
      '/api/users': {
        target: process.env.services__identity_api__http__0 || 'http://localhost:18011',
        changeOrigin: true,
      },
      '/api/organizations': {
        target: process.env.services__identity_api__http__0 || 'http://localhost:18011',
        changeOrigin: true,
      },
      '/api/plans': {
        target: process.env.services__billing_api__http__0 || 'http://localhost:18021',
        changeOrigin: true,
      },
      '/api/subscriptions': {
        target: process.env.services__billing_api__http__0 || 'http://localhost:18021',
        changeOrigin: true,
      },
      '/api/billing': {
        target: process.env.services__billing_api__http__0 || 'http://localhost:18021',
        changeOrigin: true,
      },
      '/api/payments': {
        target: process.env.services__billing_api__http__0 || 'http://localhost:18021',
        changeOrigin: true,
      },
      '/api/invoices': {
        target: process.env.services__billing_api__http__0 || 'http://localhost:18021',
        changeOrigin: true,
      },
      '/api/apikeys': {
        target: process.env.services__gateway_api__http__0 || 'http://localhost:18031',
        changeOrigin: true,
      },
      '/api/transactions': {
        target: process.env.services__gateway_api__http__0 || 'http://localhost:18031',
        changeOrigin: true,
      },
      '/api/webhooks': {
        target: process.env.services__gateway_api__http__0 || 'http://localhost:18031',
        changeOrigin: true,
      },
      '/api/settings': {
        target: process.env.services__gateway_api__http__0 || 'http://localhost:18031',
        changeOrigin: true,
      },
    },
  },
});

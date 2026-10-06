import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The API runs on http://localhost:5080 (see src/TrustDraft.Api/Properties/launchSettings.json).
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5080',
      '/health': 'http://localhost:5080',
    },
  },
})

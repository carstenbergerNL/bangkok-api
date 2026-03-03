import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  optimizeDeps: {
    include: ['@dnd-kit/core', '@dnd-kit/utilities'],
  },
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:56028',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})

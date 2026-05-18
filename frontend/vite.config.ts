import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  const apiTarget = env.VITE_API_BASE_URL ?? 'http://localhost:7000'

  return {
    plugins: [react()],
    server: {
      port: 5174,
      strictPort: true,
      host: 'localhost',
      proxy: {
        '/api': {
          target: apiTarget,
          changeOrigin: true,
          secure: false,
        },
        '/hub': {
          target: apiTarget,
          changeOrigin: true,
          secure: false,
          ws: true,
        },
      },
    },
  }
})

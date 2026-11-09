declare global {
  interface Window {
    __APP_CONFIG__?: { apiUrl?: string }
  }
}

// Priority: runtime config (Docker) > VITE_API_URL (build/dev) > default
export const API_URL = (
  window.__APP_CONFIG__?.apiUrl ||
  import.meta.env.VITE_API_URL ||
  'http://localhost:8080'
).replace(/\/$/, '')
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { Toaster } from 'react-hot-toast'
import 'bootstrap/dist/css/bootstrap.min.css'
import '@flaticon/flaticon-uicons/css/regular/rounded.css'
import '@flaticon/flaticon-uicons/css/solid/rounded.css'
import './index.css'
import './i18n'
import App from './App.tsx'
import { queryClient } from './queryClient'
import { ErrorBoundary } from './components/ErrorBoundary'
import { AuthProvider } from './auth/AuthContext'
import { ThemeProvider } from './theme/ThemeContext'
import { ConfirmProvider } from './components/ConfirmDialog'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ErrorBoundary>
      <ThemeProvider>
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <AuthProvider>
              <ConfirmProvider>
                <App />
                <Toaster
                  position="top-right"
                  toastOptions={{
                    success: { style: { background: '#198754', color: 'white' } },
                    error: { style: { background: '#dc3545', color: 'white' } },
                  }}
                />
              </ConfirmProvider>
            </AuthProvider>
          </BrowserRouter>
          {import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
        </QueryClientProvider>
      </ThemeProvider>
    </ErrorBoundary>
  </StrictMode>,
)

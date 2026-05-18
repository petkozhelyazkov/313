import { apiClient } from '../api/client'

/**
 * Fetches a file from a protected API endpoint using the configured axios
 * client (so the JWT auth interceptor adds the Authorization header), then
 * triggers a browser download by creating a Blob URL + invisible anchor.
 *
 * Using a plain `<a href="…">` doesn't work for these endpoints because
 * opening a fresh tab doesn't carry the localStorage JWT, so the request
 * arrives at the API unauthenticated.
 */
export async function downloadFile(path: string, filename: string): Promise<void> {
  const res = await apiClient.get<Blob>(path, { responseType: 'blob' })
  const url = URL.createObjectURL(res.data)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

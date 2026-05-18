import hotToast from 'react-hot-toast'

export const toast = {
  success(message: string) {
    hotToast.success(message)
  },
  error(message: string) {
    hotToast.error(message)
  },
  info(message: string) {
    hotToast(message, { icon: 'ℹ️' })
  },
  loading(message: string) {
    return hotToast.loading(message)
  },
  dismiss(id?: string) {
    hotToast.dismiss(id)
  },
}

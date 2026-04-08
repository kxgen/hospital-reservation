import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useAlertStore = defineStore('alert', () => {
  const message = ref('')
  const type = ref('info') // success, error, warning, info
  const isVisible = ref(false)
  let timeoutId = null

  function showAlert(newMsg, newType = 'info', duration = 3000) {
    // Clear any existing timer if a new alert comes in
    if (timeoutId) clearTimeout(timeoutId)

    message.value = newMsg
    type.value = newType
    isVisible.value = true

    if (duration > 0) {
      timeoutId = setTimeout(() => {
        isVisible.value = false
      }, duration)
    }
  }

  function hideAlert() {
    isVisible.value = false
  }

  return { message, type, isVisible, showAlert, hideAlert }
})

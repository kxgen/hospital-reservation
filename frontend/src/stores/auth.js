import axios from '@/api/axios'
import { ref } from 'vue'
import { defineStore } from 'pinia'

export const useAuthStore = defineStore('auth', () => {
  // State (Reactive RAM)
  const isLoggedIn = ref(false)
  const token = ref('')
  const role = ref('')
  const name = ref('')
  const userid = ref('')
  const isPasswordChangeRequired = ref(false)
  const isLoaded = ref(false)
  const unreadNotificationCount = ref(0)

  // Sync RAM with Hard Drive on app startup
  async function loadFromStorage() {
    isLoaded.value = false

    // Small delay to ensure browser environment is ready
    await new Promise((resolve) => setTimeout(resolve, 0))

    const storedToken = localStorage.getItem('token')
    if (storedToken) {
      token.value = storedToken
      isLoggedIn.value = true
      role.value = localStorage.getItem('role') || ''
      name.value = localStorage.getItem('name') || ''
      userid.value = localStorage.getItem('userid') || ''
      isPasswordChangeRequired.value = localStorage.getItem('isPasswordChangeRequired') === 'true'
    }

    isLoaded.value = true
  }

  // Clear everything on Logout
  function logout() {
    localStorage.clear() // Wipes Hard Drive

    // Wipe RAM
    token.value = ''
    isLoggedIn.value = false
    role.value = ''
    name.value = ''
    userid.value = ''
    isPasswordChangeRequired.value = false
    unreadNotificationCount.value = 0
    isLoaded.value = true
  }

  async function fetchUnreadCount() {
    if (!token.value || role.value?.toLowerCase() !== 'patient') return
    try {
      const res = await axios.get('/api/notifications/unread-count', {
        headers: { Authorization: `Bearer ${token.value}` },
      })
      unreadNotificationCount.value = res.data.count || 0
    } catch (err) {
      console.error('Error fetching unread count', err)
    }
  }

  return {
    isLoggedIn,
    token,
    role,
    name,
    userid,
    isPasswordChangeRequired,
    isLoaded,
    unreadNotificationCount,
    loadFromStorage,
    logout,
    fetchUnreadCount,
  }
})

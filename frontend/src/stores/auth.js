import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useAuthStore = defineStore('auth', () => {
  const isLoggedIn = ref(false)
  const role = ref('')
  const name = ref('')
  const userid = ref('')
  
  // 🔑 NEW: A flag to track if the store has finished loading user data.
  const isLoaded = ref(false) 

  // Make the function async to support waiting in components (Header.vue).
  async function loadFromStorage() {
    // 1. Reset isLoaded flag (useful if this is called multiple times)
    isLoaded.value = false
    
    // Simulate async operation if you were fetching from an API,
    // but even for sync localStorage, keeping it async/awaitable 
    // resolves the component rendering race condition.
    // We'll use a tiny, quick await just to confirm the pattern.
    await new Promise(resolve => setTimeout(resolve, 0)) 
    
    // 2. Load data from localStorage
    isLoggedIn.value = !!localStorage.getItem('token')
    role.value = localStorage.getItem('role') || ''
    name.value = localStorage.getItem('name') || ''
    userid.value = localStorage.getItem('userid') || ''
    
    // 3. 🏁 Set the flag to true AFTER loading is complete
    isLoaded.value = true
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('role')
    localStorage.removeItem('name')
    localStorage.removeItem('userid')
    
    // Reset state values
    isLoggedIn.value = false
    role.value = ''
    name.value = ''
    userid.value = ''
    
    // Reset loaded state, though it will immediately become true again 
    // when a component calls loadFromStorage on next load.
    isLoaded.value = true
  }

  // 4. Return the new state property
  return { 
    isLoggedIn, 
    role, 
    name, 
    userid, 
    isLoaded, // ⬅️ IMPORTANT: Expose the new flag
    loadFromStorage, 
    logout 
  }
})
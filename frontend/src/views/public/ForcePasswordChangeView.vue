<script setup>
import { ref } from 'vue'
import axios from '@/api/axios'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'

const auth = useAuthStore()
const alertStore = useAlertStore()
const router = useRouter()

const newPassword = ref('')
const confirmPassword = ref('')
const isLoading = ref(false)

const roleRoutes = {
  'patient': '/patient/dashboard',
  'doctor': '/doctor/dashboard',
  'receptionist': '/reception/dashboard',
  'admin': '/admin/dashboard',
}

const handleSubmit = async () => {
  if (newPassword.value !== confirmPassword.value) {
    alertStore.showAlert('New passwords do not match.', 'error')
    return
  }

  if (newPassword.value.length < 6) {
    alertStore.showAlert('Password must be at least 6 characters long.', 'error')
    return
  }

  isLoading.value = true
  try {
    await axios.post('/api/account/change-password', {
      current: "", // Not required for forced reset
      new: newPassword.value
    }, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })

    // Success
    auth.isPasswordChangeRequired = false
    localStorage.setItem('isPasswordChangeRequired', 'false')
    alertStore.showAlert('Password updated successfully! Welcome to the platform.', 'success')
    
    const targetRoute = roleRoutes[auth.role.toLowerCase()] || '/'
    router.push(targetRoute)
  } catch (error) {
    console.error('Password change failed', error)
    const msg = error.response?.data?.message || 'Failed to update password. Please check your current password.'
    alertStore.showAlert(msg, 'error')
  } finally {
    isLoading.value = false
  }
}

const handleLogout = () => {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <div class="force-change-page">
    <div class="auth-card">
      <div class="card-header">
        <div class="icon-circle">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>
        </div>
        <h1>Update Password</h1>
        <p>To ensure your account remains secure, please set a new password before continuing to your dashboard.</p>
      </div>

      <form @submit.prevent="handleSubmit" class="password-form">
        <div class="form-group">
          <label>New Password</label>
          <input 
            v-model="newPassword" 
            type="password" 
            placeholder="Min. 6 characters" 
            required
          >
        </div>

        <div class="form-group">
          <label>Confirm Password</label>
          <input 
            v-model="confirmPassword" 
            type="password" 
            placeholder="Repeat new password" 
            required
          >
        </div>

        <div class="actions">
          <button type="submit" class="btn-primary" :disabled="isLoading">
            {{ isLoading ? 'Processing...' : 'Update & Access Account' }}
          </button>
          <button type="button" class="btn-link" @click="handleLogout">
            Sign out
          </button>
        </div>
      </form>

      <div class="security-notice">
        <span class="lightbulb">💡</span>
        <p>Strong passwords use a mix of letters, numbers, and symbols.</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Main Container - Light Theme Base */
.force-change-page {
  min-height: 100vh;
  background-color: var(--bg-body); /* #F5F5F0 from your CSS */
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}

/* Card Surface */
.auth-card {
  width: 100%;
  max-width: 420px;
  background: var(--bg-card); /* White */
  border: 1px solid var(--border-default);
  border-radius: 0;
  padding: 40px;
  box-shadow: var(--shadow-md);
}

.card-header {
  text-align: center;
  margin-bottom: 28px;
}

.icon-circle {
  width: 52px;
  height: 52px;
  background: var(--bg-input); /* Soft green tint replacement */
  color: var(--color-primary); /* Theme Green */
  border-radius: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 16px;
}

/* Typography Adjustments */
h1 {
  font-size: 1.4rem; /* Reduced from 1.8rem */
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 8px;
  letter-spacing: -0.3px;
}

.card-header p {
  color: var(--text-secondary);
  line-height: 1.5;
  font-size: 0.88rem; /* Reduced from 0.95rem */
  margin: 0;
}

/* Form Styling */
.password-form {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

label {
  font-size: 0.7rem; /* Small & Professional */
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--text-tertiary);
}

input {
  background: var(--bg-input);
  border: 1px solid var(--border-input);
  border-radius: 0;
  padding: 12px 14px;
  color: var(--text-primary);
  font-size: 0.95rem;
  transition: all 0.2s ease;
}

input:focus {
  outline: none;
  border-color: var(--border-focus);
  box-shadow: 0 0 0 3px var(--color-primary-glow);
}

input::placeholder {
  color: var(--text-tertiary);
}

/* Action Buttons */
.actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 10px;
}

.btn-primary {
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-primary);
  color: var(--text-inverse);
  border: none;
  border-radius: 0;
  padding: 14px;
  font-weight: 600;
  font-size: 0.9rem;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-primary:hover:not(:disabled) {
  background: var(--color-primary-hover);
  box-shadow: var(--shadow-primary);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-link {
  background: transparent;
  color: var(--text-secondary);
  border: none;
  padding: 8px;
  font-weight: 500;
  font-size: 0.85rem;
  cursor: pointer;
  transition: color 0.2s;
}

.btn-link:hover {
  color: var(--text-error);
}

/* Footer Note */
.security-notice {
  margin-top: 24px;
  padding: 12px 16px;
  background: var(--bg-input);
  border: 1px solid var(--border-default);
  border-radius: 0;
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.lightbulb {
  font-size: 1rem;
}

.security-notice p {
  font-size: 0.8rem;
  color: var(--text-secondary);
  margin: 0;
  line-height: 1.4;
}

@media (max-width: 480px) {
  .auth-card {
    padding: 30px 20px;
    border: none;
    box-shadow: none;
    background: transparent;
  }
}
</style>
<script setup>
import Header from '@/layouts/Header.vue'
import Footer from '@/layouts/Footer.vue'
import axios from '@/api/axios'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'

const auth = useAuthStore()
const alertStore = useAlertStore()

const router = useRouter()

const email = ref('')
const password = ref('')

const roleRoutes = {
  'patient': '/patient/dashboard',
  'doctor': '/doctor/dashboard',
  'receptionist': '/reception/dashboard',
  'admin': '/admin/dashboard',
}

const login = async () => {
  try {
    const response = await axios.post('/api/account/login', {
      email: email.value,
      password: password.value,
    })

    const { token, user } = response.data

    localStorage.setItem('token', token)
    localStorage.setItem('role', user.role)
    localStorage.setItem('userid', user.id)
    localStorage.setItem('name', user.firstName + ' ' + user.lastName)
    localStorage.setItem('isPasswordChangeRequired', user.isPasswordChangeRequired)

    auth.token = token
    auth.isLoggedIn = true
    auth.role = user.role
    auth.userid = user.id
    auth.name = user.firstName + ' ' + user.lastName
    auth.isPasswordChangeRequired = !!user.isPasswordChangeRequired
    auth.isLoaded = true

    if (auth.isPasswordChangeRequired) {
      localStorage.setItem('isPasswordChangeRequired', 'true')
      router.push('/force-password-change')
      alertStore.showAlert('Security protocol: Please set a new password.', 'info')
    } else {
      localStorage.setItem('isPasswordChangeRequired', 'false')
      const targetRoute = roleRoutes[user.role.toLowerCase()] || '/'
      
      // Fetch unread count immediately after login
      auth.fetchUnreadCount()
      
      router.push(targetRoute)
      alertStore.showAlert(`Welcome back, ${user.firstName}!`, 'success')
    }
  } catch (error) {
    if (error.response) {
      alertStore.showAlert(error.response.data.message || 'Invalid credentials', 'error')
    } else {
      alertStore.showAlert('Server error - please try again later', 'error')
    }
  }
}
</script>

<template>
  <div class="login-view">
    <Header />
    <div class="login-wrapper">
      <form class="login-form" @submit.prevent="login">
        <h2 class="form-title">Login</h2>

        <!-- Email Field -->
        <div class="form-group">
          <label for="email">Email</label>
          <input
            v-model="email"
            name="email"
            id="email"
            type="email"
            placeholder="Enter your email"
            required
          />
        </div>

        <!-- Password Field -->
        <div class="form-group">
          <label for="password">Password</label>
          <input
            v-model="password"
            name="password"
            id="password"
            type="password"
            placeholder="Enter your password"
            required
          />
        </div>

        <button type="submit" class="btn-submit">Login</button>

        <p class="redirect-text">
          Don’t have an account?
          <router-link to="/register">Register</router-link>
        </p>
        <router-link to="/forgot-password" class="forgot-link">Forgot Password?</router-link>
      </form>
    </div>
    <Footer />
  </div>
</template>

<style scoped>
/* Theme Color: #00b39e (Teal/Turquoise) */

.login-view {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: var(--bg-body); /* Lighter, more inviting background */
}

/* Ensure the wrapper centers the form vertically on smaller screens, 
   but starts higher for better desktop aesthetics */
.login-wrapper {
  flex: 1;
  display: flex;
  justify-content: center;
  align-items: flex-start; /* Start high on desktop */
  padding-top: 100px; /* More space from the top */
  padding-bottom: 50px;
}

.login-form {
  width: 380px; /* Slightly wider for better visual balance */
  background: var(--bg-card);
  border: 1px solid var(--border-default); /* Softer border */
  padding: 2.5rem; /* Increased padding */
  border-radius: 0; /* Sharp corners globally */
  display: flex;
  flex-direction: column;
  margin-top: 0; /* Let padding-top on wrapper handle vertical placement */
  box-shadow: var(--shadow-md); /* Clearer shadow for depth */
}

.form-title {
  text-align: center;
  margin-bottom: 1.5rem;
  font-size: 1.6rem; /* Larger, more prominent title */
  font-weight: 700;
  color: var(--text-primary);
}

/* Form Group Layout */
.form-group {
  display: flex;
  flex-direction: column;
  margin-bottom: 1.25rem;
}

label {
  font-size: 0.95rem;
  margin-bottom: 0.5rem;
  color: var(--text-secondary);
  font-weight: 600;
}

/* Input Styling */
input {
  padding: 0.75rem 1rem; /* Generous padding */
  border: 1px solid var(--border-input); /* Lighter default border */
  border-radius: 6px;
  font-size: 1rem;
  transition:
  box-shadow 0.3s;
}

.forgot-link {
  display: block;
  text-align: center;
  font-size: 0.85rem;
  color: var(--color-primary);
  text-decoration: none;
  margin-top: 6px;
  font-weight: 600;
}
.forgot-link:hover { text-decoration: underline; }

input:focus {
  border-color: var(--border-focus); /* Theme color focus */
  box-shadow: 0 0 0 3px var(--color-primary-glow); /* Soft theme glow */
  outline: none;
}

/* Submit Button (Primary Action) */
.btn-submit {
  padding: 0.8rem;
  background: var(--color-primary); /* Primary theme color */
  color: var(--color-white);
  border: none;
  border-radius: 8px;
  font-weight: 700;
  cursor: pointer;
  margin-top: 1rem;
  font-size: 1rem;
  transition:
    background 0.3s,
    transform 0.1s;
}

.btn-submit:hover {
  background: var(--color-primary-hover); /* Darker theme color on hover */
  transform: translateY(-1px);
}

/* Link/Redirect Text */
.redirect-text {
  text-align: center;
  font-size: 0.9rem;
  margin-top: 1.5rem;
  color: var(--text-secondary);
}

.redirect-text a {
  color: var(--color-primary); /* Theme color link */
  text-decoration: none;
  font-weight: 600;
  transition: color 0.2s;
}

.redirect-text a:hover {
  color: var(--color-primary-hover);
  text-decoration: underline;
}

/* Mobile adjustments */
@media (max-width: 480px) {
  .login-wrapper {
    padding: 50px 20px;
    align-items: center; /* Center vertically on smaller screens */
  }
  .login-form {
    width: 100%;
    max-width: 360px;
    padding: 2rem 1.5rem;
    box-shadow: 0 0 8px rgba(0, 0, 0, 0.1);
  }
  .form-title {
    font-size: 1.4rem;
  }
}
</style>

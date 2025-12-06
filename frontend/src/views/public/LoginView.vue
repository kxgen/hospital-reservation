<script setup>
import Header from '@/components/Header.vue'
import axios from 'axios'
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const email = ref('')
const password = ref('')
const message = ref('')

const login = async () => {
  try {
    const response = await axios.post('http://localhost:5150/api/auth/login', {
      email: email.value,
      password: password.value
    })

    console.log(response)
    const token = response.data.token
    const user = response.data.user

    // Save token + role + name + id
    localStorage.setItem('token', token)
    localStorage.setItem('role', user.role)
    localStorage.setItem('userid', user.id)
    localStorage.setItem('name', user.firstName + ' ' + user.lastName)

    console.log('Token stored:', token)
    console.log('Logged user:', user)

    message.value = `Welcome ${user.firstName}`

    // Redirect based on role
    const roleRoutes = {
      patient: '/',
      doctor: '/doctor/dashboard',
      receptionist: '/reception/dashboard',
      admin: '/admin/dashboard'
    }

    router.push(roleRoutes[user.role] || '/')


  } catch (error) {
    if (error.response) {
      message.value = error.response.data.message || 'Invalid credentials'
    } else {
      message.value = 'Server error'
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

        <!-- feedback message -->
        <p v-if="message" class="login-message">{{ message }}</p>

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

      </form>
    </div>
  </div>
</template>

<style scoped>
.login-view {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: #f8f8f8;
}

.login-wrapper {
  flex: 1;
  display: flex;
  justify-content: center;
  align-items: flex-start;
  padding-top: 60px;
}

.login-form {
  width: 360px;
  background: #fff;
  border: 1px solid #000;
  padding: 2rem;
  border-radius: 6px;
  display: flex;
  flex-direction: column;
  margin-top: 2rem;
  box-shadow: 0 0 5px rgba(0, 0, 0, 0.1);
}

.form-title {
  text-align: center;
  margin-bottom: 1.5rem;
  font-size: 1.3rem;
  font-weight: bold;
  color: #000;
}

.form-group {
  display: flex;
  flex-direction: column;
  margin-bottom: 1rem;
}

label {
  font-size: 0.9rem;
  margin-bottom: 0.4rem;
  color: #333;
}

input {
  padding: 0.6rem;
  border: 1px solid #000;
  border-radius: 3px;
  font-size: 0.9rem;
}

input:focus {
  border-color: #000;
  outline: none;
}

.btn-submit {
  padding: 0.7rem;
  background: #000;
  color: #fff;
  border: none;
  border-radius: 3px;
  font-weight: bold;
  cursor: pointer;
  margin-top: 0.5rem;
}

.btn-submit:hover {
  background: #333;
}

.redirect-text {
  text-align: center;
  font-size: 0.9rem;
  margin-top: 1rem;
}

.redirect-text a {
  color: #000;
  text-decoration: underline;
}

.login-message {
  margin-top: 1rem;
  text-align: center;
  font-weight: bold;
  color: #ff1d1d;
}
</style>

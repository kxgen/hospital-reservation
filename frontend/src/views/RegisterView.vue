<script setup>
import { reactive, ref } from 'vue';
import Header from '@/components/Header.vue';
import axios from 'axios';

const message = ref('')

const form = reactive({
  firstName: '',
  lastName: '',
  dateOfBirth: '',
  gender: '',
  phone: '',
  email: '',
  password: '',
  confirmPassword: ''
})

const register = async function () {
  if (form.password !== form.confirmPassword) {
    alert("Passwords do not match")
    return;
  }

  try {
    const response = await axios.post(
      'http://localhost:5150/api/auth/register', form)
    
      message.value = 'Registration successful.'
  } catch (error) {
    if (error.response) {
      message.value = error.response.data?.message || 'Something went wrong.'
    } 
    else {
      message.value = 'Network error. Please try again.'
    }
  }
}
</script>

<template>
  <div>
    <Header />
    <div class="register-container">
      <form class="register-card" @submit.prevent="register">
        <h2 class="form-title">Patient Registration</h2>
        {{ message }}
        <div class="form-row">
          <div class="form-group">
            <label>First Name</label>
            <input v-model="form.firstName" type="text" placeholder="Enter your first name" required/>
          </div>

          <div class="form-group">
            <label>Last Name</label>
            <input v-model="form.lastName" type="text" placeholder="Enter your last name" required/>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Date of Birth</label>
            <input v-model="form.dateOfBirth" type="date" required />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Gender</label>
            <div class="gender-options">
              <label><input v-model="form.gender" type="radio" name="gender" value="Female" /> Female</label>
              <label><input v-model="form.gender" type="radio" name="gender" value="Male" /> Male</label>
            </div>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Phone Number</label>
            <input v-model="form.phone" type="tel" placeholder="Enter your phone number" required/>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Email</label>
            <input v-model="form.email" type="email" placeholder="Enter your email" required/>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group full-width">
            <label>New Password</label>
            <input v-model="form.password" type="password" placeholder="Enter your password" required/>
          </div>

          <div class="form-group full-width">
            <label>Confirm Password</label>
            <input v-model="form.confirmPassword" type="password" placeholder="Confirm your password" required/>
          </div>
        </div>

        <button type="submit" class="btn">Register</button>

        <p class="redirect-text">
          Already have an account?
          <router-link to="/login">Login</router-link>
        </p>
      </form>
    </div>
  </div>
</template>

<style scoped>
.register-container {
  margin-top: 60px;
  display: flex;
  justify-content: center;
  align-items: flex-start;
  min-height: calc(100vh - 60px);
  padding: 2rem;
  background: #f8f8f8;
}

.register-card {
  background: #fff;
  border: 1px solid #000;
  border-radius: 10px;
  padding: 2rem 2.5rem;
  width: 700px;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.08);
}

.form-title {
  text-align: center;
  margin-bottom: 1.5rem;
  font-weight: bold;
  font-size: 1.5rem;
  letter-spacing: 0.5px;
}

/* Rows for two-column layout */
.form-row {
  display: flex;
  gap: 1.5rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}

/* Single column form groups */
.form-group {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.full-width {
  flex: 1 1 100%;
}

label {
  font-size: 0.9rem;
  font-weight: 600;
  margin-bottom: 0.3rem;
}

.optional-text {
  font-weight: normal;
  font-size: 0.8rem;
  color: #666;
  margin-left: 0.5rem;
}

input[type="text"],
input[type="email"],
input[type="password"],
input[type="tel"],
input[type="date"] {
  padding: 0.6rem 0.8rem;
  border: 1px solid #000;
  border-radius: 4px;
  font-size: 0.9rem;
  width: 100%;
  transition: border-color 0.2s ease;
}

input:focus {
  border-color: #333;
  outline: none;
}

.gender-options {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-top: 0.3rem;
}

.btn {
  margin-top: 1.5rem;
  width: 100%;
  padding: 0.8rem;
  background: black;
  color: white;
  font-weight: bold;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  transition: background 0.2s ease-in-out;
}

.btn:hover {
  background: #222;
}

.redirect-text {
  text-align: center;
  font-size: 0.9rem;
  margin-top: 1rem;
}

.redirect-text a {
  color: black;
  text-decoration: underline;
}
</style>

<script setup>
import { reactive, ref } from 'vue'
import Header from '@/layouts/Header.vue'
import Footer from '@/layouts/Footer.vue'
import axios from '@/api/axios'
import { useRouter } from 'vue-router'
import { useAlertStore } from '@/stores/alertStore'

const router = useRouter()
const alertStore = useAlertStore()
const currentStep = ref(1)

const form = reactive({
  firstName: '',
  lastName: '',
  dateOfBirth: '',
  gender: '',
  phone: '',
  email: '',
  password: '',
  confirmPassword: '',
})

const validateStep1 = () => {
  if (!form.firstName || !form.lastName || !form.dateOfBirth || !form.gender || !form.phone) {
    alertStore.showAlert('Please fill out all required fields for Step 1.', 'error')
    return false
  }
  if (!/^\d{10,}$/.test(form.phone)) {
    alertStore.showAlert('Please enter a valid phone number.', 'error')
    return false
  }
  return true
}

const nextStep = () => {
  if (validateStep1()) {
    currentStep.value = 2
  }
}

const prevStep = () => {
  currentStep.value = 1
}

const register = async function () {
  if (form.password !== form.confirmPassword) {
    alertStore.showAlert('Passwords do not match.', 'error')
    return
  }
  if (!form.email || !form.password) {
    alertStore.showAlert('Email and Password are required.', 'error')
    return
  }

  try {
    console.log(form)
    const response = await axios.post('/api/account/register', form)

    // Success handling
    alertStore.showAlert('Registration successful. You can now login.', 'success')
    // Optionally redirect to login page here after success
    router.push('/login');
  } catch (error) {
    if (error.response) {
      alertStore.showAlert(error.response.data?.message || 'Something went wrong during registration.', 'error')
    } else {
      alertStore.showAlert('Network error. Please try again.', 'error')
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

        <div class="progress-indicator">
          <div class="step" :class="{ active: currentStep >= 1, completed: currentStep > 1 }">
            1. Personal Info
          </div>
          <div class="line" :class="{ active: currentStep > 1 }"></div>
          <div class="step" :class="{ active: currentStep >= 2 }">2. Account Details</div>
        </div>

        <div v-if="currentStep === 1">
          <h3>Step 1: Your Personal Details</h3>

          <div class="form-row">
            <div class="form-group">
              <label>First Name</label>
              <input
                v-model="form.firstName"
                type="text"
                placeholder="Enter your first name"
                required
              />
            </div>

            <div class="form-group">
              <label>Last Name</label>
              <input
                v-model="form.lastName"
                type="text"
                placeholder="Enter your last name"
                required
              />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Date of Birth</label>
              <input v-model="form.dateOfBirth" type="date" required
                placeholder="Date of Birth (DD/MM/YYYY)" 
                onfocus="(this.type='date')"
                onblur="if(!this.value)this.type='text'"/>
            </div>
            <div class="form-group">
              <label>Phone Number</label>
              <input
                v-model="form.phone"
                type="tel"
                placeholder="Enter your phone number"
                required
              />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group full-width">
              <label>Gender</label>
              <div class="gender-options">
                <label
                  ><input
                    v-model="form.gender"
                    type="radio"
                    name="gender"
                    value="Female"
                    required
                  />
                  Female</label
                >
                <label
                  ><input v-model="form.gender" type="radio" name="gender" value="Male" required />
                  Male</label
                >
              </div>
            </div>
          </div>

          <div class="form-navigation">
            <button type="button" class="btn next-btn" @click="nextStep">Next Step</button>
          </div>
        </div>

        <div v-if="currentStep === 2">
          <h3>Step 2: Account Security</h3>

          <div class="form-row">
            <div class="form-group full-width">
              <label>Email</label>
              <input v-model="form.email" type="email" placeholder="Enter your email" required />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>New Password</label>
              <input
                v-model="form.password"
                type="password"
                placeholder="Enter your password"
                required
              />
            </div>

            <div class="form-group">
              <label>Confirm Password</label>
              <input
                v-model="form.confirmPassword"
                type="password"
                placeholder="Confirm your password"
                required
              />
            </div>
          </div>

          <div class="form-navigation">
            <button type="button" class="btn prev-btn" @click="prevStep">Back</button>
            <button type="submit" class="btn submit-btn">Register</button>
          </div>
        </div>

        <p class="redirect-text">
          Already have an account?
          <router-link to="/login">Login</router-link>
        </p>
      </form>
    </div>
    <Footer />
  </div>
</template>

<style scoped>
/* Theme Color: #00b39e (Teal/Turquoise) */

.register-container {
  /* Pushes content below the Header, assuming Header is around 60-70px high */
  padding-top: 100px;
  padding-bottom: 40px;
  display: flex;
  justify-content: center;
  align-items: flex-start; /* Aligns content slightly higher */
  min-height: 100vh;
  background: var(--bg-body);
  box-sizing: border-box;
}

.register-card {
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  border-radius: 0;
  padding: 2.5rem 3rem;
  width: 100%;
  max-width: 650px; /* Reduced max-width slightly as fields are fewer per step */
  box-shadow: var(--shadow-lg);
}

.form-title {
  text-align: center;
  margin-bottom: 1.5rem;
  font-weight: 700;
  font-size: 1.8rem;
  color: var(--text-primary);
}

/* === NEW: Progress Indicator === */
.progress-indicator {
  display: flex;
  justify-content: center;
  align-items: center;
  margin-bottom: 2rem;
  position: relative;
}

.progress-indicator .step {
  z-index: 10;
  font-weight: 600;
  font-size: 0.9rem;
  text-align: center;
  color: var(--text-tertiary);
  padding: 5px 10px;
  position: relative;
  flex-basis: 50%;
}

.progress-indicator .step::before {
  content: attr(data-step-number); /* Not used in this template but useful for styling */
  display: block;
  width: 30px;
  height: 30px;
  line-height: 30px;
  border-radius: 50%;
  background: var(--color-gray-200);
  color: var(--color-white);
  font-size: 1rem;
  font-weight: 700;
  margin: 0 auto 5px;
  transition:
    background 0.3s,
    border-color 0.3s;
  border: 2px solid var(--color-gray-200);
}
.progress-indicator .step:first-child::before {
  content: '1';
}
.progress-indicator .step:last-child::before {
  content: '2';
}

.progress-indicator .step.active {
  color: var(--color-primary);
}

.progress-indicator .step.active::before,
.progress-indicator .step.completed::before {
  background: var(--color-primary);
  border-color: var(--color-primary);
}

.progress-indicator .line {
  position: absolute;
  height: 4px;
  background: var(--color-gray-200);
  width: 45%;
  top: 21px; /* Center with the dots */
  left: 27.5%;
}

.progress-indicator .line.active {
  background: var(--color-primary);
  width: 50%;
}
/* === END Progress Indicator === */

h3 {
  font-size: 1.25rem;
  color: var(--text-primary);
  margin-top: 0;
  margin-bottom: 20px;
  padding-bottom: 10px;
  border-bottom: 1px dashed var(--border-default);
}

/* Rows and Groups */
.form-row {
  display: flex;
  gap: 25px;
  margin-bottom: 1.2rem;
  flex-wrap: wrap;
}

.form-group {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.full-width {
  flex: 1 1 100%;
}

label {
  font-size: 0.95rem;
  font-weight: 600;
  margin-bottom: 0.5rem;
  color: var(--text-secondary);
}

/* Input Styles */
input[type='text'],
input[type='email'],
input[type='password'],
input[type='tel'],
input[type='date'] {
  padding: 0.75rem 1rem;
  border: 1px solid var(--border-input);
  border-radius: 6px;
  font-size: 1rem;
  width: 100%;
  box-sizing: border-box;
  background: var(--bg-input);
  color: var(--text-primary);
}

input:focus {
  border-color: var(--border-focus);
  box-shadow: 0 0 0 3px var(--color-primary-glow);
  outline: none;
}

.gender-options {
  display: flex;
  flex-direction: row;
  gap: 20px;
  margin-top: 5px;
  padding: 5px 0;
}
.gender-options label {
  font-weight: normal;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  cursor: pointer;
}
.gender-options input[type='radio'] {
  margin-right: 8px;
  accent-color: var(--color-primary);
}

/* === NEW: Navigation Buttons === */
.form-navigation {
  display: flex;
  justify-content: space-between;
  gap: 15px;
  margin-top: 2rem;
}

.btn {
  padding: 0.85rem;
  font-weight: 700;
  font-size: 1.05rem;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  flex: 1;
  text-align: center;
}

/* Primary Button (Next/Submit) */
.btn.submit-btn,
.btn.next-btn {
  background: var(--color-primary);
  color: var(--color-white);
}
.btn.submit-btn:hover,
.btn.next-btn:hover {
  background: var(--color-primary-hover);
  transform: translateY(-1px);
}

/* Secondary Button (Back) */
.btn.prev-btn {
  background: var(--bg-input);
  color: var(--text-secondary);
  border: 1px solid var(--border-default);
}
.btn.prev-btn:hover {
  background: var(--border-default);
  color: var(--text-primary);
}

.redirect-text {
  text-align: center;
  font-size: 0.9rem;
  margin-top: 1.5rem;
  color: var(--text-secondary);
}

.redirect-text a {
  color: var(--color-primary);
  text-decoration: none;
  font-weight: 600;
}

.redirect-text a:hover {
  text-decoration: underline;
}

/* === MEDIA QUERIES === */

@media (max-width: 750px) {
  .register-card {
    padding: 2rem;
    max-width: 100%;
  }

  .form-row {
    flex-direction: column;
    gap: 15px; /* Added gap for stacked mobile inputs */
  }

  .progress-indicator .line {
    width: 40%;
    left: 30%;
  }
}

@media (max-width: 480px) {
  .register-container {
    padding-top: 80px;
  }
  .register-card {
    padding: 1.5rem;
  }
  .form-title {
    font-size: 1.4rem;
  }
  .gender-options {
    flex-direction: column;
    gap: 0.5rem;
  }
  .form-navigation {
    flex-direction: column;
    gap: 10px;
  }
  .progress-indicator .step {
    font-size: 0.8rem;
  }
}
</style>

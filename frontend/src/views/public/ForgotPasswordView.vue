<script setup>
import { ref } from 'vue'
import axios from '@/api/axios'
import Header from '@/layouts/Header.vue'
import Footer from '@/layouts/Footer.vue'

const email = ref('')
const otp = ref('')
const isLoading = ref(false)
const step = ref(1) // 1: Request, 2: Verify, 3: Success
const errorMessage = ref('')

const submitRequest = async () => {
    isLoading.value = true
    errorMessage.value = ''
    try {
        await axios.post('/api/account/forgot-password', {
            email: email.value
        })
        step.value = 2
    } catch (error) {
        console.error(error)
        // We still move to step 2 to prevent enumeration, or show error if it's a real connection issue
        if (error.response) {
             step.value = 2
        } else {
            errorMessage.value = 'Failed to connect to the server. Please try again.'
        }
    } finally {
        isLoading.value = false
    }
}

const verifyOtp = async () => {
    isLoading.value = true
    errorMessage.value = ''
    try {
        await axios.post('/api/account/reset-password-otp', {
            email: email.value,
            otp: otp.value
        })
        step.value = 3
    } catch (error) {
        console.error(error)
        errorMessage.value = error.response?.data?.message || 'Invalid or expired OTP. Please try again.'
    } finally {
        isLoading.value = false
    }
}
</script>

<template>
    <div class="page-wrapper">
        <Header />
        <div class="content-container">
            <div class="auth-card">
                <div class="card-header">
                     <div class="icon-circle">
                        <span class="icon">{{ step === 3 ? '🎉' : '🔑' }}</span>
                    </div>
                    <h1>{{ step === 3 ? 'Check Your Email' : 'Reset Password' }}</h1>
                    <p v-if="step === 1">Enter your email address to receive a 6-digit verification code.</p>
                    <p v-if="step === 2">We've sent a code to <strong>{{ email }}</strong>. Enter it below to proceed.</p>
                    <p v-if="step === 3">Your identity has been verified!</p>
                </div>

                <!-- Step 1: Request OTP -->
                <div v-if="step === 1">
                     <form @submit.prevent="submitRequest" class="auth-form">
                        <div v-if="errorMessage" class="error-alert">{{ errorMessage }}</div>
                        <div class="form-group">
                            <label>Email Address</label>
                            <input 
                                v-model="email" 
                                type="email" 
                                placeholder="name@example.com" 
                                required 
                                class="form-input"
                            />
                        </div>

                        <div class="actions">
                            <button type="submit" class="btn-primary" :disabled="isLoading">
                                {{ isLoading ? 'Sending Code...' : 'Send Verification Code' }}
                            </button>
                             <router-link to="/login" class="btn-link">Back to Login</router-link>
                        </div>
                    </form>
                </div>

                <!-- Step 2: Verify OTP -->
                <div v-else-if="step === 2">
                     <form @submit.prevent="verifyOtp" class="auth-form">
                        <div v-if="errorMessage" class="error-alert">{{ errorMessage }}</div>
                        <div class="form-group">
                            <label>Verification Code (OTP)</label>
                            <input 
                                v-model="otp" 
                                type="text" 
                                placeholder="123456" 
                                required 
                                maxlength="6"
                                class="form-input otp-input"
                            />
                        </div>

                        <div class="actions">
                            <button type="submit" class="btn-primary" :disabled="isLoading">
                                {{ isLoading ? 'Verifying...' : 'Verify & Send Temp Password' }}
                            </button>
                             <button @click="step = 1" type="button" class="btn-link">Try a different email</button>
                        </div>
                    </form>
                </div>

                <!-- Step 3: Success -->
                <div v-else class="success-state">
                    <div class="success-icon">✓</div>
                    <h3>Temporary Password Emailed</h3>
                    <p>A new temporary password has been sent to <strong>{{ email }}</strong>.</p>
                    <div class="instruction">
                        Please use the temporary password to login. You will be asked to create a new permanent password immediately after logging in.
                    </div>
                    <router-link to="/login" class="btn-primary full-width">Proceed to Login</router-link>
                </div>
            </div>
        </div>
        <Footer />
    </div>
</template>

<style scoped>
.page-wrapper {
    min-height: 100vh;
    background: var(--bg-body);
    display: flex;
    flex-direction: column;
}

.content-container {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 20px;
}

.auth-card {
    margin-top: 4rem;
    background: var(--bg-card);
    width: 100%;
    max-width: 420px;
    padding: 40px;
    border-radius: 0;
    box-shadow: var(--shadow-md);
    border: 1px solid var(--border-default);
    text-align: center;
}

.icon-circle {
    width: 60px; height: 60px;
    background: var(--bg-input);
    color: var(--color-primary);
    border-radius: 50%;
    display: flex; align-items: center; justify-content: center;
    margin: 0 auto 20px;
    font-size: 1.5rem;
}

h1 {
    font-size: 1.5rem;
    color: var(--text-primary);
    margin-bottom: 10px;
}

p {
    color: var(--text-secondary);
    font-size: 0.95rem;
    line-height: 1.5;
}

.auth-form {
    margin-top: 30px;
    text-align: left;
}

.form-group {
    margin-bottom: 20px;
}

.error-alert {
    background: #fff5f5;
    color: #e53e3e;
    padding: 12px;
    border-radius: 8px;
    margin-bottom: 20px;
    font-size: 0.9rem;
    border-left: 4px solid #e53e3e;
}

label {
    display: block;
    font-size: 0.8rem;
    font-weight: 700;
    color: var(--text-secondary);
    text-transform: uppercase;
    margin-bottom: 8px;
}

.form-input {
    width: 100%;
    padding: 12px;
    border: 1px solid var(--border-input);
    border-radius: 0;
    background: var(--bg-input);
    color: var(--text-primary);
    font-size: 1rem;
    transition: all 0.2s;
}

.otp-input {
    text-align: center;
    letter-spacing: 0.5rem;
    font-size: 1.25rem;
    font-weight: 700;
}

.form-input:focus {
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px var(--color-primary-glow);
    outline: none;
}

.actions {
    display: flex;
    flex-direction: column;
    gap: 15px;
    margin-top: 10px;
}

.btn-primary {
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--color-primary);
    color: white;
    border: none;
    padding: 14px;
    border-radius: 8px;
    font-weight: 700;
    cursor: pointer;
    width: 100%;
    font-size: 1rem;
}
.btn-primary.full-width {
    display: block;
    text-decoration: none;
}

.btn-primary:disabled { opacity: 0.7; cursor: not-allowed; }

.btn-link {
    background: none;
    border: none;
    color: var(--text-secondary);
    text-decoration: none;
    font-size: 0.9rem;
    font-weight: 600;
    margin: 0 auto;
    cursor: pointer;
}
.btn-link:hover { color: var(--color-primary); }

.success-state {
    padding: 20px 0;
}

.success-icon {
    width: 60px; height: 60px;
    background: var(--bg-status-success);
    color: var(--status-success-text);
    border-radius: 50%;
    display: flex; align-items: center; justify-content: center;
    font-size: 2rem;
    margin: 0 auto 20px;
}

.instruction {
    background: var(--bg-input);
    padding: 15px;
    border-radius: 0;
    margin: 20px 0;
    font-size: 0.9rem;
    color: var(--text-primary);
    border-left: 4px solid var(--color-primary);
    text-align: left;
}
</style>

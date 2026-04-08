<template>

  <div class="profile-page">
    <header class="page-header">
      <h1>Profile Settings</h1>
      <p>Manage your personal information and account security.</p>
    </header>

    <template v-if="!dataLoaded">
      <div class="skeleton-container">
        <Skeleton width="100%" height="400px" />
        <Skeleton width="100%" height="200px" />
      </div>
    </template>

    <div v-else class="profile-grid">
      <section class="settings-card" :class="{ 'editing-mode': isEditingProfile }">
        <div class="card-header">
          <div class="header-icon">👤</div>
          <div class="header-text">
            <h2>Personal Information</h2>
            <p>This info is used for your medical records and bookings.</p>
          </div>
          <button @click="isEditingProfile = !isEditingProfile" class="btn-edit-toggle">
            {{ isEditingProfile ? 'Cancel' : 'Edit' }}
          </button>
        </div>

        <form @submit.prevent="updateProfile" class="settings-form">
          <div class="form-row">
            <div class="form-group">
              <label>First Name</label>
              <input 
                v-model="profile.firstName" 
                type="text" 
                placeholder="John" 
                :readonly="!isEditingProfile"
                :class="{ 'read-only-field': !isEditingProfile }"
                required 
              />
            </div>
            <div class="form-group">
              <label>Last Name</label>
              <input 
                v-model="profile.lastName" 
                type="text" 
                placeholder="Doe" 
                :readonly="!isEditingProfile"
                :class="{ 'read-only-field': !isEditingProfile }"
                required 
              />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Phone Number</label>
              <input 
                v-model="profile.phone" 
                type="tel" 
                placeholder="+1 (555) 000-0000" 
                :readonly="!isEditingProfile"
                :class="{ 'read-only-field': !isEditingProfile }"
                required 
              />
            </div>
            <div class="form-group">
              <label>Date of Birth</label>
              <input 
                v-model="profile.dateOfBirth" 
                type="date" 
                :readonly="!isEditingProfile"
                :class="{ 'read-only-field': !isEditingProfile }"
                required 
              />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Gender</label>
              <select 
                v-model="profile.gender" 
                :disabled="!isEditingProfile"
                :class="{ 'read-only-field': !isEditingProfile }"
              >
                <option value="Male">Male</option>
                <option value="Female">Female</option>
              </select>
            </div>
            <div class="form-group">
              <label>Age (Auto-calculated)</label>
              <input :value="calculatedAge" type="text" disabled class="disabled-input" />
            </div>
          </div>

          <div class="card-footer" v-if="isEditingProfile">
            <button type="submit" :disabled="loading" class="btn-primary">
              {{ loading ? 'Saving...' : 'Save Changes' }}
            </button>
          </div>
        </form>
      </section>

      <section class="settings-card security-card">
        <div class="card-header">
          <div class="header-icon">🔒</div>
          <div>
            <h2>Security</h2>
            <p>Update your password to keep your account safe.</p>
          </div>
        </div>

        <form @submit.prevent="updatePassword" class="settings-form">
          <div class="form-group">
            <label>Current Password</label>
            <input v-model="security.oldPassword" type="password" required />
          </div>
          
          <div class="form-row">
            <div class="form-group">
              <label>New Password</label>
              <input v-model="security.newPassword" type="password" required />
            </div>
            <div class="form-group">
              <label>Confirm New Password</label>
              <input v-model="security.confirmPassword" type="password" required />
            </div>
          </div>

          <div class="card-footer">
            <button type="submit" :disabled="secLoading" class="btn-outline">
              Change Password
            </button>
          </div>
        </form>
      </section>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'
import Skeleton from '@/components/Skeleton.vue'

const auth = useAuthStore()
const alert = useAlertStore()

const loading = ref(false)
const secLoading = ref(false)
const dataLoaded = ref(false)
const isEditingProfile = ref(false)

const profile = reactive({
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  gender: 'Male',
  dateOfBirth: ''
})

const security = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const fetchProfile = async () => {
  try {
    dataLoaded.value = false
    const res = await axios.get('/api/profile', {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    const data = res.data
    profile.firstName = data.firstName || ''
    profile.lastName = data.lastName || ''
    profile.email = data.email || ''
    profile.phone = data.phone || ''
    profile.gender = data.gender || 'Male'
    
    // Handle potential date format and casing issues
    const rawDob = data.dateOfBirth || data.DateOfBirth
    if (rawDob) {
      profile.dateOfBirth = rawDob.split('T')[0]
    } else {
      profile.dateOfBirth = ''
    }
  } catch (err) {
    console.error("Failed to fetch profile", err)
  } finally {
    dataLoaded.value = true
  }
}

onMounted(fetchProfile)

const calculatedAge = computed(() => {
  if (!profile.dateOfBirth) return '--'
  
  // Parse date manually to avoid timezone issues with new Date(string)
  const parts = profile.dateOfBirth.split('T')[0].split('-')
  if (parts.length !== 3) return '--'
  
  const year = parseInt(parts[0])
  const month = parseInt(parts[1]) - 1
  const day = parseInt(parts[2])
  
  const birthDate = new Date(year, month, day)
  if (isNaN(birthDate.getTime())) return '--'
  
  const today = new Date()
  let age = today.getFullYear() - birthDate.getFullYear()
  const m = today.getMonth() - birthDate.getMonth()
  
  if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
    age--
  }
  
  return age >= 0 ? `${age} years` : '0 years'
})

const updateProfile = async () => {
  loading.value = true
  try {
    const dto = {
      firstName: profile.firstName,
      lastName: profile.lastName,
      phone: profile.phone,
      gender: profile.gender,
      dateOfBirth: profile.dateOfBirth
    }
    await axios.put('/api/profile', dto, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    isEditingProfile.value = false
    alert.showAlert('Profile updated successfully!', 'success')
  } catch (err) {
    alert.showAlert('Failed to update profile: ' + (err.response?.data?.Message || err.message), 'error')
  } finally {
    loading.value = false
  }
}

const updatePassword = async () => {
  if (security.newPassword !== security.confirmPassword) {
    return alert.showAlert("New passwords do not match!", "error")
  }
  secLoading.value = true
  try {
    await axios.post('/api/profile/change-password', {
      current: security.oldPassword,
      new: security.newPassword
    }, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    alert.showAlert("Password changed successfully!", "success")
    security.oldPassword = security.newPassword = security.confirmPassword = ''
  } catch (err) {
    alert.showAlert("Error updating password: " + (err.response?.data?.Message || err.message), "error")
  } finally {
    secLoading.value = false
  }
}
</script>

<style scoped>
.profile-page {
  max-width: 1000px;
  margin: 0 auto;
  padding: 3rem 1.5rem;
  font-family: inherit;
  background: var(--bg-body);
  min-height: 100vh;
}

.page-header { margin-bottom: 2.5rem; }
.page-header h1 { font-size: 2.2rem; color: var(--text-primary); margin: 0; }
.page-header p { color: var(--text-secondary); margin-top: 0.5rem; }

.profile-grid {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.settings-card {
  background: var(--bg-card);
  border-radius: 16px;
  border: 1px solid var(--border-default);
  overflow: hidden;
  box-shadow: var(--shadow-sm);
}

.card-header {
  padding: 1.5rem;
  border-bottom: 1px solid var(--border-default);
  display: flex;
  gap: 1.25rem;
  align-items: center;
}

.header-icon {
  width: 48px; height: 48px; background: var(--bg-input);
  display: grid; place-items: center; border-radius: 12px; font-size: 1.5rem;
}

.card-header h2 { font-size: 1.25rem; margin: 0; color: var(--text-primary); }
.card-header p { font-size: 0.9rem; margin: 0; color: var(--text-secondary); }
.header-text { flex: 1; }

.btn-edit-toggle {
  padding: 8px 16px;
  border-radius: 8px;
  border: 1px solid var(--color-primary);
  background: transparent;
  color: var(--color-primary);
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-edit-toggle:hover {
  background: var(--color-primary);
  color: white;
}

.read-only-field {
  background-color: var(--bg-input) !important;
  color: var(--text-secondary) !important;
  border-color: transparent !important;
  cursor: default !important;
  border-style: dashed !important;
  border-width: 1px !important;
  border-color: var(--border-default) !important;
}

.skeleton-container {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.settings-form { padding: 2rem; }

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
  padding-top: 15px;
  margin-bottom: 1.5rem;
}

.form-group { display: flex; flex-direction: column; gap: 0.5rem; }

label { font-size: 0.875rem; font-weight: 600; color: var(--text-tertiary); }

input, select {
  padding: 0.75rem; border: 1px solid var(--border-input); border-radius: 8px;
  font-size: 1rem; color: var(--text-primary); transition: border-color 0.2s;
}

input:focus { outline: none; border-color: var(--color-primary); }

.disabled-input { background: var(--bg-input); color: var(--text-tertiary); cursor: not-allowed; }

.card-footer {
  margin-top: 1rem;
  display: flex;
  justify-content: flex-end;
}

.btn-primary {
  background: var(--color-primary); color: var(--color-white); border: none; padding: 0.75rem 2rem;
  border-radius: 8px; font-weight: 600; cursor: pointer; transition: background 0.2s;
}

.btn-primary:hover { background: var(--color-primary-hover); }

.btn-outline {
  background: transparent; border: 1px solid var(--border-default); color: var(--text-primary);
  padding: 0.75rem 2rem; border-radius: 8px; font-weight: 600; cursor: pointer;
}

.btn-outline:hover { background: var(--bg-input); border-color: var(--border-input); }

@media (max-width: 768px) {
  .form-row { grid-template-columns: 1fr; }
}
</style>
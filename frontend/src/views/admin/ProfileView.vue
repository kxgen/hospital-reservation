<template>
  <div class="profile-page">
    <div class="page-header">
      <div class="header-content">
        <h1>Profile <span>Settings</span></h1>
        <p>Manage your administrative profile and security credentials.</p>
      </div>
    </div>

    <template v-if="!dataLoaded">
      <div class="skeleton-container">
        <Skeleton width="100%" height="300px" />
        <Skeleton width="100%" height="200px" />
      </div>
    </template>

    <div v-else class="profile-grid">
      <section class="settings-card" :class="{ 'editing-mode': isEditingProfile }">
        <div class="card-header">
          <div class="header-icon">⚙️</div>
          <div class="header-text">
            <h2>Administrator Information</h2>
            <p>Your details are used for internal administrative tracking.</p>
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
                placeholder="Admin" 
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
                placeholder="User" 
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
              />
            </div>
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
          </div>

          <div class="form-group">
             <label>Email Address (Read Only)</label>
             <input v-model="profile.email" type="email" disabled class="disabled-input" />
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
            <p>Update your password to keep your administrative account safe.</p>
          </div>
        </div>

        <form @submit.prevent="updatePassword" class="settings-form">
          <div class="form-group">
            <label>Current Password</label>
            <input v-model="security.current" type="password" required autocomplete="current-password" />
          </div>
          
          <div class="form-row">
            <div class="form-group">
              <label>New Password</label>
              <input v-model="security.new" type="password" required autocomplete="new-password" />
            </div>
            <div class="form-group">
              <label>Confirm New Password</label>
              <input v-model="security.confirm" type="password" required autocomplete="new-password" />
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
import { ref, reactive, onMounted } from 'vue'
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
  gender: 'Male'
})

const security = reactive({
  current: '',
  new: '',
  confirm: ''
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
  } catch (err) {
    console.error("Failed to fetch admin profile", err)
  } finally {
    dataLoaded.value = true
  }
}

onMounted(fetchProfile)

const updateProfile = async () => {
  loading.value = true
  try {
    const dto = {
      firstName: profile.firstName,
      lastName: profile.lastName,
      phone: profile.phone,
      gender: profile.gender
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
  if (security.new !== security.confirm) {
    return alert.showAlert("New passwords do not match!", "error")
  }
  secLoading.value = true
  try {
    await axios.post('/api/profile/change-password', {
      current: security.current,
      new: security.new
    }, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    alert.showAlert("Password changed successfully!", "success")
    security.current = security.new = security.confirm = ''
  } catch (err) {
    alert.showAlert("Error updating password: " + (err.response?.data?.Message || err.message), "error")
  } finally {
    secLoading.value = false
  }
}
</script>

<style scoped>
.profile-page { animation: fadeIn 0.4s ease-out; }

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.profile-grid {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.settings-card {
  background: var(--bg-card);
  border-radius: 0;
  padding: 30px;
  border: 1px solid var(--border-default);
  overflow: hidden;
  box-shadow: var(--shadow-sm);
}

.settings-card h3 {
  font-size: 1.2rem;
  font-weight: 800;
  color: var(--text-primary);
  margin-bottom: 25px;
  display: flex;
  align-items: center;
  gap: 12px;
}

.card-header {
  padding: 1.5rem;
  border-bottom: 1px solid var(--bg-input);
  display: flex;
  gap: 1.25rem;
  align-items: center;
}

.header-icon {
  width: 48px; height: 48px; background: var(--bg-input);
  display: grid; place-items: center; border-radius: 0; font-size: 1.5rem;
}

.card-header h2 { font-size: 1.25rem; margin: 0; color: var(--text-primary); }
.card-header p { font-size: 0.9rem; margin: 0; color: var(--text-secondary); }
.header-text { flex: 1; }

.btn-edit-toggle {
  padding: 8px 16px;
  border-radius: 0;
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
  color: var(--text-tertiary) !important;
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

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 25px;
}

.info-item label {
  display: block;
  font-size: 0.75rem;
  font-weight: 800;
  color: var(--text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 8px;
}

.form-group { display: flex; flex-direction: column; gap: 0.5rem; }

label { font-size: 0.875rem; font-weight: 600; color: var(--text-secondary); }

input, select {
  padding: 0.75rem; border: 1px solid var(--border-default); border-radius: 0;
  font-size: 1rem; color: var(--text-primary); transition: border-color 0.2s;
}

input:focus, select:focus { outline: none; border-color: var(--color-primary); }

.disabled-input { background: var(--bg-input); color: var(--text-tertiary); cursor: not-allowed; }

.card-footer {
  margin-top: 1rem;
  display: flex;
  justify-content: flex-end;
}

.btn-primary {
  background: var(--color-primary); color: var(--color-white); border: none; padding: 0.75rem 2rem;
  border-radius: 0; font-weight: 600; cursor: pointer; transition: background 0.2s;
}

.btn-primary:hover { background: var(--color-primary-hover); }

.btn-outline {
  background: transparent; border: 1px solid var(--border-default); color: var(--color-gray-700);
  padding: 0.75rem 2rem; border-radius: 0; font-weight: 600; cursor: pointer;
}

.btn-outline:hover { background: var(--bg-input); border-color: var(--border-default); }

@media (max-width: 768px) {
  .form-row { grid-template-columns: 1fr; }
}
</style>
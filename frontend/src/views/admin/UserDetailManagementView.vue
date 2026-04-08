<template>
  <div class="admin-container">
    <div class="page-header">
      <div class="header-content">
        <button class="back-btn" @click="$router.push('/admin/accounts')">← User Management</button>
        <div v-if="user" class="user-header">
          <div class="user-main-info">
              <div class="avatar-lg">{{ user.firstName?.[0] }}{{ user.lastName?.[0] }}</div>
              <div class="meta">
                  <h1>{{ user.firstName }} {{ user.lastName }}</h1>
                  <span :class="['role-badge', user.role]">{{ user.role || 'Patient' }}</span>
              </div>
          </div>
          <div class="status-indicator">
              <span :class="['status-pill', (user.isSuspended || user.isDisabled) ? 'status-error' : 'status-active']">
                  {{ (user.isSuspended || user.isDisabled) ? 'Suspended' : 'Active' }}
              </span>
          </div>
        </div>
      </div>
    </div>

    <div v-if="loading" class="loader">Loading user profile...</div>
    
    <div v-else-if="user" class="management-grid">
      <!-- 1. Profile Information Edit -->
      <section class="management-card">
        <div class="card-header">
          <h3>Personal Details</h3>
          <p>Update basic account information and contact data.</p>
        </div>
        <form @submit.prevent="updateProfile" class="management-form">
          <div class="form-row">
            <div class="form-group">
              <label>First Name</label>
              <input type="text" v-model="form.firstName" class="styled-input" />
            </div>
            <div class="form-group">
              <label>Last Name</label>
              <input type="text" v-model="form.lastName" class="styled-input" />
            </div>
          </div>
          <div class="form-group">
            <label>Email Address</label>
            <input type="email" v-model="form.email" class="styled-input" disabled />
          </div>
          <div v-if="user.role === 'doctor'" class="form-group">
            <label>Medical Specialty</label>
            <select v-model="form.specialtyId" class="styled-input">
              <option disabled value="">Select Specialty</option>
              <option v-for="s in specialties" :key="s.id" :value="s.id">
                {{ s.name }}
              </option>
            </select>
          </div>
          <div class="form-group">
            <label>Phone Number</label>
            <input type="text" v-model="form.phone" class="styled-input" placeholder="e.g. +123456789" />
          </div>

          <div v-if="isPatient" class="form-row">
            <div class="form-group">
              <label>Gender</label>
              <select v-model="form.gender" class="styled-input">
                <option value="Male">Male</option>
                <option value="Female">Female</option>
              </select>
            </div>
            <div class="form-group">
              <label>Date of Birth</label>
              <input type="date" v-model="form.dateOfBirth" class="styled-input" />
            </div>
            <div class="form-group">
              <label>Calculated Age</label>
              <input type="text" :value="calculatedAge" class="styled-input disabled-input" disabled />
            </div>
          </div>

          <button type="submit" class="btn-save" :disabled="saving">
            {{ saving ? 'Saving...' : 'Update Profile' }}
          </button>
        </form>
      </section>

      <!-- 2. Administrative Actions -->
      <section class="management-card actions-card">
         <div class="card-header">
          <h3>Administrative Actions</h3>
          <p>Role-based system controls and permissions.</p>
        </div>
        
        <div class="action-list">
            <!-- Doctor Only -->
            <div v-if="user.role === 'doctor'" class="action-item">
                <div class="action-meta">
                    <strong>Shift & Availability</strong>
                    <p>Configure weekly working hours and leave days.</p>
                </div>
                <button class="btn-outline-action" @click="$router.push(`/admin/doctors/${user.id}/availability`)">
                    Manage Schedule
                </button>
            </div>

            <!-- All Staff/Patients -->
            <div class="action-item">
                <div class="action-meta">
                    <strong>Account Permission</strong>
                    <p>{{ (user.isSuspended || user.isDisabled) ? 'Allow this user to access the portal again.' : 'Restrict this user from logging into the system.' }}</p>
                </div>
                <button 
                    :class="['btn-status-toggle', (user.isSuspended || user.isDisabled) ? 'activate' : 'suspend']"
                    @click="toggleSuspension"
                >
                    {{ (user.isSuspended || user.isDisabled) ? 'Activate Member' : 'Suspend Member' }}
                </button>
            </div>

            <div class="action-item danger-zone">
                <div class="action-meta">
                    <strong>Security Reset</strong>
                    <p>Issue a temporary password for the user.</p>
                </div>
                <button class="btn-outline-danger" @click="showResetModal = true">Reset Password</button>
            </div>
        </div>
      </section>
    </div>

    <!-- Stylized Password Reset Modal -->
    <ResetPasswordModal 
      :is-open="showResetModal"
      :user-name="user?.firstName + ' ' + user?.lastName"
      :is-saving="resettingPassword"
      @close="showResetModal = false"
      @confirm="handleResetPassword"
    />
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import axios from '@/api/axios'
import { useAlertStore } from '@/stores/alertStore'
import ResetPasswordModal from '@/components/admin/ResetPasswordModal.vue'

const props = defineProps({
    id: { type: [String, Number], required: true },
    role: { type: String, required: false }
})

const route = useRoute()
const alertStore = useAlertStore()
const user = ref(null)
const loading = ref(true)
const saving = ref(false)
const resettingPassword = ref(false)
const showResetModal = ref(false)
const form = ref({ firstName: '', lastName: '', specialtyId: '', email: '', phone: '', bio: '' })
const specialties = ref([])

const loadSpecialties = async () => {
    try {
        const res = await axios.get("/api/doctors/specialties");
        specialties.value = res.data;
    } catch (err) {
        console.error("Failed to load specialties", err);
    }
}

// Check if it's a patient based on the role prop or route path fallback
const isPatient = computed(() => props.role === 'patient' || route.path.includes('/patients/'))

const calculatedAge = computed(() => {
  if (!form.value.dateOfBirth) return '--'
  const parts = form.value.dateOfBirth.split('T')[0].split('-')
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

const fetchUser = async () => {
    try {
        const token = localStorage.getItem('token')
        let endpoint = ""
        if (isPatient.value) {
            endpoint = `/api/admin/patients/${props.id}`
        } else {
            endpoint = `/api/admin/staff/${props.id}`
        }
        
        const res = await axios.get(endpoint, {
            headers: { Authorization: `Bearer ${token}` }
        })
        
        if (isPatient.value) {
            user.value = {
                id: res.data.patientId || res.data.PatientId,
                firstName: res.data.firstName || res.data.FirstName,
                lastName: res.data.lastName || res.data.LastName,
                email: res.data.email || res.data.Email,
                phone: res.data.phone || res.data.Phone,
                gender: res.data.gender || res.data.Gender,
                dateOfBirth: (res.data.dateOfBirth || res.data.DateOfBirth)?.split('T')[0],
                isDisabled: res.data.isDisabled !== undefined ? res.data.isDisabled : (res.data.IsDisabled !== undefined ? res.data.IsDisabled : false),
                role: 'patient'
            }
        } else {
            user.value = {
                id: res.data.id || res.data.Id,
                firstName: res.data.firstName || res.data.FirstName,
                lastName: res.data.lastName || res.data.LastName,
                email: res.data.email || res.data.Email,
                phone: res.data.phone || res.data.Phone,
                role: (res.data.role || res.data.Role)?.toLowerCase(),
                isSuspended: res.data.isSuspended !== undefined ? res.data.isSuspended : (res.data.IsSuspended !== undefined ? res.data.IsSuspended : false),
                bio: res.data.bio || res.data.Bio,
                specialtyId: res.data.specialtyId || res.data.SpecialtyId
            }
        }
        
        form.value = { ...user.value }
    } catch (err) {
        alertStore.showAlert('Error loading user details', 'error')
    } finally {
        loading.value = false
    }
}

const updateProfile = async () => {
    saving.value = true
    try {
        const token = localStorage.getItem('token')
        let endpoint = ""
        if (isPatient.value) {
            endpoint = `/api/admin/patients/${user.value.id}`
        } else {
            endpoint = `/api/admin/staff/${user.value.id}`
        }

        const payload = isPatient.value ? {
            PatientId: form.value.id,
            FirstName: form.value.firstName,
            LastName: form.value.lastName,
            Email: form.value.email,
            Phone: form.value.phone,
            Gender: form.value.gender,
            DateOfBirth: form.value.dateOfBirth
        } : {
            Id: form.value.id,
            FirstName: form.value.firstName,
            LastName: form.value.lastName,
            Email: form.value.email,
            Phone: form.value.phone,
            Role: form.value.role,
            Bio: form.value.bio,
            SpecialtyId: parseInt(form.value.specialtyId) || 0,
            IsSuspended: form.value.isSuspended
        }

        await axios.put(endpoint, payload, {
            headers: { Authorization: `Bearer ${token}` }
        })
        alertStore.showAlert('Profile updated successfully', 'success')
        fetchUser()
    } catch (err) {
        console.error('Update failed:', err)
        alertStore.showAlert('Update failed: ' + (err.response?.data?.Message || 'Unknown error'), 'error')
    } finally {
        saving.value = false
    }
}

const toggleSuspension = async () => {
     try {
        const token = localStorage.getItem('token')
        let endpoint = ""
        if (isPatient.value) {
            endpoint = `/api/admin/patients/${user.value.id}/toggle-disable`
        } else {
            endpoint = `/api/admin/staff/${user.value.id}/toggle-status`
        }

        await axios.post(endpoint, {}, {
            headers: { Authorization: `Bearer ${token}` }
        })
        
        if (isPatient.value) user.value.isDisabled = !user.value.isDisabled
        else user.value.isSuspended = !user.value.isSuspended
        
        alertStore.showAlert('Account status updated', 'success')
    } catch (err) {
        alertStore.showAlert('Action failed', 'error')
    }
}

const handleResetPassword = async (tempPassword) => {
    resettingPassword.value = true
    try {
        const token = localStorage.getItem('token')
        let endpoint = ""
        if (isPatient.value) {
            endpoint = `/api/admin/patients/${user.value.id}/reset-password`
        } else {
            endpoint = `/api/admin/staff/${user.value.id}/reset-password`
        }

        await axios.post(endpoint, {
            password: tempPassword
        }, {
            headers: { Authorization: `Bearer ${token}` }
        })
        alertStore.showAlert(`Password reset successful for ${user.value.firstName}.`, 'success')
        showResetModal.value = false
    } catch (err) {
        alertStore.showAlert('Reset failed: ' + (err.response?.data?.Message || 'Unknown error'), 'error')
    } finally {
        resettingPassword.value = false
    }
}

onMounted(() => {
    fetchUser()
    loadSpecialties()
})
</script>

<style scoped>
.admin-container { animation: fadeIn 0.4s ease-out; }

input, select, textarea {
  width: 100%; 
  padding: 12px; 
  border: 2px solid var(--border-input); 
  border-radius: 10px; 
  font-family: inherit;
  font-size: var(--font-size-base);
  box-sizing: border-box; /* This is the critical line */
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.back-btn {
    background: none; border: none; color: var(--color-primary);
    font-weight: 700; cursor: pointer; margin-bottom: 20px;
    padding: 0; font-size: 0.95rem;
}

.user-header {
    display: flex; justify-content: space-between; align-items: center;
    background: var(--bg-card); padding: 30px; border-radius: 12px;
    border: 1px solid var(--border-default); margin-bottom: 30px;
}

.user-main-info { display: flex; align-items: center; gap: 20px; }
.avatar-lg {
    width: 64px; height: 64px; background: var(--color-primary-light);
    color: var(--color-primary); border-radius: 50%; display: flex;
    align-items: center; justify-content: center; font-size: 1.5rem; font-weight: 800;
}

.meta { padding-right: 10px;}
.meta h1 { margin: 0; font-size: 1.5rem; color: var(--text-primary); }
.role-badge {
    display: inline-block; padding: 4px 0; border-radius: 6px;
    font-size: 0.75rem; font-weight: 800; text-transform: uppercase;
    background: var(--bg-input); color: var(--text-secondary);
}

.status-pill {
    padding: 8px 16px; border-radius: 30px; font-size: 0.8rem; font-weight: 800;
}
.status-pill.active { background: var(--bg-status-success); color: var(--status-success-text); }
.status-pill.suspended { background: var(--bg-status-error); color: var(--status-error-text); }

.management-grid { 
  display: grid; 
  grid-template-columns: 1.2fr 0.8fr; 
  gap: 30px; 
  align-items: start;
}

.management-card {
    background: var(--bg-card); border-radius: 20px; padding: 30px;
    border: 1px solid var(--border-default); height: fit-content;
}

.card-header h3 { margin: 0; font-size: 1.2rem; color: var(--text-primary); }
.card-header p { margin: 5px 0 25px; font-size: 0.9rem; color: var(--text-secondary); }

.form-group { margin-bottom: 20px; }
.form-group label { display: block; font-size: var(--font-size-small); font-weight: 700; color: var(--text-tertiary); margin-bottom: 8px; text-transform: uppercase; }

.styled-input {
    width: 100%; padding: 12px; border: 1.5px solid var(--border-input);
    border-radius: 10px; font-size: var(--font-size-base); color: var(--text-primary); transition: 0.2s;
}
.styled-input:focus { outline: none; border-color: var(--color-primary); box-shadow: 0 0 0 3px var(--color-primary-glow); }

.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 25px; }

.btn-save {
    width: 100%; background: var(--color-primary); color: white; border: none;
    padding: 14px; border-radius: 12px; font-weight: 800; cursor: pointer; margin-top: 10px;
}

.action-list { display: flex; flex-direction: column; gap: 25px; }
.action-item { display: flex; justify-content: space-between; align-items: flex-start; gap: 15px; }
.action-meta strong { display: block; font-size: 0.95rem; color: var(--text-primary); }
.action-meta p { margin: 4px 0 0; font-size: 0.85rem; color: var(--text-secondary); line-height: 1.4; }

.btn-outline-action {
    background: var(--bg-body); border: 1.5px solid var(--border-default);
    padding: 8px 15px; border-radius: 8px; font-weight: 700; cursor: pointer; white-space: nowrap;
}

.btn-status-toggle {
    padding: 8px 15px; border-radius: 8px; font-weight: 700; cursor: pointer; white-space: nowrap; border: none;
}
.btn-status-toggle.suspend { background: var(--bg-status-error); color: var(--status-error-text); }
.btn-status-toggle.activate { background: var(--bg-status-success); color: var(--status-success-text); }

.btn-outline-danger {
    background: none; border: 1.5px solid var(--border-default); color: var(--color-error);
    padding: 8px 15px; border-radius: 0; font-weight: 700; cursor: pointer;
}

.loader { text-align: center; padding: 100px; color: var(--text-tertiary); font-style: italic; }

@media (max-width: 1024px) {
    .management-grid { grid-template-columns: 1fr; }
    .user-header { flex-direction: column; gap: 20px; text-align: center; }
    .user-main-info { flex-direction: column; gap: 10px; }
    .status-pill { width: 100%; display: block; text-align: center; }
}

@media (max-width: 600px) {
    .form-row { grid-template-columns: 1fr; gap: 15px; } /* Increased vertical gap when stacked */
    .action-item { flex-direction: column; gap: 10px; }
    .btn-outline-action, .btn-status-toggle, .btn-outline-danger { width: 100%; }
}
</style>

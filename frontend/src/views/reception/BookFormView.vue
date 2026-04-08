<template>
  <div class="booking-flow">
    <!-- Progress Indicator -->
    <div class="progress-container wrapper">
      <div class="progress-bar">
        <div class="step active"><span>1</span><label>Specialist</label></div>
        <div class="progress-line filled"></div>
        <div class="step active"><span>2</span><label>Patient Details</label></div>
        <div class="progress-line" :class="{ filled: successMessage }"></div>
        <div class="step" :class="{ active: successMessage }"><span>3</span><label>Success</label></div>
      </div>
    </div>

    <main class="booking-main wrapper">
      <header class="booking-header">
        <button class="btn-back" @click="goBack">
          <span class="icon">←</span> Back to Selection
        </button>
        <h1>Reception <span>Booking</span></h1>
        <p>Registering an appointment for a patient visit.</p>
      </header>

      <div class="layout-grid">
        <!-- Left: Summary -->
        <div class="details-section">
          <div class="card summary-card shadow">
            <div class="card-title">Appointment Summary</div>
            
            <div class="doctor-profile-mini">
              <div class="mini-avatar">{{ doctorName ? doctorName[0] : 'D' }}</div>
              <div class="profile-info">
                <h3>{{ doctorName }}</h3>
                <span>{{ specialty }}</span>
              </div>
            </div>

            <div class="appointment-details">
              <div class="detail-item">
                <span class="detail-icon">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
                </span>
                <div class="detail-text">
                  <label>Scheduled Date & Time</label>
                  <p>{{ selectedTimeLabel }}</p>
                </div>
              </div>

            </div>
          </div>
        </div>

        <!-- Right: Form -->
        <div class="form-section">
          <div class="card form-card shadow">
            <div class="card-title">Patient Identification</div>
            
            <div v-if="successMessage" class="success-banner animate-in">
              <span class="icon">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
              </span>
              <div class="success-text">
                <h3>Appointment Registered</h3>
                <p>Redirecting to dashboard...</p>
              </div>
            </div>

            <form v-else @submit.prevent="requestBooking" class="booking-form">
              <div class="form-row">
                <div class="input-group">
                  <label>First Name</label>
                  <input v-model="form.FirstName" type="text" required placeholder="Abebe" />
                </div>
                <div class="input-group">
                  <label>Last Name</label>
                  <input v-model="form.LastName" type="text" required placeholder="Kebede" />
                </div>
              </div>

              <div class="form-row">
                <div class="input-group">
                  <label>Phone Number</label>
                  <input v-model="form.Phone" type="tel" required placeholder="+251..." />
                </div>
                <div class="input-group">
                  <label>Gender</label>
                  <select v-model="form.Gender">
                    <option value="male">Male</option>
                    <option value="female">Female</option>
                  </select>
                </div>
              </div>

              <div class="input-group">
                <label>Date of Birth</label>
                <input v-model="form.DateOfBirth" type="date" />
              </div>

              <div class="input-group">
                <label>Reason for Visit</label>
                <textarea 
                  v-model="form.Reason" 
                  placeholder="Describe the clinical reason for this visit..."
                  required
                ></textarea>
              </div>

              <button type="submit" class="btn-confirm" :disabled="isSubmitting">
                <span v-if="isSubmitting" class="spinner"></span>
                {{ isSubmitting ? 'Processing...' : 'Register Appointment' }}
              </button>
            </form>
          </div>
        </div>
      </div>
      
      <ConfirmationModal
        :visible="confirmVisible"
        title="Confirm Booking"
        :message="`Are you sure you want to schedule an appointment for patient <b>${form.FirstName} ${form.LastName}</b>?`"
        type="info"
        confirmText="Yes, Book It"
        @close="confirmVisible = false"
        @confirm="confirmBooking"
      />

      <ConfirmationModal
        :visible="linkModalVisible"
        title="Patient Record Found"
        :message="`Existing patient found: <b>${foundPatient?.firstName || foundPatient?.FirstName} ${foundPatient?.lastName || foundPatient?.LastName}</b><br/>Gender: ${foundPatient?.gender || foundPatient?.Gender}<br/><br/>Do you want to link this appointment to their existing profile?`"
        type="warning"
        confirmText="Yes, Link Profile"
        cancelText="Change Phone Number"
        @close="cancelLink"
        @confirm="confirmLink"
      />
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'
import ConfirmationModal from '@/components/ConfirmationModal.vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const alerts = useAlertStore()

const doctorId = Number(route.query.doctorId)
const doctorName = route.query.doctorName || ''
const specialty = route.query.specialty || ''
const selectedStartTime = ref(route.query.startTime)
const selectedEndTime = ref(route.query.endTime)
const selectedTimeLabel = ref(route.query.timeLabel)

const form = ref({
  FirstName: '',
  LastName: '',
  Phone: '',
  Gender: 'male',
  DateOfBirth: '',
  Reason: ''
})

// UI State
const errorMessage = ref('')
const successMessage = ref('')
const isSubmitting = ref(false)

onMounted(() => {
  if (!doctorId || !selectedStartTime.value) {
    router.replace('/reception/appointments')
  }
})

const goBack = () => router.push('/reception/appointments')

const confirmVisible = ref(false)

const requestBooking = async () => {
    // Reset state first
    selectedPatientId.value = null;
    foundPatient.value = null;

    if(!form.value.Phone || form.value.Phone.length < 9) {
        confirmVisible.value = true;
        return;
    }

    try {
        const res = await axios.get(`/api/receptionist/search-patient?phone=${encodeURIComponent(form.value.Phone)}`, {
             headers: { Authorization: `Bearer ${auth.token}` }
        })
        if(res.data) {
             foundPatient.value = res.data
             linkModalVisible.value = true
             return; // Wait for user choice
        }
    } catch (e) {
        // Not found is fine, proceed to confirm
    }

    confirmVisible.value = true;
}

const selectedPatientId = ref(null)
const foundPatient = ref(null)
const linkModalVisible = ref(false)

// checkPatient removed, logic moved to requestBooking

const confirmLink = () => {
    if(foundPatient.value) {
        // Handle potential camelCase from API
        const p = foundPatient.value
        selectedPatientId.value = p.patientId || p.PatientId
        form.value.FirstName = p.firstName || p.FirstName
        form.value.LastName = p.lastName || p.LastName
        form.value.Gender = p.gender || p.Gender || 'male'
        const dob = p.dateOfBirth || p.DateOfBirth
        if(dob) {
            form.value.DateOfBirth = dob.split('T')[0]
        }
    }
    linkModalVisible.value = false
    alerts.showAlert("Linked to existing patient profile.", "info")
    confirmVisible.value = true; // Proceed to booking confirmation
}

const cancelLink = () => {
    selectedPatientId.value = null
    linkModalVisible.value = false
    // Do NOT proceed to confirmVisible.value = true
    // This forces the user to either Link or Change the phone number.
    alerts.showAlert("Please link to the existing profile or use a different phone number.", "warning")
}

const confirmBooking = async () => {
  confirmVisible.value = false;
  if (isSubmitting.value) return
  
  isSubmitting.value = true

  try {
    const payload = {
      ...form.value,
      DoctorId: doctorId,
      StartTime: selectedStartTime.value,
      EndTime: selectedEndTime.value,
      TimeSlotId: null,
      PatientId: selectedPatientId.value
    }

    await axios.post('/api/receptionist/book', payload, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })

    alerts.showAlert("Appointment registered successfully!", "success")
    successMessage.value = "Success!" // To trigger the success banner in template if needed or just use alert
    setTimeout(() => {
      router.push('/reception/dashboard')
    }, 2000)

  } catch (err) {
    console.error('Reception Booking Error:', err)
    const msg = err.response?.data?.Message || err.response?.data || "Failed to register appointment."
    alerts.showAlert(msg, "error")
  } finally {
    isSubmitting.value = false
  }
}
</script>

<style scoped>
.booking-flow {
  background: var(--bg-body);
  min-height: 100vh;
  padding-bottom: 80px;
}

.wrapper {
  max-width: 1100px;
  margin: 0 auto;
  padding: 0 20px;
}

/* Progress Stepper */
.progress-container { padding: 40px 0 20px; }
.progress-bar { display: flex; align-items: center; justify-content: space-between; max-width: 500px; margin: 0 auto; }
.step { display: flex; flex-direction: column; align-items: center; gap: 8px; position: relative; z-index: 2; }
.step span {
  width: 32px; height: 32px; border-radius: 0; background: white; border: 2px solid var(--border-default);
  display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 0.9rem; color: var(--text-secondary);
}
.step label { font-size: 0.75rem; font-weight: 700; color: var(--text-secondary); text-transform: uppercase; }
.step.active span { background: var(--color-primary); border-color: var(--color-primary); color: white; }
.step.active label { color: var(--color-primary); }
.progress-line { flex: 1; height: 3px; background: var(--border-default); margin: 0 -10px; transform: translateY(-12px); z-index: 1; }
.progress-line.filled { background: var(--color-primary); }

/* Header */
.booking-header { margin-bottom: 40px; }
.btn-back { background: none; border: none; color: var(--color-primary); font-weight: 700; cursor: pointer; display: flex; align-items: center; gap: 8px; margin-bottom: 15px; }
.booking-header h1 { font-size: 2.2rem; font-weight: 800; margin: 0; color: var(--text-primary); }
.booking-header h1 span { color: var(--color-primary); }
.booking-header p { color: var(--text-secondary); margin-top: 6px; }

/* Layout Grid */
.layout-grid { display: grid; grid-template-columns: 350px 1fr; gap: 30px; }
@media (max-width: 1024px) {
    .layout-grid { grid-template-columns: 1fr; }
    .details-section { order: -1; }
    .summary-card { position: static; }
}

@media (max-width: 600px) {
    .form-row { grid-template-columns: 1fr; gap: 15px; } /* Increased vertical gap when stacked */
    .progress-bar { max-width: 100%; }
    .booking-header h1 { font-size: 1.8rem; }
}

.card { background: var(--bg-card); border-radius: 0; border: 1px solid var(--border-default); overflow: hidden; }
.card-title { padding: 15px 25px; background: var(--bg-input); border-bottom: 1px solid var(--border-default); font-weight: 800; font-size: 0.75rem; letter-spacing: 1px; color: var(--text-tertiary); text-transform: uppercase; }

.doctor-profile-mini { padding: 25px; display: flex; align-items: center; gap: 15px; border-bottom: 1px solid var(--border-default); }
.mini-avatar { width: 50px; height: 50px; background: var(--bg-input); color: var(--color-primary); border-radius: 0; display: flex; align-items: center; justify-content: center; font-weight: 800; }
.profile-info h3 { margin: 0; font-size: 1.1rem; }
.profile-info span { font-size: var(--font-size-small); color: var(--color-primary); font-weight: 700; }

.appointment-details { padding: 25px; display: flex; flex-direction: column; gap: 15px; }
.detail-item { display: flex; gap: 12px; align-items: flex-start; }
.detail-icon { color: var(--text-secondary); width: 20px; height: 20px; margin-top: 2px;} 
.detail-icon svg { width: 100%; height: 100%; } 
.detail-text label { display: block; font-size: var(--font-size-caption); font-weight: 800; color: var(--text-tertiary); text-transform: uppercase; }
.detail-text p { margin: 0; font-weight: 700; font-size: var(--font-size-small); }

input, select, textarea {
  width: 100%; 
  padding: 12px; 
  border: 2px solid var(--border-input); 
  border-radius: 0; 
  font-family: inherit;
  font-size: var(--font-size-base);
  box-sizing: border-box; /* This is the critical line */
}
/* Form */
.booking-form { padding: 25px; display: flex; flex-direction: column; gap: 20px; }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 25px; }
.input-group {
  display: flex; flex-direction: column; gap: 5px;
}
.input-group label { display: block; font-size: var(--font-size-small); font-weight: 800; margin-bottom: 8px; }
input, select, textarea {
  width: 100%; padding: 12px; border: 2px solid var(--border-input); border-radius: 0; font-family: inherit;
  font-size: var(--font-size-base);
}
textarea { min-height: 100px; resize: none; }
input:focus, select:focus, textarea:focus { border-color: var(--color-primary); outline: none; }

.btn-confirm {
  background: var(--color-primary); color: white; border: none; padding: 16px; border-radius: 0; font-weight: 800; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 10px;
}
.btn-confirm:disabled { opacity: 0.6; }

.spinner {
  width: 16px; height: 16px; border: 2px solid rgba(255, 255, 255, 0.3); border-radius: 50%;
  border-top-color: white; animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

.error-banner { margin: 25px; padding: 12px; background: var(--color-error-light); border: 1px solid var(--color-error-border); color: var(--color-error); border-radius: 0; display: flex; gap: 10px; }
.success-banner { margin: 25px; padding: 12px; background: var(--color-success-light); border: 1px solid var(--color-success-border); color: var(--color-success); border-radius: 0; display: flex; gap: 10px; }
</style>

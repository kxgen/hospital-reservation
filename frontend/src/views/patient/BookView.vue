<template>
  <div class="booking-flow">
    <!-- Progress Indicator -->
    <div class="progress-container wrapper">
      <div class="progress-bar">
        <div class="step active"><span>1</span><label>Selection</label></div>
        <div class="progress-line"></div>
        <div class="step active"><span>2</span><label>Review</label></div>
        <div class="progress-line" :class="{ filled: successMessage }"></div>
        <div class="step" :class="{ active: successMessage }"><span>3</span><label>Success</label></div>
      </div>
    </div>

    <main class="booking-main wrapper">
      <header class="booking-header">
        <button class="btn-back" @click="goBack">
          <span class="icon">←</span> Back to Specialists
        </button>
        <h1>Confirm <span>Appointment</span></h1>
        <p>You're just one step away from securing your consultation.</p>
      </header>

      <div class="layout-grid">
        <!-- Left: Details -->
        <div class="details-section">
          <div class="card summary-card shadow">
            <div class="card-title">Summary</div>
            
            <div class="doctor-profile-mini">
              <div class="mini-avatar">{{ doctorName[0] }}</div>
              <div class="profile-info">
                <h3>{{ doctorName }}</h3>
                <span>{{ specialty }}</span>
              </div>
            </div>

            <div class="appointment-details">
              <div class="detail-item">
                <div class="detail-text">
                  <label>Scheduled Date & Time</label>
                  <p>{{ selectedTimeLabel }}</p>
                </div>
              </div>
            </div>

            <button class="btn-change" @click="showChangeTimeModal = true">
              Change Appointment Time
            </button>
          </div>

          <!-- Notices -->
          <div class="info-card shadow-sm">
            <p><strong>Arrive Early:</strong> Please arrive 15 minutes before your scheduled time for check-in procedures.</p>
          </div>
        </div>

        <!-- Right: Form -->
        <div class="form-section">
          <div class="card form-card shadow">
            <div class="card-title">Additional Information</div>
            
            <div v-if="successMessage" class="success-banner animate-in">
              <span class="icon">✓</span>
              <div class="success-text">
                <h3>Appointment Confirmed</h3>
                <p>Redirecting you to your dashboard...</p>
              </div>
            </div>

            <form v-else @submit.prevent="confirmBooking" class="booking-form">
              <div class="input-group">
                <label>Reason for Visit</label>
                <textarea 
                  v-model="reason" 
                  placeholder="e.g., Routine checkup or persistent symptoms..."
                  required
                ></textarea>
                <p class="helper">Tell the doctor briefly about the purpose of your visit.</p>
              </div>

              <div class="form-agreement">
                <p>By confirming this appointment, you agree to our <a>Cancellation Policy</a> and <a>Terms of Service</a>.</p>
              </div>

              <button type="submit" class="btn-confirm" :disabled="isSubmitting">
                <span v-if="isSubmitting" class="spinner"></span>
                {{ isSubmitting ? 'Securing your spot...' : 'Confirm & Book Appointment' }}
              </button>
            </form>
          </div>
        </div>
      </div>
    </main>

    <!-- Timeslot Modal for changes -->
    <TimeslotModal
      v-if="doctor"
      :doctor="doctor"
      :appointments="appointments"
      :visible="showChangeTimeModal"
      :emitOnly="true"
      :date="new Date()"
      @time-selected="handleTimeChange"
      @close="closeModal"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'
import TimeslotModal from '@/components/TimeSlotModal.vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const alerts = useAlertStore()

const doctorId = Number(route.query.doctorId)
const doctorName = route.query.doctorName || ''
const specialty = route.query.specialty || ''

const selectedTimeslotId = ref(route.query.timeslotId || 0)
const selectedStartTime = ref(route.query.startTime)
const selectedEndTime = ref(route.query.endTime)
const selectedTimeLabel = ref(route.query.timeLabel)

const showChangeTimeModal = ref(false)
const doctor = ref(null)
const appointments = ref([])
const reason = ref('')

// UI State
const errorMessage = ref('')
const successMessage = ref('')
const isSubmitting = ref(false)

onMounted(async () => {
  if (!doctorId || !selectedStartTime.value) {
    router.replace('/doctors')
    return
  }
  
  try {
    const res = await axios.get(`/api/doctors/${doctorId}`)
    doctor.value = res.data
    appointments.value = doctor.value.timeslots || []
  } catch (err) {
    alerts.showAlert("Could not load doctor details. Please refresh the page.", "error")
  }
})

const goBack = () => router.push('/doctors')
const closeModal = () => { showChangeTimeModal.value = false }

const handleTimeChange = (selection) => {
  selectedTimeslotId.value = selection.timeslotId
  selectedStartTime.value = selection.startTime
  selectedEndTime.value = selection.endTime
  selectedTimeLabel.value = selection.timeLabel
  showChangeTimeModal.value = false
}

const confirmBooking = async () => {
  if (isSubmitting.value) return
  
  if (!reason.value || reason.value.length < 5) {
    alerts.showAlert("Please provide a brief reason (min 5 characters).", "warning")
    return
  }

  isSubmitting.value = true

  try {
    const payload = {
      DoctorId: doctorId,
      StartTime: selectedStartTime.value,
      EndTime: selectedEndTime.value,
      TimeSlotId: Number(selectedTimeslotId.value) || null,
      Reason: reason.value
    }

    const response = await axios.post('/api/appointments', payload, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })

    alerts.showAlert("Success! Your appointment is confirmed.", "success")
    router.push('/patient/dashboard')

  } catch (err) {
    console.error('Booking Error:', err)
    const backendMsg = err.response?.data?.Message || err.response?.data?.message
    alerts.showAlert(backendMsg || "Issue confirming booking. Please try a different time.", "error")
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
  max-width: 1000px;
  margin: 0 auto;
  padding: 0 20px;
}

/* Progress Stepper */
.progress-container {
  padding: 40px 0 20px;
}

.progress-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  max-width: 500px;
  margin: 0 auto;
}

.step {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  position: relative;
  z-index: 2;
}

.step span {
  width: 32px; height: 32px;
  border-radius: 50%;
  background: white;
  border: 2px solid var(--border-default);
  display: flex; align-items: center; justify-content: center;
  font-weight: 800; font-size: 0.9rem;
  color: var(--text-secondary);
  transition: all 0.3s;
}

.step label { font-size: 0.75rem; font-weight: 700; color: var(--text-secondary); text-transform: uppercase; letter-spacing: 0.5px; }

.step.active span { background: var(--color-primary); border-color: var(--color-primary); color: white; }
.step.active label { color: var(--color-primary); }

.progress-line {
  flex: 1;
  height: 3px;
  background: var(--border-default);
  margin: 0 -10px;
  transform: translateY(-12px);
  position: relative;
  z-index: 1;
}

.progress-line.filled { background: var(--color-primary); }

/* Header */
.booking-header {
  margin-bottom: 40px;
  text-align: left;
}

.btn-back {
  background: none; border: none; color: var(--color-primary);
  font-weight: 700; font-size: 0.9rem; cursor: pointer; padding: 0;
  display: flex; align-items: center; gap: 8px; margin-bottom: 15px;
}

.btn-back:hover { text-decoration: underline; }

.booking-header h1 { font-size: 2.2rem; font-weight: 800; margin: 0; color: var(--text-primary); }
.booking-header h1 span { color: var(--color-primary); }
.booking-header p { color: var(--text-secondary); margin-top: 6px; font-size: 1.1rem; }

/* Layout Grid */
.layout-grid {
  display: grid;
  grid-template-columns: 380px 1fr;
  gap: 30px;
}

.card {
  background: white;
  border-radius: 20px;
  overflow: hidden;
  border: 1px solid var(--border-default);
}

.card-title {
  padding: 20px 25px;
  background: var(--bg-input);
  border-bottom: 1px solid var(--border-default);
  font-weight: 800;
  text-transform: uppercase;
  font-size: 0.8rem;
  letter-spacing: 1px;
  color: var(--text-tertiary);
}

/* Summary Card */
.summary-card { padding-bottom: 25px; }

.doctor-profile-mini {
  padding: 25px;
  display: flex;
  align-items: center;
  gap: 15px;
  border-bottom: 1px solid var(--border-default);
}

.mini-avatar {
  width: 50px; height: 50px;
  background: var(--bg-input);
  color: var(--color-primary);
  border-radius: 12px;
  display: flex; align-items: center; justify-content: center;
  font-weight: 800; font-size: 1.2rem;
}

.profile-info h3 { margin: 0; font-size: 1.1rem; font-weight: 800; }
.profile-info span { font-size: 0.85rem; color: var(--color-primary); font-weight: 700; text-transform: uppercase; }

.appointment-details { padding: 25px; display: flex; flex-direction: column; gap: 20px; }

.detail-item { display: flex; gap: 15px; }
.detail-icon { font-size: 1.2rem; }

.detail-text label { display: block; font-size: 0.7rem; font-weight: 800; color: var(--text-tertiary); text-transform: uppercase; margin-bottom: 4px; }
.detail-text p { margin: 0; font-weight: 700; color: var(--text-primary); font-size: 0.95rem; line-height: 1.4; }

.btn-change {
  margin: 0 25px;
  background: var(--bg-input);
  border: 1px solid var(--border-default);
  padding: 12px;
  border-radius: 10px;
  width: calc(100% - 50px);
  font-weight: 700;
  font-size: 0.85rem;
  color: var(--color-gray-600);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-change:hover { background: var(--color-gray-100); color: var(--color-primary); }

.info-card {
  margin-top: 20px;
  background: #fff9db;
  border: 1px solid #ffec99;
  padding: 15px 20px;
  border-radius: 16px;
  display: flex;
  gap: 12px;
  font-size: 0.85rem;
  color: #856404;
}

/* Form Card */
.form-card { padding-bottom: 10px; }

.booking-form { padding: 25px; }

.input-group { margin-bottom: 25px; }
.input-group label { display: block; font-weight: 800; color: var(--text-primary); margin-bottom: 10px; }

textarea {
  width: 100%;
  min-height: 140px;
  padding: 15px;
  border: 2px solid var(--border-input);
  border-radius: 12px;
  background: var(--bg-input);
  font-family: inherit;
  font-size: 1rem;
  color: var(--text-primary);
  resize: none;
  transition: all 0.2s;
}

textarea:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 4px var(--color-primary-glow);
}

.helper { font-size: 0.8rem; color: var(--text-secondary); margin-top: 8px; }

.form-agreement { margin-bottom: 30px; padding: 15px; background: var(--bg-input); border-radius: 10px; }
.form-agreement p { margin: 0; font-size: 0.85rem; color: var(--text-secondary); line-height: 1.5; }
.form-agreement a { color: var(--color-primary); font-weight: 700; text-decoration: none; cursor: pointer; }

.btn-confirm {
  width: 100%;
  background: var(--color-primary);
  color: white;
  border: none;
  padding: 18px;
  border-radius: 12px;
  font-weight: 800;
  font-size: 1.1rem;
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  gap: 12px;
  transition: all 0.3s;
}

.btn-confirm:hover:not(:disabled) { background: var(--color-primary-hover); transform: translateY(-2px); box-shadow: var(--shadow-primary); }
.btn-confirm:disabled { opacity: 0.6; cursor: not-allowed; }

/* Banners */
.error-banner, .success-banner {
  margin: 25px;
  padding: 15px 20px;
  border-radius: 12px;
  display: flex;
  gap: 12px;
}

.error-banner { background: var(--color-error-light); border: 1px solid var(--color-error-border); color: var(--color-error); }
.success-banner { background: var(--color-success-light); border: 1px solid var(--color-success-border); color: var(--color-success); }

.success-text h3 { margin: 0; font-size: 1.1rem; font-weight: 800; }
.success-text p { margin: 5px 0 0; font-size: 0.9rem; opacity: 0.8; }

.animate-in { animation: slideUp 0.4s ease-out; }

@keyframes slideUp {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.spinner {
  width: 18px; height: 18px;
  border: 2px solid rgba(255,255,255,0.3);
  border-top-color: white; border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

@media (max-width: 900px) {
  .layout-grid { grid-template-columns: 1fr; }
  .booking-header { text-align: center; }
  .btn-back { margin: 0 auto 20px; }
  .details-section { order: 2; }
  .form-section { order: 1; }
}
</style>
<template>
  <div class="doctor-container followup-view">
    <div v-if="loading" class="loader-container">
        <div class="spinner"></div>
        <p>Loading clinical session context...</p>
    </div>

    <div v-else-if="error" class="error-container">
        <p>{{ error }}</p>
        <button @click="fetchFollowupDetails" class="btn-primary">Try Again</button>
    </div>

    <div v-else>
        <!-- Header with Back Link -->
        <header class="page-header">
          <div class="header-content">
            <router-link to="/doctor/dashboard" class="back-link">← Cancel and Exit Visit</router-link>
            <h1 class="title-mt">Clinical <span>Session</span></h1>
            <p>Reviewing linkage and progress for the current session.</p>
          </div>
        </header>

        <!-- Patient Banner -->
        <div class="patient-banner shadow-sm">
          <div class="patient-identity">
            <div class="avatar-circle">{{ patient.firstName[0] }}</div>
            <div class="id-details">
              <h3>{{ patient.firstName }} {{ patient.lastName }}</h3>
              <span class="p-meta">DOB: {{ formatDate(patient.dateOfBirth) }}</span>
            </div>
          </div>
          
          <div class="session-timer-display">
             <div class="big-time">{{ formatTime(currentApt.startTime) }}</div>
             <div class="med-date">{{ formatDate(currentApt.startTime) }}</div>
          </div>

          <div class="status-badge pulse">Patient In Room</div>
        </div>

        <!-- Linkage Grid: Parent vs Child -->
        <!-- Linkage Grid: Consolidated View -->
        <div class="linkage-grid">
          <!-- Previous Consultation (Historical/Context) -->
          <div class="card linkage-card parent-card shadow-sm" :class="{ 'no-parent': !parentApt }">
            <span class="card-label">Previous Consultation</span>
            <div v-if="parentApt">
                <div class="time-header">{{ formatDate(parentApt.startTime) }}</div>
                <div class="apt-meta">Dr. {{ parentApt.doctorName }}</div>
                <div class="intent-box">
                  "{{ parentApt.doctorReminder || 'No instruction recorded' }}"
                </div>
            </div>
            <div v-else class="empty-parent">
                <p>No immediate follow-up context.</p>
            </div>
          </div>

          <!-- Current Consultation (Active) -->
          <div class="card linkage-card child-card shadow-sm">
            <span class="card-label highlight">Patient Note</span>
            <!-- Date moved to banner -->
            <div class="time-header">Current Visit</div>
            <div class="apt-meta">{{ patient.firstName }} {{ patient.lastName }}</div>
            <div class="intent-box">
              {{ currentApt.reason || 'General Review' }}
            </div>
          </div>
        </div>

        <!-- Refinement Grid: Notes and Reliability -->
        <div class="refinement-grid">

          <div class="card info-card reliability-box shadow-sm">
            <span class="card-label success">Attendance</span>
            <div class="reliability-stat">
              <strong>{{ reliability.rate }} Attendance Rate</strong>
              <span class="stat-detail">({{ reliability.missed }} Missed / {{ reliability.attended }} Attended)</span>
            </div>
            <small class="reliability-hint">Based on appointments with you.</small>
          </div>
        </div>

        <!-- Note Area / Formulation -->
        <div class="card note-area shadow-sm">
          <span class="card-label">Visit Summary (Follow-up Details)</span>
          <textarea 
            v-model="summary" 
            rows="4" 
            placeholder="Enter summary observation or plan here..."
            class="styled-textarea"
          ></textarea>
          
          <div class="control-row">
            <div class="checkbox-group">
              <input type="checkbox" id="resolved" v-model="isResolved"> 
              <label for="resolved">Issue Resolved / Goal Met</label>
            </div>
            <div class="hint-text">(Closes this follow-up loop)</div>
          </div>

          <div class="next-apt-section" v-if="!isResolved">
            <label class="section-label">Schedule Next Appointment (Optional):</label>
            <div class="scheduling-action">
                <p v-if="!doctorFull" class="meta-info">Loading availability...</p>
                <div v-else class="scheduler-box">
                    <button class="btn-schedule-trigger" @click="showTimeModal = true">
                        {{ selectedFollowupTime ? 'Change Slot' : 'Select Future Slot' }}
                    </button>
                    <div v-if="selectedFollowupTime" class="selected-slot-info">
                        <span class="slot-label">Selected:</span> 
                        <span class="slot-value">{{ selectedFollowupTime.timeLabel }}</span>
                    </div>
                </div>
            </div>
          </div>
          <div v-else class="completion-message">
            <span class="icon">✓</span> Case marked as resolved. No follow-up needed.
          </div>
        </div>

        <!-- Footer Actions -->
        <div class="footer-actions">
          <button class="btn-primary" @click="requestFinalization">Sign & Finalize Appointment</button>
        </div>
    </div>

    <!-- Timeslot Modal -->
    <TimeSlotModal
      v-if="doctorFull"
      :visible="showTimeModal"
      :doctor="doctorFull"
      :appointments="doctorFull.timeslots"
      :emitOnly="true"
      @close="showTimeModal = false"
      @time-selected="handleTimeSelected"
    />

    <!-- Confirmation Modal -->
    <ConfirmationModal
      :visible="confirmModal.visible"
      :title="confirmModal.title"
      :message="confirmModal.message"
      :type="confirmModal.type"
      confirmText="Yes, Finalize"
      cancelText="Cancel"
      @close="confirmModal.visible = false"
      @confirm="executeFinalization"
    />
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'
import { formatDate, formatTime } from '@/utils/dateFormatter'
import TimeSlotModal from '@/components/TimeSlotModal.vue'
import ConfirmationModal from '@/components/ConfirmationModal.vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const alertStore = useAlertStore()

const appointmentId = Number(route.params.id)
const todayDate = new Date().toISOString().split('T')[0]

// Data
const loading = ref(true)
const error = ref(null)
const data = ref(null)

// Interactive States
const summary = ref('')
const isResolved = ref(false)
const showTimeModal = ref(false)
const selectedFollowupTime = ref(null)
const doctorFull = ref(null)
const isScheduling = ref(false)

// Confirmation Modal State
const confirmModal = ref({
  visible: false,
  title: 'Confirm Finalization',
  message: 'Are you sure you want to sign and finalize this visit outcome?',
  type: 'info'
})

const patient = computed(() => data.value?.patient || {})
const parentApt = computed(() => data.value?.parentAppointment)
const currentApt = computed(() => data.value?.currentAppointment || {})
const reliability = computed(() => data.value?.reliability || { rate: '0%', missed: 0, attended: 0 })

// Watcher: If issue is resolved, clear any selected time
watch(isResolved, (newVal) => {
    if (newVal) {
        selectedFollowupTime.value = null
    }
})

const fetchFollowupDetails = async () => {
    loading.value = true
    try {
        const res = await axios.get(`/api/appointments/${appointmentId}/followup-details`, {
            headers: { Authorization: `Bearer ${auth.token}` }
        })
        data.value = res.data
        summary.value = data.value.currentAppointment.doctorReminder || ''
    } catch (err) {
        console.error('Fetch Error:', err)
        error.value = "Failed to load session details. Please try again."
    } finally {
        loading.value = false
    }
}

const fetchDoctorInfo = async () => {
    try {
        const doctorsRes = await axios.get('/api/doctors')
        
        // Find doctor by ID from the current appointment
        if (currentApt.value.doctorId) {
            doctorFull.value = doctorsRes.data.find(d => d.id === currentApt.value.doctorId)
        }

        // If not found by ID (fallback), try by full name match
        if (!doctorFull.value) {
            doctorFull.value = doctorsRes.data.find(d => {
                const fullName = `Dr. ${d.firstName} ${d.lastName}`
                return fullName === currentApt.value.doctorName || d.id === auth.id
            })
        }
    } catch (err) {
        console.error('Doctor Info fetch failed', err)
    }
}

onMounted(async () => {
    await fetchFollowupDetails()
    await fetchDoctorInfo()
})

const handleTimeSelected = (selection) => {
    selectedFollowupTime.value = selection
    showTimeModal.value = false
    // Do not book immediately. Defer to finalization.
}

const saveDraft = () => {
  alertStore.showAlert('Draft saved locally (simulated).', 'success')
}

const requestFinalization = () => {
    if (!summary.value) {
        alertStore.showAlert("Please enter a visit summary before finalizing.", "error")
        return
    }

    // Validation: Enforce either Resolved OR Future Booking
    if (!isResolved.value && !selectedFollowupTime.value) {
        alertStore.showAlert("Action Required: Please select a follow-up time OR mark the issue as resolved.", "warning")
        return
    }

    confirmModal.value.visible = true
}

const executeFinalization = async () => {
  confirmModal.value.visible = false
  
  try {
      // 1. If follow-up selected, book it first
      if (!isResolved.value && selectedFollowupTime.value) {
          const selection = selectedFollowupTime.value
          const payload = {
            DoctorId: doctorFull.value.id,
            PatientId: data.value.currentAppointment.patientDbId,
            StartTime: selection.startTime,
            EndTime: selection.endTime,
            TimeSlotId: selection.timeslotId || null,
            Reason: `Follow up from ${formatDate(currentApt.value.startTime)}`,
            ParentAppointmentId: appointmentId
          }
          
          await axios.post('/api/appointments', payload, {
            headers: { Authorization: `Bearer ${auth.token}` }
          })
      }

      // 2. Finalize current appointment
      await axios.post(`/api/appointments/${appointmentId}/finalize`, {
          DoctorReminder: summary.value,
          IsResolved: isResolved.value
      }, {
          headers: { Authorization: `Bearer ${auth.token}` }
      })

      alertStore.showAlert('Appointment finalized and signed.', "success")
      router.push('/doctor/dashboard')
  } catch (err) {
      console.error(err)
      alertStore.showAlert("Failed to finalize. If booking a follow-up, the slot might have been taken.", "error")
  }
}
</script>

<style scoped>
.followup-view {
  max-width: 900px;
  margin: 0 auto;
  animation: fadeIn 0.4s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.back-link { color: var(--text-secondary); text-decoration: none; font-weight: 600; font-size: 0.85rem; }
.back-link:hover { color: var(--color-primary); }

.title-mt { margin-top: 10px; }

/* Patient Banner */
.patient-banner {
  background: var(--bg-card);
  border-radius: 0;
  padding: 20px 30px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 25px;
  border: 1px solid var(--border-default);
}

.patient-identity { display: flex; align-items: center; gap: 15px; }

.avatar-circle {
  width: 50px; height: 50px;
  background: var(--color-primary-light);
  color: var(--color-primary);
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-weight: 800; font-size: 1.2rem;
}

.id-details h3 { margin: 0; font-size: 1.2rem; color: var(--text-primary); }
.p-meta { font-size: 0.75rem; color: var(--text-tertiary); }

.status-badge {
  background: var(--color-info-light);
  color: var(--color-info);
  padding: 6px 14px;
  border-radius: 50px;
  font-weight: 800;
  font-size: 0.75rem;
  text-transform: uppercase;
}

.pulse { animation: statusPulse 2s infinite; }
@keyframes statusPulse {
  0% { box-shadow: 0 0 0 0 rgba(66, 153, 225, 0.4); }
  70% { box-shadow: 0 0 0 10px rgba(66, 153, 225, 0); }
  100% { box-shadow: 0 0 0 0 rgba(66, 153, 225, 0); }
}

.session-timer-display {
    text-align: center;
    padding: 0 20px;
    border-left: 1px solid var(--border-default);
    border-right: 1px solid var(--border-default);
}

.big-time {
    font-size: 2.2rem;
    font-weight: 800;
    color: var(--color-primary);
    line-height: 1;
}

.med-date {
    font-size: 0.9rem;
    font-weight: 600;
    color: var(--text-secondary);
    margin-top: 4px;
}

/* Linkage Grid */
.linkage-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  margin-bottom: 25px;
}

.linkage-card { padding: 25px; position: relative; }
.parent-card { opacity: 0.8; border: 1px dashed var(--border-default); }
.child-card { border-left: 4px solid var(--color-primary); }

.card-label {
  display: block;
  font-size: 0.65rem;
  font-weight: 800;
  color: var(--text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.1em;
  margin-bottom: 12px;
}
.card-label.highlight { color: var(--color-primary); }

.time-header { font-size: 1.1rem; font-weight: 800; color: var(--text-primary); margin-bottom: 4px; }
.apt-meta { font-size: 0.8rem; color: var(--text-secondary); margin-bottom: 15px; }
.status-text { color: var(--color-primary); font-weight: 700; }

.intent-box {
  background: var(--bg-input);
  padding: 15px;
  border-radius: 0;
  font-size: 0.85rem;
  color: var(--text-primary);
  line-height: 1.5;
}
.intent-box strong { color: var(--text-primary); display: block; margin-bottom: 4px; }

/* Refinement Grid */
.refinement-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  margin-bottom: 25px;
}

.info-card { padding: 20px 25px; }

.card-label.warning { color: var(--color-warning); }
.card-label.success { color: var(--color-success); }

.note-content { font-size: 0.9rem; color: var(--text-primary); font-style: italic; margin-top: 10px; }

.reliability-stat { margin: 10px 0 5px; }
.reliability-stat strong { font-size: 1.1rem; color: var(--color-success); }
.stat-detail { font-size: 0.8rem; color: var(--text-tertiary); margin-left: 8px; }
.reliability-hint { font-size: 0.75rem; color: var(--text-secondary); display: block; }

/* Note Area */
.note-area { padding: 30px; margin-bottom: 30px; }

.styled-textarea {
  width: 100%;
  margin-top: 15px;
  padding: 15px;
  border: 1px solid var(--border-input);
  border-radius: 12px;
  font-family: inherit;
  font-size: 0.95rem;
  resize: vertical;
  background: var(--bg-card);
  color: var(--text-primary);
  transition: all 0.2s;
}
.styled-textarea:focus { outline: none; border-color: var(--color-primary); box-shadow: 0 0 0 4px var(--color-primary-glow); }

.control-row {
  display: flex;
  align-items: center;
  gap: 15px;
  margin-top: 15px;
  padding-bottom: 20px;
  border-bottom: 1px solid var(--border-default);
}

.checkbox-group { display: flex; align-items: center; gap: 10px; }
.checkbox-group input { width: 18px; height: 18px; cursor: pointer; accent-color: var(--color-primary); }
.checkbox-group label { font-weight: 700; font-size: 0.95rem; color: var(--text-primary); cursor: pointer; }

.hint-text { font-size: 0.8rem; color: var(--text-tertiary); }

.next-apt-section { margin-top: 25px; }
.section-label { display: block; font-weight: 700; font-size: 0.95rem; color: var(--text-primary); margin-bottom: 15px; }

.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 25px; }

.styled-input {
  width: 100%;
  padding: 12px 15px;
  border: 1px solid var(--border-input);
  border-radius: 10px;
  font-family: inherit;
  font-size: 0.95rem;
  background: var(--bg-card);
  color: var(--text-primary);
}
.styled-input:focus { outline: none; border-color: var(--color-primary); }

/* Footer Actions */
.footer-actions {
  display: flex;
  justify-content: flex-end;
  gap: 15px;
}

.btn-ghost {
  background: transparent;
  border: 1px solid var(--border-default);
  color: var(--text-secondary);
  padding: 14px 28px;
  border-radius: 0;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-ghost:hover { background: var(--bg-input); color: var(--text-primary); }

.btn-primary {
  background: var(--color-primary);
  color: white;
  border: none;
  padding: 14px 32px;
  border-radius: 0;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-primary:hover { transform: translateY(-2px); box-shadow: var(--shadow-primary); }

.card {
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  border-radius: 0;
}

@media (max-width: 768px) {
  .linkage-grid, .refinement-grid, .form-row { grid-template-columns: 1fr; gap: 15px; }
  .patient-banner { flex-direction: column; align-items: flex-start; gap: 15px; }
}

/* Loader & Error */
.loader-state, .error-container {
    padding: 100px 20px;
    text-align: center;
    background: var(--bg-card);
    border-radius: 20px;
    border: 1px solid var(--border-default);
    margin-top: 40px;
}

.spinner {
    width: 40px; height: 40px;
    border: 3px solid var(--bg-input);
    border-top-color: var(--color-primary);
    border-radius: 50%;
    margin: 0 auto 20px;
    animation: spin 0.8s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

/* Scheduling */
.btn-schedule-trigger {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 14px 25px;
    background: var(--bg-input);
    border: 1px solid var(--border-default);
    border-radius: 0;
    font-weight: 700;
    color: var(--color-primary);
    cursor: pointer;
    transition: all 0.2s;
}

.btn-schedule-trigger:hover {
    background: var(--color-primary-light);
    border-color: var(--color-primary);
}

.meta-info { font-size: 0.85rem; color: var(--text-tertiary); }
.empty-parent { padding: 40px; text-align: center; color: var(--text-tertiary); font-style: italic; }
.no-parent { opacity: 0.6; }
.completion-message {
    margin-top: 25px;
    background: #f0fdf4;
    border: 1px solid #bbf7d0;
    color: #166534;
    padding: 15px;
    border-radius: 0;
    text-align: center;
    font-weight: 700;
}

.scheduler-box { display: flex; align-items: center; gap: 15px; flex-wrap: wrap; }
.selected-slot-info { 
    background: #e0f2fe; 
    padding: 10px 15px; 
    border-radius: 8px; 
    color: #0369a1; 
    border: 1px solid #bae6fd;
    display: flex;
    gap: 8px;
    align-items: center;
}
.slot-label { font-size: 0.75rem; text-transform: uppercase; font-weight: 700; opacity: 0.8; }
.slot-value { font-weight: 800; font-size: 0.95rem; }
</style>

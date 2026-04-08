<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'
import TimeSlotModal from '@/components/TimeSlotModal.vue'
import ConfirmationModal from '@/components/ConfirmationModal.vue'

const props = defineProps({
  id: {
    type: [String, Number],
    required: true
  }
})

const auth = useAuthStore()
const alerts = useAlertStore()
const route = useRoute()
const router = useRouter()

// --- State ---
const appointment = ref(null)
const isLoading = ref(true)
const isEditing = ref(false)
const editReason = ref('')

// Modals State
const showTimeModal = ref(false)
const availableSlots = ref([])
const confirmModal = ref({ visible: false, title: '', message: '', type: 'info', confirmText: 'Confirm', cancelText: 'Cancel', onConfirm: () => {} })

// Auth headers
const getAuthHeaders = () => ({
  headers: { Authorization: `Bearer ${auth.token || localStorage.getItem('token')}` }
})

// --- Fetching Data ---
const fetchAppointmentDetail = async () => {
  isLoading.value = true
  try {
    const res = await axios.get(`/api/appointments/${props.id}`, getAuthHeaders())
    appointment.value = res.data
    editReason.value = res.data.reason
  } catch (err) {
    console.error('Fetch failed:', err)
    alerts.showAlert('Failed to load appointment details.', 'error')
    // Redirect back to list if appointment not found or access denied
    setTimeout(() => router.replace('/patient/appointments'), 2000)
  } finally {
    isLoading.value = false
  }
}

const fetchAvailableSlots = async () => {
  try {
    const res = await axios.get(
      `/api/appointments/${props.id}/available-slots`,
      getAuthHeaders()
    )
    availableSlots.value = res.data || []
  } catch (err) {
    console.error('Failed to load available slots:', err)
    availableSlots.value = []
  }
}

// --- Actions ---

const toggleEdit = () => {
  if (isEditing.value) {
    // If cancelling edit, reset the reason
    editReason.value = appointment.value.reason
  }
  isEditing.value = !isEditing.value
}

const saveChanges = async () => {
  try {
    // Assuming there's an endpoint to update appointment details like reason
    // If not, we can either add one or just focus on cancel/reschedule as requested.
    // The user said "make it work properly including the data it fetches, everything on the page"
    // So I'll add a simple update endpoint if needed, but for now let's assume it's just local or add it.
    
    // For now, let's just update the local state if no backend support yet, OR I can add it to the backend.
    // The user mentioned "setup the proper backend to function properly" so I should add it.
    await axios.put(`/api/appointments/${appointment.value.id}/update-reason`, { reason: editReason.value }, getAuthHeaders())
    
    appointment.value.reason = editReason.value
    isEditing.value = false
    alerts.showAlert('Details updated successfully!', 'success')
  } catch (err) {
    alerts.showAlert('Failed to update details.', 'error')
  }
}

const handleCancelRequest = () => {
  openConfirm({
    title: 'Cancel Appointment',
    message: 'Are you sure you want to cancel this visit? This action cannot be undone.',
    type: 'danger',
    confirmText: 'Yes, Cancel',
    cancelText: 'No, Keep It',
    onConfirm: performCancel
  })
}

const performCancel = async () => {
  try {
    await axios.put(`/api/appointments/${appointment.value.id}/cancel`, {}, getAuthHeaders())
    appointment.value.status = 'Cancelled'
    closeConfirm()
    alerts.showAlert('Appointment cancelled successfully.', 'success')
     // Optionally redirect back after a delay
     router.push('/patient/appointments')
  } catch (err) {
    closeConfirm()
    alerts.showAlert('Cancellation failed.', 'error')
  }
}

const openReschedule = async () => {
  await fetchAvailableSlots()
  showTimeModal.value = true
}

const handleRescheduleSelect = (selection) => {
  showTimeModal.value = false
  openConfirm({
    title: 'Confirm Reschedule',
    message: `Are you sure you want to move this appointment to <strong>${selection.timeLabel}</strong>?`,
    type: 'warning',
    confirmText: 'Yes, Reschedule',
    onConfirm: () => performReschedule(selection)
  })
}

const performReschedule = async ({ startTime, endTime }) => {
  try {
    await axios.put(
      `/api/appointments/${appointment.value.id}/reschedule`,
      { NewStartTime: startTime, NewEndTime: endTime },
      getAuthHeaders()
    )
    
    await fetchAppointmentDetail()
    closeConfirm()
    alerts.showAlert('Your appointment has been successfully rescheduled.', 'success')
  } catch (err) {
    console.error('Reschedule error:', err)
    closeConfirm()
    alerts.showAlert('Rescheduling failed.', 'error')
  }
}

// --- Helpers ---
const openConfirm = (config) => { 
  confirmModal.value = { ...config, visible: true } 
}
const closeConfirm = () => { confirmModal.value.visible = false }

const formatDate = (dateStr, format) => {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  const opts = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }
  if (format === 'time') return formatTime(dateStr)
  return d.toLocaleDateString('en-US', opts)
}

import { formatTime } from '@/utils/dateFormatter'

onMounted(() => {
  fetchAppointmentDetail()
})
</script>

<template>
  <div class="full-page-container">
    <header class="detail-header">
      <router-link to="/patient/appointments" class="back-link">← Back to List</router-link>
      <h1 v-if="appointment && (appointment.status.toLowerCase() === 'completed' || appointment.status.toLowerCase() === 'cancelled' || appointment.status.toLowerCase() === 'pending')">
        Session <span>Archive</span>
      </h1>
      <h1 v-else>Appointment <span>Details</span></h1>
    </header>

    <div v-if="isLoading" class="loader">Loading...</div>

    <main v-else class="detail-card shadow">
      <section class="top-bar">
        <div class="doctor-info">
          <div class="avatar">{{ appointment.doctorName.charAt(0) }}</div>
          <div>
            <h2>Dr. {{ appointment.doctorName }}</h2>
            <p class="specialty">{{ appointment.specialty }}</p>
          </div>
        </div>
        <div :class="['status-badge', appointment.status.toLowerCase()]">
          {{ appointment.status }}
        </div>
      </section>

      <hr class="divider" />

      <section class="info-grid">
        <div class="info-group">
          <label>Date</label>
          <div class="value">{{ formatDate(appointment.startTime) }}</div>
        </div>
        <div class="info-group">
          <label>Time</label>
          <div class="value">
            {{ formatDate(appointment.startTime, 'time') }}
          </div>
        </div>
      </section>

      <section class="reason-section">
        <div class="section-header">
          <label>Reason for Visit</label>
          <button v-if="!isEditing && appointment.status.toLowerCase() !== 'completed' && appointment.status.toLowerCase() !== 'cancelled' && appointment.status.toLowerCase() !== 'pending'" @click="toggleEdit" class="btn-edit-text">Edit Details</button>
        </div>

        <div v-if="!isEditing" class="reason-text">
          {{ appointment.reason }}
        </div>

        <div v-else class="edit-mode">
          <textarea v-model="editReason" rows="5" class="custom-textarea"></textarea>
          <div class="edit-actions">
            <button @click="toggleEdit" class="btn-cancel">Cancel</button>
            <button @click="saveChanges" class="btn-save">Save Changes</button>
          </div>
        </div>
      </section>

      <footer class="footer-actions" v-if="!isEditing && appointment.status.toLowerCase() !== 'completed' && appointment.status.toLowerCase() !== 'cancelled' && appointment.status.toLowerCase() !== 'pending'">
        <button @click="openReschedule" class="btn-reschedule-full">Reschedule Appointment</button>
        <button @click="handleCancelRequest" class="btn-cancel-visit">Cancel Visit</button>
      </footer>
    </main>

    <TimeSlotModal
      :visible="showTimeModal"
      :appointments="availableSlots"
      :doctor="{ name: appointment?.doctorName, specialty: appointment?.specialty, id: appointment?.doctorId }"
      :emit-only="true"
      @close="showTimeModal = false"
      @time-selected="handleRescheduleSelect"
    />

    <ConfirmationModal
      v-bind="confirmModal"
      @close="closeConfirm"
      @confirm="confirmModal.onConfirm"
    />
  </div>
</template>

<style scoped>
.full-page-container {
  max-width: 800px;
  margin: 40px auto;
  padding: 0 20px;
}

.detail-header {
  margin-bottom: 30px;
}

.back-link {
  color: var(--color-primary);
  text-decoration: none;
  font-weight: 600;
  display: block;
  margin-bottom: 10px;
  transition: all 0.2s;
}

.back-link:hover {
  transform: translateX(-5px);
  color: var(--color-primary-hover);
}

.detail-header h1 {
  font-size: 2rem;
  font-weight: 800;
  color: var(--text-primary);
}

.detail-header h1 span { color: var(--color-primary); }

.detail-card {
  background: var(--bg-card);
  border-radius: 0;
  padding: 40px;
  border: 1px solid var(--border-default);
}

/* Top Bar */
.top-bar {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.doctor-info {
  display: flex;
  gap: 20px;
  align-items: center;
}

.avatar {
  width: 60px;
  height: 60px;
  background: var(--bg-input);
  color: var(--color-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  font-size: 1.5rem;
  font-weight: 800;
  border: 2px solid var(--border-default);
}

.doctor-info h2 { margin: 0; font-size: 1.4rem; color: var(--text-primary); }
.specialty { color: var(--color-primary); font-weight: 600; margin: 0; }

.status-badge {
  padding: 6px 16px;
  border-radius: 50px;
  font-size: 0.8rem;
  font-weight: 800;
  text-transform: uppercase;
}
.status-badge.scheduled { background: var(--bg-status-info); color: var(--status-info-text); }
.status-badge.cancelled { background: var(--bg-status-error); color: var(--status-error-text); }

.divider {
  margin: 30px 0;
  border: 0;
  border-top: 1px solid var(--border-default);
}

/* Grid Info */
.info-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 30px;
  margin-bottom: 40px;
}

.info-group label {
  display: block;
  font-size: 0.75rem;
  text-transform: uppercase;
  color: var(--text-tertiary);
  font-weight: 800;
  margin-bottom: 8px;
}

.info-group .value {
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--text-primary);
}

/* Reason Section */
.reason-section {
  background: var(--bg-input);
  padding: 25px;
  border-radius: 0;
  margin-bottom: 40px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 15px;
}

.section-header label {
  font-size: 0.75rem;
  text-transform: uppercase;
  color: var(--text-tertiary);
  font-weight: 800;
}

.btn-edit-text {
  background: none;
  border: none;
  color: var(--color-primary);
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-edit-text:hover {
  color: var(--color-primary-hover);
  text-decoration: underline;
}

.reason-text {
  font-size: 1rem;
  line-height: 1.6;
  color: var(--text-secondary);
}

/* Edit Mode */
.custom-textarea {
  width: 100%;
  padding: 15px;
  border: 2px solid var(--border-default);
  border-radius: 0;
  background: var(--bg-card);
  color: var(--text-primary);
  font-family: inherit;
  font-size: 1rem;
  resize: vertical;
  box-sizing: border-box;
}

.edit-actions {
  display: flex;
  justify-content: flex-end;
  gap: 15px;
  margin-top: 15px;
}

.btn-save {
  background: var(--color-primary);
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 6px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-save:hover {
  background: var(--color-primary-hover);
  transform: translateY(-1px);
  box-shadow: var(--shadow-sm);
}

.btn-cancel {
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  color: var(--text-primary);
  padding: 10px 20px;
  border-radius: 0;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-cancel:hover {
  background: var(--bg-input);
  border-color: var(--border-input);
}

/* Footer Actions */
.footer-actions {
  display: flex;
  gap: 20px;
}

.btn-reschedule-full {
  flex: 2;
  background: var(--bg-card);
  color: var(--text-primary);
  border: 1px solid var(--border-default);
  padding: 16px;
  border-radius: 0;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}

.btn-reschedule-full:hover {
  background: var(--color-primary-hover);
  transform: translateY(-2px);
  box-shadow: var(--shadow-primary);
}

.btn-cancel-visit {
  flex: 1;
  background: var(--bg-card);
  color: var(--color-error);
  border: 1.5px solid var(--color-error);
  padding: 16px;
  border-radius: 0;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-cancel-visit:hover {
  background: var(--color-primary-light);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}
</style>
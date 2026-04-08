<script setup>
import { ref, onMounted, computed } from 'vue'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'
import { formatTime, formatDate } from '@/utils/dateFormatter'
import ConfirmationModal from '@/components/ConfirmationModal.vue'
import TimeSlotModal from '@/components/TimeSlotModal.vue'

const auth = useAuthStore()
const alertStore = useAlertStore()

defineProps({
  isEmbedded: { type: Boolean, default: false }
})

const confirmModalState = ref({
    visible: false,
    title: '',
    message: '',
    type: 'info',
    onConfirm: null
})

const myAppointments = ref([])
const isLoading = ref(true)

const fetchMyBookings = async () => {
    isLoading.value = true
    try {
        const res = await axios.get(`/api/receptionist/my-bookings`, {
            headers: { Authorization: `Bearer ${auth.token}` }
        })
        myAppointments.value = res.data || []
    } catch (err) {
        console.error('Failed to load my bookings', err)
    } finally {
        isLoading.value = false
    }
}

const cancelAppointment = async (id) => {
    confirmModalState.value = {
        visible: true,
        title: 'Cancel Appointment',
        message: 'Are you sure you want to cancel this appointment? This action cannot be undone.',
        type: 'danger',
        onConfirm: async () => {
             try {
                await axios.put(`/api/appointments/${id}/cancel`, {}, {
                    headers: { Authorization: `Bearer ${auth.token}` }
                })
                alertStore.showAlert('Appointment cancelled.', 'success')
                fetchMyBookings()
            } catch (err) {
                alertStore.showAlert('Failed to cancel.', 'error')
            }
            confirmModalState.value.visible = false
        }
    }
}

const confirmAction = () => {
    if (confirmModalState.value.onConfirm) {
        confirmModalState.value.onConfirm()
    }
}

// --- Reschedule Logic ---
const showRescheduleModal = ref(false)
const rescheduleData = ref({ appointmentId: null, doctor: null, date: null, slots: [] })

const openReschedule = async (apt) => {
    try {
        isLoading.value = true
        const res = await axios.get(`/api/doctors/${apt.doctorId}`, {
             headers: { Authorization: `Bearer ${auth.token}` }
        })
        
        const doctor = res.data
        const timeslots = doctor.timeslots || []
        
        rescheduleData.value = {
            appointmentId: apt.id,
            doctor: { id: doctor.id, name: doctor.fullName, specialty: doctor.specialtyName || apt.specialty },
            date: new Date(),
            slots: timeslots
        }
        showRescheduleModal.value = true
    } catch (err) {
        console.error("Failed to load doctor availability", err)
        alertStore.showAlert("Failed to load availability for rescheduling.", "error")
    } finally {
        isLoading.value = false
    }
}

const handleRescheduleSelection = async (slotData) => {
    try {
        const payload = {
            NewStartTime: slotData.startTime,
            NewEndTime: slotData.endTime
        }
        await axios.put(`/api/appointments/${rescheduleData.value.appointmentId}/reschedule`, payload, {
            headers: { Authorization: `Bearer ${auth.token}` }
        })
        alertStore.showAlert(`Rescheduled successfully to ${slotData.timeLabel}`, 'success')
        fetchMyBookings()
    } catch (err) {
        console.error("Reschedule failed", err)
        alertStore.showAlert(err.response?.data || "Failed to reschedule.", "error")
    }
}

const canEdit = (apt) => {
    const aptDate = new Date(apt.startTime)
    return aptDate > new Date() && apt.status !== 'cancelled' && apt.status !== 'completed'
}

onMounted(fetchMyBookings)
</script>

<template>
  <div class="page-container" :class="{ 'embedded': isEmbedded }">
    <header v-if="!isEmbedded" class="page-header">
      <h1>My <span>Bookings</span></h1>
      <p>Manage appointments you have created manually.</p>
    </header>

    <div class="content-card">
        <table class="premium-table">
            <thead>
                <tr>
                    <th>Date & Time</th>
                    <th>Patient</th>
                    <th>Doctor</th>
                    <th>Status</th>
                    <th class="text-right">Actions</th>
                </tr>
            </thead>
            <tbody v-if="!isLoading && myAppointments.length > 0">
                <tr v-for="apt in myAppointments" :key="apt.id" class="table-row">
                    <td>
                        <div class="time-cell">
                             <span class="time-main">{{ formatTime(apt.startTime) }}</span>
                             <span class="date-sub">{{ formatDate(apt.startTime) }}</span>
                        </div>
                    </td>
                    <td class="font-bold">{{ apt.patientName }}</td>
                    <td>Dr. {{ apt.doctorName }}<br><span class="text-xs text-secondary">{{ apt.specialty }}</span></td>
                    <td>
                        <span :class="['status-pill', apt.status.toLowerCase()]">{{ apt.status }}</span>
                    </td>
                    <td class="text-right">
                        <div class="actions">
                            <button v-if="canEdit(apt)" @click="openReschedule(apt)" class="btn-icon info" title="Reschedule">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                  <path stroke-linecap="round" stroke-linejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                                </svg>
                            </button>
                            <button v-if="canEdit(apt)" @click="cancelAppointment(apt.id)" class="btn-icon danger" title="Cancel">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </button>
                            <span v-else class="text-secondary text-xs">Locked</span>
                        </div>
                    </td>
                </tr>
            </tbody>
            <tbody v-else-if="isLoading">
                <tr><td colspan="5" class="text-center p-5">Loading...</td></tr>
            </tbody>
            <tbody v-else>
                <tr><td colspan="5" class="text-center p-5">No myAppointments found.</td></tr>
            </tbody>
        </table>
    </div>

    <ConfirmationModal
        :visible="confirmModalState.visible"
        :title="confirmModalState.title"
        :message="confirmModalState.message"
        :type="confirmModalState.type"
        confirmText="Yes, Cancel"
        @close="confirmModalState.visible = false"
        @confirm="confirmAction"
    />

    <TimeSlotModal
        v-if="showRescheduleModal"
        :visible="showRescheduleModal"
        :doctor="rescheduleData.doctor"
        :date="rescheduleData.date"
        :appointments="rescheduleData.slots"
        :emitOnly="true"
        @close="showRescheduleModal = false"
        @time-selected="handleRescheduleSelection"
    />
  </div>
</template>

<style scoped>
.page-container { max-width: 1000px; margin: 40px auto; padding: 0 20px; }
.page-container.embedded { margin: 0; padding: 0; max-width: none; }
.page-header { margin-bottom: 30px; }
.page-header h1 { font-size: 2rem; font-weight: 800; color: var(--text-primary); }
.page-header h1 span { color: var(--color-primary); }
.page-header p { color: var(--text-secondary); }

.content-card { background: var(--bg-card); border-radius: 0; border: 1px solid var(--border-default); overflow: hidden; box-shadow: var(--shadow-sm); }
.premium-table { width: 100%; border-collapse: collapse; }
.premium-table th { text-align: left; padding: 15px 20px; background: var(--bg-table-header); font-size: 0.75rem; text-transform: uppercase; color: var(--text-secondary); border-bottom: 1px solid var(--border-default); }
.table-row { border-bottom: 1px solid var(--border-default); }
.table-row td { padding: 15px 20px; vertical-align: middle; }

.time-cell { display: flex; flex-direction: column; }
.time-main { font-weight: 700; color: var(--text-primary); }
.date-sub { font-size: 0.8rem; color: var(--text-secondary); }

.status-pill { padding: 4px 10px; border-radius: 50px; font-size: 0.7rem; font-weight: 700; text-transform: uppercase; }
.status-pill.scheduled { background: var(--bg-status-warning); color: var(--status-warning-text); }
.status-pill.completed { background: var(--bg-status-success); color: var(--status-success-text); }
.status-pill.cancelled { background: var(--bg-status-error); color: var(--status-error-text); }

.btn-icon { width: 32px; height: 32px; border-radius: 8px; border: none; display: inline-flex; align-items: center; justify-content: center; cursor: pointer; transition: 0.2s; }
.btn-icon svg { width: 16px; height: 16px; }
.btn-icon.danger { background: #fee2e2; color: #ef4444; }
.btn-icon.info { background: #e0f2fe; color: #0ea5e9; margin-right: 8px; }
.btn-icon:hover { transform: scale(1.1); }
.actions { display: flex; justify-content: flex-end; }
</style>

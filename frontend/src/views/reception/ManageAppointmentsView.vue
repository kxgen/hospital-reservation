<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'
import { formatTime, formatDate, toLocalDateString } from '@/utils/dateFormatter'
import ConfirmationModal from '@/components/ConfirmationModal.vue'
import TimeSlotModal from '@/components/TimeSlotModal.vue'

const auth = useAuthStore()
const alertStore = useAlertStore()

// --- Tabs State ---
const activeTab = ref('Calendar') // Calendar, Today, MyBookings, Pending, History

// --- General State ---
const isLoading = ref(false)
const searchQuery = ref('')
const currentPage = ref(1)
const itemsPerPage = 8

// --- Schedule (Calendar) State ---
const allAppointments = ref([])
const selectedDate = ref(toLocalDateString(new Date()))
const today = toLocalDateString(new Date())
const currentStartDate = ref(new Date())
currentStartDate.value.setHours(0, 0, 0, 0)

// --- My Bookings State ---
const myAppointments = ref([])

// --- All Managed State ---
const managedAppointments = ref([])
const pendingAppointments = ref([])

// --- Modals State ---
const confirmModalState = ref({
    visible: false,
    title: '',
    message: '',
    type: 'info',
    onConfirm: null
})

const showTimeModal = ref(false)
const rescheduleData = ref({ appointmentId: null, doctor: null, date: null, slots: [] })

// --- Fetching Logic ---
const fetchSchedule = async () => {
  isLoading.value = true
  try {
    const res = await axios.get(`/api/receptionist/appointments/upcoming`, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    allAppointments.value = res.data || []
  } catch (err) {
    console.error('Failed to load schedule', err)
  } finally {
    isLoading.value = false
  }
}

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

const fetchManaged = async () => {
  isLoading.value = true
  try {
    const res = await axios.get(`/api/appointments/managed`, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    managedAppointments.value = res.data || []
  } catch (err) {
    console.error('Failed to load managed appointments', err)
  } finally {
    isLoading.value = false
  }
}

const fetchPending = async () => {
    isLoading.value = true
    try {
        const res = await axios.get(`/api/receptionist/appointments/pending`, {
            headers: { Authorization: `Bearer ${auth.token}` }
        })
        pendingAppointments.value = res.data || []
    } catch (err) {
        console.error('Failed to load pending appointments', err)
    } finally {
        isLoading.value = false
    }
}

const loadTabData = () => {
    if (activeTab.value === 'MyBookings') fetchMyBookings()
    else if (activeTab.value === 'History') fetchManaged()
    else if (activeTab.value === 'Pending') fetchPending()
    else fetchSchedule()
}

watch(activeTab, () => {
    currentPage.value = 1
    searchQuery.value = ''
    loadTabData()
})

// --- Calendar Logic ---
const weekDates = computed(() => {
  const dates = []
  const start = new Date(currentStartDate.value)
  for (let i = 0; i < 7; i++) {
    const d = new Date(start)
    d.setDate(start.getDate() + i)
    dates.push(d)
  }
  return dates
})

const nextWeek = () => {
    const newDate = new Date(currentStartDate.value)
    newDate.setDate(newDate.getDate() + 7)
    currentStartDate.value = newDate
}

const prevWeek = () => {
    const newDate = new Date(currentStartDate.value)
    newDate.setDate(newDate.getDate() - 7)
    currentStartDate.value = newDate
}

const selectDate = (date) => {
    selectedDate.value = toLocalDateString(date)
}

const formatDayName = (date) => date.toLocaleDateString("en-US", { weekday: "short" });
const formatDayNumber = (date) => date.getDate();

const getApptCountForDate = (date) => {
    const dStr = toLocalDateString(date)
    return allAppointments.value.filter(a => {
        const apptDate = toLocalDateString(a.startTime)
        const s = a.status.toLowerCase()
        return apptDate === dStr && (s === 'scheduled' || s === 'confirmed')
    }).length
}

const handleDateInput = (event) => {
  const selected = new Date(event.target.value)
  selected.setHours(0,0,0,0)
  currentStartDate.value = selected
  selectedDate.value = event.target.value
}

// --- Data Filtering ---
const currentDataList = computed(() => {
  let filtered = []
  const qStr = searchQuery.value.toLowerCase()

  if (activeTab.value === 'Calendar') {
    filtered = allAppointments.value.filter(a => toLocalDateString(a.startTime) === selectedDate.value)
  } else if (activeTab.value === 'Today') {
    filtered = allAppointments.value.filter(a => toLocalDateString(a.startTime) === today)
  } else if (activeTab.value === 'History') {
    // Show all managed bookings that are past or terminal status
    filtered = managedAppointments.value.filter(a => {
      const s = a.status.toLowerCase()
      const isPast = a.startTime < new Date().toISOString()
      return s === 'completed' || s === 'cancelled' || isPast
    }).sort((a,b) => new Date(b.startTime) - new Date(a.startTime)) // Ordered by recent
    return filtered
  } else if (activeTab.value === 'MyBookings') {
    // Show only active/future bookings made by current user
    filtered = myAppointments.value.filter(a => {
      const s = a.status.toLowerCase()
      const isFuture = a.startTime >= new Date().toISOString()
      return s !== 'cancelled' && s !== 'completed' && isFuture
    })
  } else if (activeTab.value === 'Pending') {
    filtered = pendingAppointments.value
  }

  if (qStr) {
    filtered = filtered.filter(a => 
        a.patientName?.toLowerCase().includes(qStr) || 
        a.doctorName?.toLowerCase().includes(qStr)
    )
  }

  return filtered.sort((a, b) => new Date(a.startTime) - new Date(b.startTime))
})

// --- Action Methods ---
const checkIn = async (id) => {
    try {
        await axios.post(`/api/receptionist/check-in`, { id }, {
            headers: { Authorization: `Bearer ${auth.token}` }
        })
        loadTabData()
        alertStore.showAlert('Patient checked in successfully.', 'success')
    } catch (err) {
        alertStore.showAlert('Check-in failed.', 'error')
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
                alertStore.showAlert('Appointment cancelled successfully.', 'success')
                loadTabData()
            } catch (err) {
                alertStore.showAlert('Failed to cancel appointment.', 'error')
            }
            confirmModalState.value.visible = false
        }
    }
}

const openReschedule = async (apt) => {
    try {
        isLoading.value = true
        const res = await axios.get(`/api/doctors/${apt.doctorId}`, {
             headers: { Authorization: `Bearer ${auth.token}` }
        })
        const doctor = res.data
        rescheduleData.value = {
            appointmentId: apt.id,
            doctor: { id: doctor.id, name: doctor.fullName, specialty: doctor.specialtyName || apt.specialty },
            date: new Date(),
            slots: doctor.timeslots || []
        }
        showTimeModal.value = true
    } catch (err) {
        alertStore.showAlert("Failed to load availability for rescheduling.", "error")
    } finally {
        isLoading.value = false
    }
}

const handleRescheduleSelection = async (slotData) => {
    try {
        await axios.put(`/api/appointments/${rescheduleData.value.appointmentId}/reschedule`, {
            NewStartTime: slotData.startTime,
            NewEndTime: slotData.endTime
        }, {
            headers: { Authorization: `Bearer ${auth.token}` }
        })
        alertStore.showAlert(`Rescheduled successfully to ${slotData.timeLabel}`, 'success')
        showTimeModal.value = false
        loadTabData()
    } catch (err) {
        alertStore.showAlert(err.response?.data || "Failed to reschedule.", "error")
    }
}

const markNoShow = async (id) => {
    confirmModalState.value = {
        visible: true,
        title: 'Mark as No-Show',
        message: 'Are you sure this patient did not arrive for the appointment?',
        type: 'warning',
        onConfirm: async () => {
             try {
                await axios.post(`/api/receptionist/appointments/${id}/no-show`, {}, {
                    headers: { Authorization: `Bearer ${auth.token}` }
                })
                alertStore.showAlert('Marked as no-show.', 'success')
                loadTabData()
            } catch (err) {
                alertStore.showAlert('Failed to update status.', 'error')
            }
            confirmModalState.value.visible = false
        }
    }
}

const canEdit = (apt) => {
    const s = apt.status.toLowerCase()
    const isTerminal = s === 'cancelled' || s === 'completed'
    if (isTerminal) return false

    // Receptionists can reschedule future OR pending (passed) appointments
    const aptDate = new Date(apt.startTime)
    const isFuture = aptDate > new Date()
    return isFuture || s === 'pending'
}

const paginatedList = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage
  return currentDataList.value.slice(start, start + itemsPerPage)
})

const totalPages = computed(() => Math.ceil(currentDataList.value.length / itemsPerPage))

onMounted(loadTabData)
</script>

<template>
  <div class="manage-container">
    <header class="page-header">
      <div class="header-content">
        <h1>Appointment <span>Manager</span></h1>
        <p>Coordinate patient flow, check-ins, and booking management in one place.</p>
        
        <div class="tab-system">
            <button v-for="t in ['Calendar', 'Today', 'MyBookings', 'Pending', 'History']" :key="t"
                :class="['main-tab', { active: activeTab === t }]" @click="activeTab = t">
                {{ t === 'MyBookings' ? 'My Bookings' : t }}
            </button>
        </div>
      </div>
    </header>

    <div class="main-content-view">
        <!-- CALENDAR NAVIGATION (Special for Calendar Tab) -->
        <div v-if="activeTab === 'Calendar'" class="calendar-nav animate-fade-in">
            <div class="week-controls">
                <button class="nav-arrow" @click="prevWeek">
                    <svg viewBox="0 0 24 24" width="20" height="20"><path fill="currentColor" d="M15.41,16.58L10.83,12L15.41,7.41L14,6L8,12L14,18L15.41,16.58Z"/></svg>
                </button>
                <span class="week-label">{{ formatDate(currentStartDate) }} - {{ formatDate(weekDates[6]) }}</span>
                <button class="nav-arrow" @click="nextWeek">
                    <svg viewBox="0 0 24 24" width="20" height="20"><path fill="currentColor" d="M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z"/></svg>
                </button>
            </div>

            <div class="dates-row">
                <button v-for="date in weekDates" :key="date.toISOString()"
                    class="date-card" :class="{ 'selected': selectedDate === toLocalDateString(date), 'has-dots': getApptCountForDate(date) > 0 }"
                    @click="selectDate(date)">
                    <span class="d-name">{{ formatDayName(date) }}</span>
                    <span class="d-num">{{ formatDayNumber(date) }}</span>
                    <span v-if="getApptCountForDate(date) > 0" class="appt-count">{{ getApptCountForDate(date) }}</span>
                </button>
            </div>
        </div>

        <div class="data-card">
            <table class="premium-table">
                <thead>
                    <tr>
                        <th>Time</th>
                        <th v-if="activeTab !== 'Calendar'">Date</th>
                        <th>Patient Name</th>
                        <th>Assigned Provider</th>
                        <th>Current Status</th>
                        <th class="text-right">Actions</th>
                    </tr>
                </thead>
                <tbody v-if="!isLoading && paginatedList.length > 0">
                    <tr v-for="apt in paginatedList" :key="apt.id" class="table-row">
                        <td>
                             <span class="t-main">{{ formatTime(apt.startTime) }}</span>
                        </td>
                        <td v-if="activeTab !== 'Calendar'">
                             <span class="n-main" style="color: var(--text-secondary); font-weight: 600;">{{ formatDate(apt.startTime) }}</span>
                        </td>
                        <td>
                             <span class="n-main">{{ apt.patientName }}</span>
                        </td>
                        <td>
                            <div class="dr-info">
                                <span class="dr-name">Dr. {{ apt.doctorName }}</span>
                                <span class="dr-dept">{{ apt.specialty }}</span>
                            </div>
                        </td>
                        <td>
                            <span :class="['status-box', apt.checkedInAt ? 'checkedin' : apt.status.toLowerCase()]">
                                {{ apt.checkedInAt ? 'Checked In' : apt.status }}
                            </span>
                        </td>
                        <td class="text-right">
                            <div class="op-btns">
                                <button v-if="activeTab === 'MyBookings' && canEdit(apt)" @click="openReschedule(apt)" class="btn-op info">Reschedule</button>
                                
                                <button v-if="activeTab === 'Pending' && canEdit(apt)" @click="openReschedule(apt)" class="btn-op info">Reschedule</button>

                                <button v-if="(activeTab === 'Today' || activeTab === 'Calendar' || activeTab === 'Pending') && (apt.status.toLowerCase() === 'scheduled' || apt.status.toLowerCase() === 'confirmed' || apt.status.toLowerCase() === 'pending') && !apt.checkedInAt"
                                    class="btn-action checkin" @click="checkIn(apt.id)">Check In</button>
                                
                                <button v-if="activeTab === 'Pending'" @click="markNoShow(apt.id)" class="btn-op danger">No-Show</button>

                                <button v-if="canEdit(apt)" @click="cancelAppointment(apt.id)" class="btn-op danger">Cancel</button>
                                
                                <span v-else class="locked-text">{{ apt.checkedInAt ? 'Presence Logged' : 'Closed' }}</span>
                            </div>
                        </td>
                    </tr>
                </tbody>
                <tbody v-else>
                    <tr><td :colspan="activeTab === 'Calendar' ? 5 : 6" class="empty-state">{{ isLoading ? 'Processing...' : 'No appointments found.' }}</td></tr>
                </tbody>
            </table>
        </div>
    </div>

    <!-- Pagination -->
    <footer class="pagi-footer" v-if="totalPages > 1">
        <button :disabled="currentPage === 1" @click="currentPage--" class="pg-btn">Previous</button>
        <span class="pg-info">Page {{ currentPage }} of {{ totalPages }}</span>
        <button :disabled="currentPage === totalPages" @click="currentPage++" class="pg-btn">Next</button>
    </footer>

    <ConfirmationModal
        :visible="confirmModalState.visible"
        :title="confirmModalState.title"
        :message="confirmModalState.message"
        :type="confirmModalState.type"
        confirmText="Yes, Proceed"
        @close="confirmModalState.visible = false"
        @confirm="confirmModalState.onConfirm"
    />

    <TimeSlotModal
        v-if="showTimeModal"
        :visible="showTimeModal"
        :doctor="rescheduleData.doctor"
        :date="rescheduleData.date"
        :appointments="rescheduleData.slots"
        :emitOnly="true"
        @close="showTimeModal = false"
        @time-selected="handleRescheduleSelection"
    />
  </div>
</template>

<style scoped>
.manage-container { max-width: 1400px; margin: 40px auto; padding: 0 40px; }
.page-header { margin-bottom: 40px; }

.header-content h1 { font-size: 2.5rem; font-weight: 800; color: var(--text-primary); margin: 0; }
.header-content h1 span { color: var(--color-primary); }
.header-content p { color: var(--text-secondary); margin-top: 8px; font-size: 1.1rem; max-width: 600px; }

/* Tabs */
.tab-system { display: flex; background: var(--bg-card); border: 1px solid var(--border-default); padding: 4px; border-radius: 0; margin-top: 25px; width: fit-content; }
.main-tab { padding: 10px 24px; border: none; background: none; font-weight: 800; color: var(--text-secondary); cursor: pointer; border-radius: 0; transition: 0.2s; white-space: nowrap; }
.main-tab.active { background: var(--color-primary); color: white; }

.search-main input { padding: 10px 18px; border: 1px solid var(--border-default); background: var(--bg-card); color: var(--text-primary); border-radius: 0; width: 350px; }

/* Calendar */
.calendar-nav { background: var(--bg-card); border: 1px solid var(--border-default); padding: 25px; margin-bottom: 30px; border-radius: 0; }
.week-controls { display: flex; align-items: center; justify-content: center; gap: 20px; margin-bottom: 20px; position: relative; }
.nav-arrow { background: var(--bg-body); border: 1px solid var(--border-default); width: 36px; height: 36px; cursor: pointer; display: flex; align-items: center; justify-content: center; border-radius: 0; }
.week-label { font-weight: 800; color: var(--text-primary); font-size: 1.1rem; }

.dates-row { display: grid; grid-template-columns: repeat(7, 1fr); gap: 10px; }
.date-card { padding: 15px; display: flex; flex-direction: column; align-items: center; background: var(--bg-input); border: 1px solid var(--border-default); cursor: pointer; transition: 0.2s; position: relative; border-radius: 0; }
.date-card.selected { background: var(--color-primary); border-color: var(--color-primary); color: white; }
.d-name { font-size: 0.7rem; font-weight: 800; text-transform: uppercase; opacity: 0.7; }
.d-num { font-size: 1.4rem; font-weight: 800; }
.appt-count { position: absolute; top: -5px; right: -5px; background: var(--color-primary); color: white; height: 20px; min-width: 20px; padding: 0 4px; display: flex; align-items: center; justify-content: center; font-size: 0.7rem; font-weight: 800; border: 2px solid white; border-radius: 0; }
.selected .appt-count { background: white; color: var(--color-primary); }

/* Table */
.data-card { background: var(--bg-card); border: 1px solid var(--border-default); border-radius: 0; overflow: hidden; }
.premium-table { width: 100%; border-collapse: collapse; }
.premium-table th { padding: 18px 25px; text-align: left; font-size: 0.75rem; text-transform: uppercase; background: var(--bg-table-header); border-bottom: 2px solid var(--border-default); color: var(--text-tertiary); }
.table-row { border-bottom: 1px solid var(--border-default); transition: 0.2s; }
.table-row:hover { background: var(--bg-input); }
.table-row td { padding: 20px 25px; vertical-align: middle; font-size: var(--table-font-size); }

.pt-info { display: flex; flex-direction: column; }
.t-main { font-weight: 800; font-size: var(--font-size-base); color: var(--color-primary); }
.n-main { font-weight: 700; color: var(--text-primary); font-size: var(--font-size-base); }

.dr-name { display: block; font-weight: 800; color: var(--text-primary); }
.dr-dept { font-size: 0.75rem; color: var(--text-secondary); font-weight: 600; }

.appt-details { display: flex; flex-direction: column; }
.dt-time { font-weight: 800; font-size: 1rem; color: var(--color-primary); }
.dt-info { font-size: 0.85rem; color: var(--text-secondary); font-weight: 600; }

.status-box { padding: 6px 14px; font-size: 0.95rem; font-weight: 800; text-transform: uppercase; border: 1px solid transparent; border-radius: 0; display: inline-block; }
.status-box.scheduled { border-color: #fbbf24; color: #b45309; background: #fffbeb; }
.status-box.checkedin { border-color: #10b981; color: #065f46; background: #ecfdf5; }
.status-box.cancelled { border-color: #ef4444; color: #991b1b; background: #fef2f2; }
.status-box.completed { border-color: #3b82f6; color: #1e40af; background: #eff6ff; }
.status-box.pending { border-color: #f97316; color: #c2410c; background: #fff7ed; }
.status-box.no_show { border-color: #6b7280; color: #374151; background: #f3f4f6; }

.btn-action { padding: 8px 16px; font-weight: 800; cursor: pointer; border: none; transition: 0.2s; border-radius: 0; }
.btn-action.checkin { background: var(--color-primary); color: white; }
.btn-action:hover { opacity: 0.9; }

.locked-text { font-size: 0.8rem; color: var(--text-tertiary); font-style: italic; font-weight: 600; }

.op-btns { display: flex; justify-content: flex-end; gap: 8px; }
.btn-op { padding: 8px 16px; font-weight: 800; font-size: 0.8rem; cursor: pointer; border-radius: 0; border: none; transition: 0.2s; }
.btn-op:hover { opacity: 0.8; }
.btn-op.info { background: #e0f2fe; color: #0369a1; border: 1px solid #bae6fd; }
.btn-op.danger { background: #fff5f5; color: #c53030; border: 1px solid #fc8181; }

.pagi-footer { margin-top: 30px; display: flex; align-items: center; justify-content: center; gap: 20px; }
.pg-btn { padding: 8px 16px; border: 1px solid var(--border-default); background: var(--bg-card); font-weight: 800; cursor: pointer; border-radius: 0; }
.pg-info { font-weight: 700; color: var(--text-secondary); }

.empty-state { text-align: center; padding: 60px; color: var(--text-tertiary); font-style: italic; }
.bold { font-weight: 800; color: var(--text-primary); }
.text-right { text-align: right; }
</style>

<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from '@/api/axios'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'
import { formatTime, formatDate, toLocalDateString } from '@/utils/dateFormatter'

const auth = useAuthStore()
const alertStore = useAlertStore()
const router = useRouter()

// 1. Data State
const allAppointments = ref([])
const isLoading = ref(false)
const activeTab = ref('Today') // Today, Upcoming, History
const selectedDate = ref(toLocalDateString(new Date()))
const today = toLocalDateString(new Date())

// 2. Pagination State
const currentPage = ref(1)
const itemsPerPage = 8

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

const currentStartDate = ref(new Date())
currentStartDate.value.setHours(0, 0, 0, 0)

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
    const todayStart = new Date()
    todayStart.setHours(0, 0, 0, 0)
    currentStartDate.value = newDate
}

const selectDate = (date) => {
    selectedDate.value = toLocalDateString(date)
    currentPage.value = 1
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
  selectedDate.value = event.target.value
  currentPage.value = 1
  const selected = new Date(event.target.value)
  selected.setHours(0,0,0,0)
  currentStartDate.value = selected
}

const filteredAppointments = computed(() => {
  let filtered = []

  if (activeTab.value === 'Today') {
    filtered = allAppointments.value.filter(a => toLocalDateString(a.startTime) === today)
  } else if (activeTab.value === 'Upcoming') {
    filtered = allAppointments.value.filter(a => {
        const appointmentDate = toLocalDateString(a.startTime)
        return appointmentDate === selectedDate.value
    })
  } else if (activeTab.value === 'History') {
    filtered = allAppointments.value.filter(a => {
      const s = a.status.toLowerCase()
      const isPast = a.startTime < new Date().toISOString()
      return s === 'completed' || s === 'cancelled' || isPast
    })
  }

  return filtered.sort((a, b) => new Date(a.startTime) - new Date(b.startTime))
})

const paginatedAppointments = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage
  const end = start + itemsPerPage
  return filteredAppointments.value.slice(start, end)
})

const totalPages = computed(() => Math.ceil(filteredAppointments.value.length / itemsPerPage))

const setTab = (tab) => {
  activeTab.value = tab
  currentPage.value = 1
  if (tab === 'Today') selectedDate.value = today
}

const checkIn = async (id) => {
    try {
        await axios.post(`/api/receptionist/check-in`, { id }, {
            headers: { Authorization: `Bearer ${auth.token}` }
        })
        const apt = allAppointments.value.find(a => a.id === id)
        if (apt) {
            apt.checkedInAt = new Date().toISOString()
        }
        alertStore.showAlert('Patient checked in successfully.', 'success')
    } catch (err) {
        alertStore.showAlert(err.response?.data || 'Check-in failed.', 'error')
    }
}

onMounted(fetchSchedule)
</script>

<template>
  <div class="reception-container">
    <header class="page-header">
      <div class="header-content">
        <h1>Master <span>Schedule</span></h1>
        <p>Coordinate patient flow and manage arrivals for all clinical departments.</p>
      </div>
      
      <div class="header-tabs">
        <button 
          v-for="tab in ['Today', 'Upcoming', 'History']" 
          :key="tab"
          :class="['tab-btn', { active: activeTab === tab }]"
          @click="setTab(tab)"
        >
          {{ tab }}
          <span class="tab-count" v-if="allAppointments.length > 0">
             {{ allAppointments.filter(a => {
                 if (tab === 'Today') return toLocalDateString(a.startTime) === today;
                 if (tab === 'Upcoming') return (a.status.toLowerCase() === 'scheduled' || a.status.toLowerCase() === 'confirmed') && toLocalDateString(a.startTime) >= today;
                 if (tab === 'History') return a.status.toLowerCase() === 'completed' || a.status.toLowerCase() === 'cancelled' || toLocalDateString(a.startTime) < today;
                 return false;
             }).length }}
          </span>
        </button>
      </div>
    </header>

    <!-- Calendar Selector (shown for Upcoming) -->
    <div class="calendar-nav-container animate-fade-in" v-if="activeTab === 'Upcoming'">
      <div class="week-nav-controls">
        <button class="week-arrow" @click="prevWeek">
          <svg viewBox="0 0 24 24" width="20" height="20"><path fill="currentColor" d="M15.41,16.58L10.83,12L15.41,7.41L14,6L8,12L14,18L15.41,16.58Z"/></svg>
        </button>
        <span class="week-label">{{ formatDate(currentStartDate) }} - {{ formatDate(weekDates[6]) }}</span>
        <button class="week-arrow" @click="nextWeek">
          <svg viewBox="0 0 24 24" width="20" height="20"><path fill="currentColor" d="M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z"/></svg>
        </button>

        <div class="date-picker-mini">
          <input type="date" :value="selectedDate" @input="handleDateInput" class="hidden-date-input" id="cal-picker" />
          <label for="cal-picker" class="cal-icon-btn">
            <svg viewBox="0 0 24 24" width="20" height="20"><path fill="currentColor" d="M19,19H5V8H19M16,1V3H8V1H6V3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.9 20.1,3 19,3H18V1M17,12H12V17H17V12Z"/></svg>
          </label>
        </div>
      </div>

      <div class="dates-grid">
        <button
          v-for="date in weekDates"
          :key="date.toISOString()"
          class="date-cell"
          :class="{
            'selected': selectedDate === toLocalDateString(date),
            'has-appts': getApptCountForDate(date) > 0
          }"
          @click="selectDate(date)"
        >
          <span class="day-name">{{ formatDayName(date) }}</span>
          <span class="day-num">{{ formatDayNumber(date) }}</span>
          <span v-if="getApptCountForDate(date) > 0" class="slot-indicator">
            {{ getApptCountForDate(date) }}
          </span>
        </button>
      </div>
    </div>

    <div class="content-card">
      <div class="table-container">
        <table class="premium-table">
          <thead>
            <tr>
              <th>Arrival Time</th>
              <th>Patient Identity</th>
              <th>Assigned Provider</th>
              <th>Current Status</th>
              <th class="text-right">Desk Action</th>
            </tr>
          </thead>
          <tbody v-if="!isLoading && paginatedAppointments.length > 0">
            <tr v-for="apt in paginatedAppointments" :key="apt.id" class="table-row">
              <td>
                <div class="time-cell">
                  <span class="time-main">{{ formatTime(apt.startTime) }}</span>
                  <span class="date-sub">{{ formatDate(apt.startTime) }}</span>
                </div>
              </td>
              <td>
                <div class="p-identity">
                    <span class="p-name">{{ apt.patientName || 'Anonymous' }}</span>
                    <span class="p-id">ID: #{{ apt.patientDbId || '?' }}</span>
                </div>
              </td>
              <td>
                <div class="provider-info">
                    <span class="doc-name">Dr. {{ apt.doctorName }}</span>
                    <span class="dept-tag">{{ apt.specialty || 'OPD' }}</span>
                </div>
              </td>
              <td>
                <span :class="['status-pill', apt.checkedInAt ? 'inprogress' : apt.status.toLowerCase()]">
                  <span class="status-dot"></span>
                  {{ apt.checkedInAt ? 'Checked In' : apt.status }}
                </span>
              </td>
              <td class="text-right">
                <button 
                    v-if="(apt.status === 'scheduled' || apt.status === 'confirmed') && toLocalDateString(apt.startTime) === today && !apt.checkedInAt" 
                    class="btn-checkin" 
                    @click="checkIn(apt.id)"
                >
                    Mark Arrival
                </button>
                <span v-else class="action-locked">
                  {{ apt.checkedInAt ? 'Presence Logged' : 'Post-Dated' }}
                </span>
              </td>
            </tr>
          </tbody>
          <tbody v-else-if="isLoading">
            <tr><td colspan="5" class="empty-cell"><div class="loading-spinner"></div></td></tr>
          </tbody>
          <tbody v-else>
            <tr><td colspan="5" class="empty-cell">No appointments found for this selection.</td></tr>
          </tbody>
        </table>
      </div>

      <div class="table-footer" v-if="totalPages > 1">
        <div class="pagination-info">
          Schedule view: {{ ((currentPage - 1) * itemsPerPage) + 1 }} - {{ Math.min(currentPage * itemsPerPage, filteredAppointments.length) }} of {{ filteredAppointments.length }}
        </div>
        <div class="pagination-controls">
          <button :disabled="currentPage === 1" @click="currentPage--" class="pg-btn">Previous</button>
          <button v-for="p in totalPages" :key="p" :class="['pg-num', { active: currentPage === p }]" @click="currentPage = p">{{ p }}</button>
          <button :disabled="currentPage === totalPages" @click="currentPage++" class="pg-btn">Next</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.reception-container {
  max-width: 1200px;
  margin: 40px auto;
  padding: 0 20px;
}

.page-header {
  margin-bottom: 40px;
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
}

.header-content h1 { font-size: 2.4rem; font-weight: 800; color: var(--text-primary); margin: 0; }
.header-content h1 span { color: var(--color-primary); }
.header-content p { color: var(--text-secondary); margin-top: 5px; font-size: 1.1rem; }

.header-tabs {
  display: flex;
  background: var(--bg-card);
  padding: 6px;
  border-radius: 14px;
  border: 1px solid var(--border-default);
}

.tab-btn {
  padding: 10px 20px;
  border: none;
  background: none;
  border-radius: 10px;
  font-weight: 700;
  color: var(--text-secondary);
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  gap: 10px;
}
.tab-btn.active { background: var(--color-primary); color: white; }
.tab-count { background: rgba(0,0,0,0.1); padding: 2px 7px; border-radius: 20px; font-size: 0.7rem; }
.active .tab-count { background: rgba(255,255,255,0.2); }

/* Calendar */
.calendar-nav-container {
  margin-bottom: 30px;
  background: var(--bg-card);
  border-radius: 0;
  border: 1px solid var(--border-default);
  padding: 24px;
}
.week-nav-controls { display: flex; align-items: center; justify-content: center; gap: 20px; margin-bottom: 20px; position: relative; }
.week-arrow { background: var(--bg-body); border: 1px solid var(--border-default); width: 36px; height: 36px; border-radius: 10px; cursor: pointer; display: flex; align-items: center; justify-content: center; }
.week-label { font-weight: 800; color: var(--text-primary); font-size: 1.1rem; }
.date-picker-mini { position: absolute; right: 0; top: 50%; transform: translateY(-50%); }
.hidden-date-input { position: absolute; opacity: 0; pointer-events: none; }
.cal-icon-btn { width: 38px; height: 38px; border-radius: 10px; border: 1.5px solid var(--border-default); color: var(--color-primary); display: flex; align-items: center; justify-content: center; cursor: pointer; }

.dates-grid { display: grid; grid-template-columns: repeat(7, 1fr); gap: 10px; }
.date-cell { height: 3.5rem; display: flex; flex-direction: column; align-items: center; justify-content: center; background: var(--bg-input); border: 2px solid var(--border-default); border-radius: 12px; cursor: pointer; transition: all 0.2s; position: relative; }
.date-cell.selected { background: var(--color-primary); color: white; border-color: var(--color-primary); }
.day-name { font-size: 0.65rem; font-weight: 700; text-transform: uppercase; opacity: 0.7; }
.day-num { font-size: 1.2rem; font-weight: 800; }
.slot-indicator { position: absolute; top: -5px; right: -5px; background: var(--color-primary); color: white; width: 18px; height: 18px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 0.65rem; border: 2px solid white; }
.selected .slot-indicator { background: white; color: var(--color-primary); }

/* Table */
.content-card { background: var(--bg-card); border-radius: 0; border: 1px solid var(--border-default); overflow: hidden; box-shadow: var(--shadow-sm); }
.premium-table { width: 100%; border-collapse: collapse; }
.premium-table th { padding: 20px 25px; text-align: left; font-size: 0.75rem; text-transform: uppercase; color: var(--text-tertiary); background: var(--bg-table-header); border-bottom: 1px solid var(--border-default); }
.table-row { border-bottom: 1px solid var(--border-default); transition: 0.2s; }
.table-row:hover { background: var(--color-primary-light); }
.table-row td { padding: 20px 25px; vertical-align: middle; }

.time-cell { display: flex; flex-direction: column; }
.time-main { font-weight: 800; font-size: 1.1rem; color: var(--text-primary); }
.date-sub { font-size: 0.8rem; color: var(--text-secondary); }

.p-name { display: block; font-weight: 700; color: var(--text-primary); font-size: inherit; }
.p-id { font-size: 0.75rem; color: var(--color-primary); font-weight: 600; }

.doc-name { display: block; font-weight: 700; }
.dept-tag { font-size: 0.7rem; background: var(--bg-input); padding: 2px 6px; border-radius: 4px; font-weight: 600; color: var(--text-secondary); }

.status-pill { display: inline-flex; align-items: center; gap: 8px; border-radius: 0; }
.status-pill.scheduled, .status-pill.confirmed { background: var(--bg-warning); color: var(--text-warning); }
.status-pill.inprogress { background: var(--bg-success); color: var(--text-success); }
.status-pill.completed { background: #e0f2fe; color: #0369a1; }
.status-dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }

.btn-checkin { background: var(--color-primary); color: white; border: none; padding: 8px 16px; border-radius: 8px; font-weight: 700; cursor: pointer; transition: 0.2s; }
.btn-checkin:hover { opacity: 0.9; transform: translateY(-1px); }
.action-locked { font-size: 0.8rem; color: var(--text-secondary); font-style: italic; font-weight: 600; }

.table-footer { padding: 20px 25px; display: flex; justify-content: space-between; align-items: center; background: var(--bg-table-header); }
.pg-btn { padding: 6px 12px; border: 1px solid var(--border-default); background: white; border-radius: 6px; cursor: pointer; font-weight: 700; }
.pg-num { width: 32px; height: 32px; border: 1px solid var(--border-default); background: white; border-radius: 6px; cursor: pointer; font-weight: 700; margin: 0 2px; }
.pg-num.active { background: var(--color-primary); color: white; }

.empty-cell { text-align: center; padding: 60px; color: var(--text-tertiary); }
.loading-spinner { width: 30px; height: 30px; border: 3px solid var(--border-default); border-top-color: var(--color-primary); border-radius: 50%; animation: spin 1s linear infinite; margin: 0 auto; }
@keyframes spin { to { transform: rotate(360deg); } }

@media (max-width: 900px) {
    .page-header { flex-direction: column; align-items: flex-start; gap: 20px; }
    .header-tabs { width: 100%; }
    .tab-btn { flex: 1; justify-content: center; }
}
</style>
<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from '@/api/axios'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { formatTime, formatDate, toLocalDateString } from '@/utils/dateFormatter'

const auth = useAuthStore()
const router = useRouter()

// 1. Data State
const allAppointments = ref([])
const isLoading = ref(false)
const activeTab = ref('Upcoming') // Upcoming, Today, History
const selectedDate = ref(toLocalDateString(new Date()))
const today = toLocalDateString(new Date())

// 2. Pagination State
const currentPage = ref(1)
const itemsPerPage = 8

// 3. Navigation
const openDetails = (apt) => {
  router.push({ name: 'doctor-appointment-detail', params: { id: apt.id } })
}

const fetchSchedule = async () => {
  isLoading.value = true
  try {
    const res = await axios.get(`/api/appointments/doctor-schedule`, {
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
    if (newDate < todayStart) {
        currentStartDate.value = todayStart
    } else {
        currentStartDate.value = newDate
    }
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
        return apptDate === dStr && (a.status.toLowerCase() === 'scheduled' || a.status.toLowerCase() === 'confirmed')
    }).length
}

const isCurrentWeek = computed(() => {
    const todayStart = new Date()
    todayStart.setHours(0, 0, 0, 0)
    return currentStartDate.value.getTime() === todayStart.getTime()
})

const handleDateInput = (event) => {
  selectedDate.value = event.target.value
  currentPage.value = 1
  
  // Also update currentStartDate to show the week containing the selected date
  const selected = new Date(event.target.value)
  selected.setHours(0,0,0,0)
  currentStartDate.value = selected
}

const filteredAppointments = computed(() => {
  let filtered = []

  if (activeTab.value === 'Today') {
    filtered = allAppointments.value.filter(a => toLocalDateString(a.startTime) === today)
  } else if (activeTab.value === 'Upcoming') {
    // Show appointments for the selected date
    filtered = allAppointments.value.filter(a => {
        const appointmentDate = toLocalDateString(a.startTime)
        const status = a.status.toLowerCase()
        return appointmentDate === selectedDate.value && (status === 'scheduled' || status === 'confirmed' || status === 'pending')
    })
  } else if (activeTab.value === 'History') {
    filtered = allAppointments.value.filter(a => {
      const s = a.status.toLowerCase()
      return s === 'completed' || s === 'cancelled' || s === 'pending'
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

onMounted(fetchSchedule)
</script>

<template>
  <div class="doctor-container">
    <div class="page-header">
      <div class="header-content">
        <h1>Consultation <span>Schedule</span></h1>
        <p>Manage your patient queue and review medical histories.</p>
      </div>
      
      <div class="header-actions">
        <div class="header-tabs">
          <button 
            v-for="tab in ['Upcoming', 'Today', 'History']" 
            :key="tab"
            :class="['tab-btn', { active: activeTab === tab }]"
            @click="setTab(tab)"
          >
            {{ tab }}
            <span class="tab-count" v-if="allAppointments.length > 0">
               {{ allAppointments.filter(a => {
                   if (tab === 'Today') return toLocalDateString(a.startTime) === today;
                   if (tab === 'Upcoming') return (a.status.toLowerCase() === 'scheduled' || a.status.toLowerCase() === 'confirmed') && toLocalDateString(a.startTime) >= today;
                   if (tab === 'History') return a.status.toLowerCase() === 'completed' || a.status.toLowerCase() === 'cancelled';
                   return false;
               }).length }}
            </span>
          </button>
        </div>
      </div>
    </div>

    <!-- Custom Date Navigator (7-Day Grid) -->
    <div class="calendar-nav-container animate-fade-in" v-if="activeTab === 'Upcoming'">
      <div class="week-nav-controls">
        <button class="week-arrow" @click="prevWeek" :disabled="isCurrentWeek">
          <svg viewBox="0 0 24 24" width="20" height="20"><path fill="currentColor" d="M15.41,16.58L10.83,12L15.41,7.41L14,6L8,12L14,18L15.41,16.58Z"/></svg>
        </button>
        <span class="week-label">{{ formatDate(currentStartDate) }} - {{ formatDate(weekDates[6]) }}</span>
        <button class="week-arrow" @click="nextWeek">
          <svg viewBox="0 0 24 24" width="20" height="20"><path fill="currentColor" d="M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z"/></svg>
        </button>

        <div class="date-picker-mini">
          <input type="date" :min="today" :value="selectedDate" @input="handleDateInput" class="hidden-date-input" id="cal-picker" />
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
              <th>Time & Date</th>
              <th>Patient</th>
              <th>Visit Type</th>
              <th>Status</th>
              <th class="text-right">Actions</th>
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
                <div class="patient-cell">
                  <div class="patient-meta">
                    <span class="p-name">{{ apt.patientName || 'Anonymous' }}</span>
                  </div>
                </div>
              </td>
              <td>
                <span :class="['type-tag', apt.appointmentType?.toLowerCase()]">
                  {{ apt.appointmentType || 'New' }}
                </span>
              </td>
              <td>
                <span :class="['status-pill', apt.status.toLowerCase()]">
                  <span class="status-dot"></span>
                  {{ apt.status }}
                </span>
              </td>
              <td class="text-right">
                <button class="btn-detail" @click="openDetails(apt)">View Details</button>
              </td>
            </tr>
          </tbody>
          <tbody v-else-if="isLoading">
            <tr><td colspan="5" class="empty-cell"><div class="loading-spinner"></div></td></tr>
          </tbody>
          <tbody v-else>
            <tr><td colspan="5" class="empty-cell">No appointments found in this category.</td></tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination Footer -->
      <div class="table-footer" v-if="totalPages > 1">
        <div class="pagination-info">
          Showing {{ ((currentPage - 1) * itemsPerPage) + 1 }} - {{ Math.min(currentPage * itemsPerPage, filteredAppointments.length) }} of {{ filteredAppointments.length }}
        </div>
        <div class="pagination-controls">
          <button 
            :disabled="currentPage === 1" 
            @click="currentPage--"
            class="pg-btn"
          >Previous</button>
          
          <button 
            v-for="p in totalPages" 
            :key="p"
            :class="['pg-num', { active: currentPage === p }]"
            @click="currentPage = p"
          >{{ p }}</button>

          <button 
            :disabled="currentPage === totalPages" 
            @click="currentPage++"
            class="pg-btn"
          >Next</button>
        </div>
      </div>
    </div>

    <!-- Details Modal replaced by full page navigation -->
  </div>
</template>

<style scoped>
.doctor-container {
  padding: 0 20px;
}

/* Header & Tabs */
.page-header {
  margin-bottom: 40px;
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
}

.header-content h1 { font-size: 2.2rem; font-weight: 800; color: var(--text-primary); margin: 0; }
.header-content h1 span { color: var(--color-primary); }
.header-content p { color: var(--text-secondary); margin-top: 8px; font-size: 1.1rem; }

.header-tabs {
  display: flex;
  background: var(--bg-input);
  padding: 4px;
  border-radius: 0;
  border: 1px solid var(--border-default);
}

.tab-btn {
  padding: 10px 20px;
  border: none;
  background: none;
  border-radius: 0;
  font-weight: 700;
  color: var(--text-secondary);
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  gap: 10px;
}

.tab-btn:hover { background: var(--bg-body); }
.tab-btn.active { background: var(--color-primary); color: white; }

.tab-count {
  background: rgba(0,0,0,0.1);
  padding: 2px 8px;
  border-radius: 20px;
  font-size: 0.7rem;
}
.tab-btn.active .tab-count { background: rgba(255,255,255,0.2); }

/* Custom Date Picker Styles */
.calendar-nav-container {
  margin-bottom: 30px;
  background: var(--bg-card);
  border-radius: 0;
  border: 1px solid var(--border-default);
  padding: 24px;
  box-shadow: var(--shadow-sm);
}

.week-nav-controls {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 20px;
  margin-bottom: 20px;
  position: relative;
}

.week-arrow {
  background: var(--bg-body);
  border: 1px solid var(--border-default);
  width: 36px;
  height: 36px;
  border-radius: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: 0.2s;
  color: var(--text-primary);
}

.week-arrow:hover:not(:disabled) {
  background: var(--color-primary-light);
  color: var(--color-primary);
  border-color: var(--color-primary-border);
}

.week-arrow:disabled { opacity: 0.3; cursor: not-allowed; }

.week-label {
  font-weight: 800;
  color: var(--text-primary);
  font-size: 1.1rem;
  letter-spacing: -0.5px;
}

.date-picker-mini {
  position: absolute;
  right: 0;
  top: 50%;
  transform: translateY(-50%);
}

.hidden-date-input {
  position: absolute;
  opacity: 0;
  pointer-events: none;
}

.cal-icon-btn {
  width: 38px;
  height: 38px;
  border-radius: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--bg-card);
  border: 1.5px solid var(--border-default);
  color: var(--color-primary);
  cursor: pointer;
  transition: 0.2s;
}

.cal-icon-btn:hover { 
  background: var(--color-primary-light); 
  border-color: var(--color-primary); 
  transform: scale(1.05);
}

/* Dates Grid Styles (Matching DoctorCard) */
.dates-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 0.75rem;
}

.date-cell {
  height: 3.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: var(--bg-body);
  border: 1.5px solid var(--border-default);
  border-radius: 0;
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  padding: 2px;
  position: relative;
}

.date-cell:hover {
  border-color: var(--color-primary-border);
  background: var(--color-primary-light);
  transform: translateY(-2px);
}

.date-cell.selected {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: white;
  box-shadow: 0 8px 16px rgba(0, 179, 158, 0.2);
}

.date-cell.has-appts:not(.selected) {
    border-color: var(--color-primary-border);
}

.day-name {
  font-size: 0.65rem;
  font-weight: 700;
  color: var(--text-secondary);
  text-transform: uppercase;
  margin-bottom: 2px;
}

.date-cell.selected .day-name { color: rgba(255,255,255,0.8); }

.day-num {
  font-size: 1.2rem;
  font-weight: 800;
  color: var(--text-primary);
}

.date-cell.selected .day-num { color: white; }

.slot-indicator {
  position: absolute;
  top: -6px;
  right: -6px;
  width: 18px;
  height: 18px;
  background: var(--color-primary);
  color: white;
  border-radius: 50%;
  font-size: 0.65rem;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px solid var(--bg-card);
}

.date-cell.selected .slot-indicator {
  background: white;
  color: var(--color-primary);
}

@media (max-width: 768px) {
  .dates-grid {
    grid-template-columns: repeat(4, 1fr);
  }
}

@media (max-width: 480px) {
  .dates-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

/* Table Styling */
.content-card {
  background: var(--bg-card);
  border-radius: 20px;
  border: 1px solid var(--border-default);
  box-shadow: var(--shadow-md);
  overflow: hidden;
}

.premium-table { width: 100%; border-collapse: collapse; }
.premium-table th {
  padding: 20px 25px;
  text-align: left;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--text-tertiary);
  border-bottom: 1px solid var(--border-default);
  background: var(--bg-table-header);
}

.table-row { border-bottom: 1px solid var(--border-default); transition: all 0.2s; }
.table-row:hover { background: var(--color-primary-light); }

.table-row td { padding: 20px 25px; vertical-align: middle; }

/* Cell Content */
.time-cell { display: flex; flex-direction: column; }
.time-main { font-weight: 800; color: var(--text-primary); font-size: 1.1rem; }
.date-sub { font-size: 0.8rem; color: var(--text-secondary); font-weight: 600; }

.patient-cell { display: flex; align-items: center; gap: 15px; }
.avatar-circle {
  width: 40px; height: 40px; border-radius: 50%;
  background: var(--bg-input); color: var(--text-secondary);
  display: flex; align-items: center; justify-content: center;
  font-weight: 800; font-size: 1.1rem;
}
.p-name { display: block; font-weight: 700; color: var(--text-primary); font-size: 1rem; }

.reason-cell { display: flex; flex-direction: column; gap: 5px; }
.type-tag { 
  display: inline-block; width: fit-content; font-size: 0.65rem; font-weight: 900; 
  padding: 2px 8px; border-radius: 4px; text-transform: uppercase; letter-spacing: 0.5px;
}
.type-tag.new { background: #e0f2fe; color: #0369a1; }
.type-tag.follow-up { background: #fef3c7; color: #92400e; }
.reason-text { font-size: 0.9rem; color: var(--text-primary); font-weight: 600; }

/* Status Pills */
.status-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 6px 14px;
  border-radius: 50px;
  font-size: 0.7rem;
  font-weight: 800;
  text-transform: uppercase;
}
.status-dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }

.status-pill.confirmed, .status-pill.scheduled { background: var(--bg-status-warning); color: var(--status-warning-text); }
.status-pill.completed { background: var(--bg-status-success); color: var(--status-success-text); }
.status-pill.cancelled { background: var(--bg-status-error); color: var(--status-error-text); }
.status-pill.pending { background: var(--bg-status-info); color: var(--status-info-text); }

.btn-detail {
  background: var(--bg-card);
  border: 1px solid var(--color-primary);
  color: var(--color-primary);
  padding: 8px 16px;
  border-radius: 0;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-detail:hover { background: var(--color-primary); color: white; }

/* Pagination Footer */
.table-footer {
  padding: 20px 25px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: var(--bg-body);
}

.pagination-info { font-size: 0.9rem; color: var(--text-secondary); font-weight: 600; }
.pagination-controls { display: flex; gap: 8px; }

.pg-num {
  width: 36px; height: 36px; border: 1px solid var(--border-default);
  background: var(--bg-card); border-radius: 0;
  cursor: pointer; font-weight: 700; color: var(--text-secondary);
}
.pg-num.active { background: var(--color-primary); color: white; border-color: var(--color-primary); }

.pg-btn {
  padding: 0 15px; border: 1px solid var(--border-default);
  background: var(--bg-card); border-radius: 0;
  cursor: pointer; font-weight: 700; color: var(--text-secondary);
}
.pg-btn:disabled { opacity: 0.5; cursor: not-allowed; }

.empty-cell { text-align: center; padding: 60px; color: var(--text-tertiary); font-style: italic; }
.loading-spinner { width: 30px; height: 30px; border: 3px solid var(--border-default); border-top-color: var(--color-primary); border-radius: 50%; animation: spin 1s linear infinite; margin: 0 auto; }
@keyframes spin { to { transform: rotate(360deg); } }

@media (max-width: 900px) {
  .page-header { flex-direction: column; align-items: flex-start; gap: 20px; }
  .header-tabs { width: 100%; border-radius: 0; }
  .tab-btn { flex: 1; }
}
</style>
<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import TimeSlotModal from '@/components/TimeSlotModal.vue'

const auth = useAuthStore()

defineProps({
  isEmbedded: { type: Boolean, default: false }
})

// --- Data ---
const doctors = ref([])
const specialties = ref([])
const loading = ref(true)
const searchQuery = ref('')
const selectedSpecialty = ref('')

// --- Date Logic (For the 7-day grid) ---
const today = new Date()
today.setHours(0, 0, 0, 0)
const currentStartDate = ref(today)

const nextWeek = () => {
  const newDate = new Date(currentStartDate.value)
  newDate.setDate(newDate.getDate() + 7)
  currentStartDate.value = newDate
}

const prevWeek = () => {
  const newDate = new Date(currentStartDate.value)
  newDate.setDate(newDate.getDate() - 7)
  currentStartDate.value = newDate < today ? today : newDate
}

const weekDates = computed(() => {
  const dates = []
  const start = new Date(currentStartDate.value)
  for (let i = 0; i < 7; i++) {
    const date = new Date(start)
    date.setDate(start.getDate() + i)
    dates.push(date)
  }
  return dates
})

const weekRange = computed(() => {
  const start = currentStartDate.value
  const end = new Date(start)
  end.setDate(start.getDate() + 6)
  const options = { month: 'short', day: 'numeric' }
  return `${start.toLocaleDateString('en-US', options)} - ${end.toLocaleDateString('en-US', options)}`
})

// --- Fetching ---
const fetchData = async () => {
  loading.value = true
  try {
    const [docsRes, specsRes] = await Promise.all([
      axios.get('/api/doctors', {
        headers: { Authorization: `Bearer ${auth.token}` }
      }),
      axios.get('/api/doctors/specialties')
    ])
    
    doctors.value = docsRes.data.map(doc => ({
      id: doc.id,
      name: doc.fullName,
      specialty: doc.specialtyName,
      timeslots: doc.timeslots || [],
      gender: doc.gender || 'unknown'
    }))
    specialties.value = specsRes.data.map(s => s.name)
  } catch (err) {
    console.error('Failed to load booking data', err)
  } finally {
    loading.value = false
  }
}

// --- Filtering ---
const filteredDoctors = computed(() => {
  let list = doctors.value
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    list = list.filter(d => 
      (d.name && d.name.toLowerCase().includes(q)) || 
      (d.specialty && d.specialty.toLowerCase().includes(q))
    )
  }
  if (selectedSpecialty.value) {
    list = list.filter(d => d.specialty === selectedSpecialty.value)
  }
  return list
})

onMounted(fetchData)

// --- Modal State ---
const showModal = ref(false)
const activeDoctor = ref(null)
const activeDate = ref(null)

const openBookingModal = (doctor, date) => {
  activeDoctor.value = doctor
  activeDate.value = date
  showModal.value = true
}

const handleSuccess = () => {
  fetchData() // Refresh data if needed
}

// --- Helper for Date Key ---
const formatLocalDateKey = (date) => {
    if (!date) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
};

const getSlotsCount = (doctor, date) => {
  if (!doctor || !doctor.timeslots || !date) return 0
  const dKey = formatLocalDateKey(date)
  return doctor.timeslots.filter(s => {
    const sTime = s.startTime || s.StartTime
    if (!sTime) return false
    const isAvail = s.isAvailable !== undefined ? s.isAvailable : s.IsAvailable
    const sDate = new Date(sTime)
    return formatLocalDateKey(sDate) === dKey && isAvail
  }).length
}
</script>

<template>
  <div class="booking-page">
    <div v-if="!isEmbedded" class="page-header">
      <div class="header-content">
        <h1>Book <span>Appointment</span></h1>
        <p>Select a specialist and available time slot to book a new visit.</p>
      </div>
    </div>

    <div class="search-and-nav shadow-sm">
      <div class="search-group">
        <input v-model="searchQuery" type="text" placeholder="Search doctor or specialty..." class="search-input" />
        <select v-model="selectedSpecialty" class="spec-select">
          <option value="">All Specialties</option>
          <option v-for="s in specialties" :key="s" :value="s">{{ s }}</option>
        </select>
      </div>

      <div class="nav-group">
        <button class="nav-btn" @click="prevWeek" :disabled="currentStartDate <= today">
           <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M15 19l-7-7 7-7" />
          </svg>
        </button>
        <div class="date-range">
          <span class="range-val">{{ weekRange }}</span>
        </div>
        <button class="nav-btn" @click="nextWeek">
          <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M9 5l7 7-7 7" />
          </svg>
        </button>
      </div>
    </div>

    <div v-if="loading" class="loader-container">
      <div class="fancy-loader"></div>
      <p>Fetching specialists...</p>
    </div>

    <div v-else class="doctors-list">
      <div v-for="dr in filteredDoctors" :key="dr.id" class="reception-doc-card shadow-sm">
        <div class="doc-info">
          <div class="doc-avatar">
            {{ dr.name.split(' ').map(n => n[0]).join('').replace('Dr.', '') }}
          </div>
          <div class="doc-details">
            <h3 class="doc-name">{{ dr.name }}</h3>
            <span class="doc-spec">{{ dr.specialty }}</span>
          </div>
        </div>

        <div class="calendar-grid">
          <button 
            v-for="date in weekDates" 
            :key="date.toISOString()" 
            class="calendar-cell"
            :class="{ 'has-slots': getSlotsCount(dr, date) > 0 }"
            @click="getSlotsCount(dr, date) > 0 && openBookingModal(dr, date)"
          >
            <span class="day-name">{{ date.toLocaleDateString('en-US', { weekday: 'short' }) }}</span>
            <span class="day-num">{{ date.getDate() }}</span>
            <span v-if="getSlotsCount(dr, date) > 0" class="slot-badge">
              {{ getSlotsCount(dr, date) }}
            </span>
          </button>
        </div>
      </div>

      <div v-if="filteredDoctors.length === 0" class="empty-results">
        <span class="empty-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="48" height="48">
            <circle cx="11" cy="11" r="8"></circle>
            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
          </svg>
        </span>
        <h3>No balance specialists found</h3>
        <p>Try adjusting your search or filters.</p>
      </div>
    </div>

    <TimeSlotModal
      v-if="showModal"
      :visible="showModal"
      :doctor="activeDoctor"
      :date="activeDate"
      :appointments="activeDoctor.timeslots"
      @close="showModal = false"
    />
  </div>
</template>

<style scoped>
.booking-page { animation: fadeIn 0.4s ease-out; }

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.page-header { margin-bottom: 30px; }
.header-content h1 { font-size: 2.2rem; font-weight: 800; margin: 0; color: var(--text-primary); }
.header-content h1 span { color: var(--color-primary); }
.header-content p { color: var(--text-secondary); margin-top: 5px; }

.search-and-nav {
  background: var(--bg-card);
  border-radius: 0;
  padding: 20px 25px;
  margin-bottom: 30px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 20px;
  border: 1px solid var(--border-default);
}

.search-group { display: flex; gap: 12px; flex: 1; }
.search-input { 
  flex: 1; padding: 10px 15px; border-radius: 0; border: 1px solid var(--border-input);
  background: var(--bg-input); color: var(--text-primary); font-size: 0.9rem;
}
.spec-select {
  padding: 10px 15px; border-radius: 0; border: 1px solid var(--border-input);
  background: var(--bg-input); color: var(--text-primary); font-size: 0.9rem;
}

.nav-group { display: flex; align-items: center; gap: 15px; }
.nav-btn {
  width: 40px; height: 40px; border-radius: 0; border: 1.5px solid var(--border-default);
  background: var(--bg-card); cursor: pointer; display: flex; align-items: center;
  justify-content: center; transition: all 0.2s; color: var(--text-secondary);
}
.nav-btn:hover:not(:disabled) { border-color: var(--color-primary); color: var(--color-primary); background: var(--bg-input); }
.nav-btn:disabled { opacity: 0.3; cursor: not-allowed; }

.nav-icon {
  width: 18px;
  height: 18px;
}

.date-range { text-align: right; }
.range-label { font-size: 0.75rem; color: var(--text-secondary); text-transform: uppercase; font-weight: 700; display: block; }
.range-val { font-size: 1rem; font-weight: 800; color: var(--text-primary); }

.doctors-list { display: flex; flex-direction: column; gap: 20px; }

.reception-doc-card {
  background: var(--bg-card); border-radius: 0; border: 1px solid var(--border-default);
  padding: 20px; display: flex; align-items: center; gap: 30px;
  transition: all 0.3s ease;
}
.reception-doc-card:hover { border-color: var(--card-border); box-shadow: var(--shadow-md); }

.doc-info { display: flex; align-items: center; gap: 20px; min-width: 250px; }
.doc-avatar {
  width: 60px; height: 60px; border-radius: 0; background: var(--bg-input);
  color: var(--color-primary); display: flex; align-items: center; justify-content: center;
  font-weight: 800; font-size: 1.2rem; border: 2px solid var(--border-default);
}
.doc-name { margin: 0; font-size: 1.15rem; font-weight: 800; color: var(--text-primary); }
.doc-spec { color: var(--color-primary); font-weight: 700; font-size: 0.85rem; text-transform: uppercase; }

.calendar-grid { flex: 1; display: grid; grid-template-columns: repeat(7, 1fr); gap: 10px; }

.calendar-cell {
  aspect-ratio: 1; border-radius: 0; border: 1.5px solid var(--border-default);
  background: var(--bg-card); display: flex; flex-direction: column;
  align-items: center; justify-content: center; position: relative;
  cursor: default; transition: all 0.2s; padding: 4px;
}

.calendar-cell.has-slots {
  background: var(--bg-input); border-color: var(--border-default);
  cursor: pointer;
}
.calendar-cell.has-slots:hover {
  border-color: var(--color-primary); transform: translateY(-3px);
  box-shadow: 0 4px 10px rgba(0, 179, 158, 0.15);
}

.day-name { font-size: 0.65rem; font-weight: 700; color: var(--text-secondary); text-transform: uppercase; margin-bottom: 2px; }
.day-num { font-size: 1.1rem; font-weight: 800; color: var(--text-tertiary); }
.has-slots .day-num { color: var(--color-primary-hover); }

.slot-badge {
  position: absolute; top: -6px; right: -6px; width: 18px; height: 18px;
  background: var(--color-primary); color: white; border-radius: 0;
  font-size: 0.65rem; font-weight: 800; display: flex; align-items: center; justify-content: center;
}

.loader-container { text-align: center; padding: 80px 0; color: var(--text-secondary); }
.fancy-loader {
  width: 40px; height: 40px; border: 4px solid var(--border-default);
  border-top-color: var(--color-primary); border-radius: 50%;
  animation: spin 1s linear infinite; margin: 0 auto 15px;
}
@keyframes spin { to { transform: rotate(360deg); } }

.empty-results { text-align: center; padding: 60px; color: var(--text-secondary); }
.empty-icon { font-size: 3rem; margin-bottom: 10px; display: block; opacity: 0.5; }

@media (max-width: 1100px) {
  .reception-doc-card { flex-direction: column; align-items: flex-start; }
  .calendar-grid { width: 100%; }
}

@media (max-width: 768px) {
  .search-and-nav { flex-direction: column; align-items: stretch; }
  .calendar-grid { grid-template-columns: repeat(4, 1fr); }
}
</style>
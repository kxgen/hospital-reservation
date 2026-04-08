<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Header from '@/layouts/Header.vue'
import Footer from '@/layouts/Footer.vue'
import axios from '@/api/axios'

const props = defineProps({
  id: { type: String, required: true }
})

const route = useRoute()
const router = useRouter()
const doctor = ref(null)
const loading = ref(true)

// --- Date Logic ---
const today = new Date()
today.setHours(0, 0, 0, 0)
const currentStartDate = ref(new Date(today))

const nextWeek = () => {
    const next = new Date(currentStartDate.value)
    next.setDate(next.getDate() + 7)
    currentStartDate.value = next
}

const prevWeek = () => {
    const prev = new Date(currentStartDate.value)
    prev.setDate(prev.getDate() - 7)
    if (prev < today) {
        currentStartDate.value = new Date(today)
    } else {
        currentStartDate.value = prev
    }
}

const currentWeekRangeLabel = computed(() => {
    const start = currentStartDate.value
    const end = new Date(start)
    end.setDate(start.getDate() + 6)
    return `${start.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${end.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`
})

// Fetch Doctor Data with Time Slots
onMounted(async () => {
  try {
    const doctorId = route.params.id
    const res = await axios.get(`/api/doctors/${doctorId}`)
    const data = res.data
    
    doctor.value = {
      id: data.id,
      name: data.fullName,
      specialty: data.specialtyName,
      bio: data.bio || '',
      photoUrl: data.photoUrl || '',
      timeslots: data.timeslots || []
    }
  } catch (error) {
    console.error('Error fetching doctor:', error)
  } finally {
    loading.value = false
  }
})

// Filter and group available slots by date (from today onwards)
const availableSlots = computed(() => {
  if (!doctor.value?.timeslots) return []
  
  const now = new Date()
  now.setHours(0, 0, 0, 0)
  
  return doctor.value.timeslots
    .filter(slot => {
      const slotDate = new Date(slot.startTime)
      return slot.isAvailable && slotDate >= now
    })
    .sort((a, b) => new Date(a.startTime) - new Date(b.startTime))
})

// Group slots by date within the 7-day window
const slotsByDate = computed(() => {
  if (!doctor.value?.timeslots) return []
  
  const startRange = currentStartDate.value
  const endRange = new Date(startRange)
  endRange.setDate(startRange.getDate() + 7)

  const grouped = {}
  
  // Filter and initial grouping
  doctor.value.timeslots.forEach(slot => {
    const slotDate = new Date(slot.startTime)
    if (slot.isAvailable && slotDate >= startRange && slotDate < endRange) {
        const dateKey = slotDate.toDateString()
        if (!grouped[dateKey]) {
            grouped[dateKey] = { date: slotDate, slots: [] }
        }
        grouped[dateKey].slots.push(slot)
    }
  })
  
  // Create fixed 7-day array to ensure vertical flow even on empty days
  const result = []
  for (let i = 0; i < 7; i++) {
    const d = new Date(startRange)
    d.setDate(startRange.getDate() + i)
    const dateKey = d.toDateString()
    result.push({
        date: d,
        displayDate: d.toLocaleDateString('en-US', { weekday: 'long' }),
        fullDate: d.toLocaleDateString('en-US', { month: 'long', day: 'numeric' }),
        slots: (grouped[dateKey]?.slots || []).sort((a, b) => new Date(a.startTime) - new Date(b.startTime))
    })
  }
  
  return result
})

// Formatting Helpers
const formatDate = (date) => {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const tomorrow = new Date(today)
  tomorrow.setDate(tomorrow.getDate() + 1)
  
  if (date.toDateString() === today.toDateString()) return 'Today'
  if (date.toDateString() === tomorrow.toDateString()) return 'Tomorrow'
  
  return date.toLocaleDateString('en-US', { 
    weekday: 'long'
  })
}

const formatFullDate = (date) => {
  return date.toLocaleDateString('en-US', { 
    month: 'long', 
    day: 'numeric',
    year: 'numeric'
  })
}

import { formatTime } from '@/utils/dateFormatter'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'

const auth = useAuthStore()
const alertStore = useAlertStore()

const bookSlot = (slot) => {
  if (auth.isLoggedIn && (auth.role === 'admin' || auth.role === 'doctor')) {
    alertStore.showAlert('Staff members cannot use the public patient booking flow. Please manage schedules through your portal.', 'warning');
    return;
  }
  
  const slotDate = new Date(slot.startTime)
  const isReceptionist = auth.role === 'receptionist';
  const targetRoute = isReceptionist ? 'reception-book-form' : 'book-appointment';

  router.push({
    name: targetRoute,
    query: {
      doctorId: doctor.value.id,
      doctorName: doctor.value.name,
      specialty: doctor.value.specialty,
      timeslotId: slot.id || 0,
      startTime: slot.startTime,
      endTime: slot.endTime,
      timeLabel: `${formatFullDate(slotDate)} at ${formatTime(slot.startTime)}`,
    },
  })
}
</script>

<template>
  <div class="page-root">
    <Header />
    
    <div class="page-container">
      <div v-if="!loading && doctor">
      <!-- Hero Section -->
      <header class="hero-section">
        <div class="wrapper">
          <button class="back-link" @click="$router.push('/doctors')">
            <span class="icon">←</span> Back to Search
          </button>

          <div class="hero-flex">
            <div class="profile-avatar">
              {{ doctor.name?.split(' ').map((n) => n[0]).join('').replace('Dr.', '') }}
            </div>
            <div class="hero-info">
              <span class="badge">{{ doctor.specialty }}</span>
              <h1>{{ doctor.name }}</h1>
            </div>
          </div>
        </div>
      </header>

      <!-- Main Content -->
      <div class="layout-grid wrapper">
        <!-- Left Column: Doctor Info -->
        <aside class="info-column">
          <div class="card info-card">
            <h3>About the Doctor</h3>
            <p class="bio-text" v-if="doctor.bio && doctor.bio.trim()">
              {{ doctor.bio }}
            </p>
            <p class="bio-text empty-bio" v-else>
              This doctor has not yet added a professional biography. Please check back later or contact the clinic for more information.
            </p>
          </div>
        </aside>

        <!-- Right Column: Available Time Slots -->
        <main class="schedule-column">
          <div class="card schedule-card">
            <div class="card-header">
              <h3>Available Appointments</h3>
              <p class="subtitle">Book your appointment from the available time slots below</p>
            </div>

            <!-- Time Slots List -->
            <div v-if="doctor.timeslots && doctor.timeslots.length > 0" class="schedule-paginator">
               <div class="paginator-nav">
                  <button class="nav-arrow" @click="prevWeek" :disabled="currentStartDate <= today">
                     <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" width="18"><path d="M15 19l-7-7 7-7"/></svg>
                  </button>
                  <div class="range-info">
                     <span class="range-val">{{ currentWeekRangeLabel }}</span>
                  </div>
                  <button class="nav-arrow" @click="nextWeek">
                     <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" width="18"><path d="M9 5l7 7-7 7"/></svg>
                  </button>
               </div>

               <div class="slots-vertical-list">
                 <div 
                   v-for="(group, index) in slotsByDate" 
                   :key="index"
                   class="day-row"
                 >
                   <div class="day-header">
                     <span class="day-name">{{ group.displayDate }}</span>
                     <span class="day-date">{{ group.fullDate }}</span>
                   </div>
                   
                   <div class="day-slots">
                     <template v-if="group.slots.length > 0">
                       <button
                         v-for="slot in group.slots"
                         :key="slot.id"
                         class="time-pill"
                         @click="bookSlot(slot)"
                       >
                         <span class="time-text">{{ formatTime(slot.startTime) }}</span>
                         <span class="btn-book-label">Book</span>
                       </button>
                     </template>
                     <div v-else class="empty-day-note">No availability for this day</div>
                   </div>
                 </div>
               </div>
            </div>

            <!-- Empty State -->
            <div v-else class="empty-state">
              <div class="empty-icon">📅</div>
              <h4>No Available Appointments</h4>
              <p>This doctor currently has no available time slots. Please check back later or contact the clinic directly.</p>
            </div>
          </div>
        </main>
      </div>
    </div>

    <!-- Loading State -->
    <div v-else-if="loading" class="loading-screen">
      <div class="loader"></div>
      <p>Loading Doctor Profile...</p>
    </div>

    <!-- Error State -->
    <div v-else class="loading-screen">
      <p>Doctor not found</p>
      <button class="back-link" @click="$router.push('/doctors')">
        Back to Search
      </button>
      </div>
    </div>

    <Footer />
  </div>
</template>

<style scoped>
.page-container {
  padding-top: 60px;
  background: var(--bg-body);
  min-height: 100vh;
  padding-bottom: 60px;
}

.wrapper {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 20px;
}

/* Hero Section */
.hero-section {
  background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-hover) 100%);
  color: var(--color-white);
  padding: 60px 0 80px 0;
  text-align: left;
}

.back-link {
  background: rgba(255, 255, 255, 0.15);
  border: none;
  color: var(--color-white);
  padding: 8px 16px;
  border-radius: 20px;
  cursor: pointer;
  font-weight: 500;
  transition: all 0.3s ease;
  margin-bottom: 20px;
  display: inline-flex;
  align-items: center;
  gap: 5px;
}

.back-link:hover {
  background: rgba(255, 255, 255, 0.25);
  transform: translateX(-5px);
}

.hero-flex {
  display: flex;
  align-items: center;
  gap: 25px;
}

.profile-avatar {
  padding: 2rem;
  width: 110px;
  height: 110px;
  background: var(--bg-card);
  color: var(--color-primary);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 2.5rem;
  font-weight: 800;
  box-shadow: var(--shadow-lg);
  flex-shrink: 0;
}

.hero-info h1 {
  font-size: 2.2rem;
  margin: 10px 0 5px 0;
  letter-spacing: -0.5px;
}

.badge {
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(5px);
  color: var(--color-white);
  padding: 5px 14px;
  border-radius: 8px;
  font-size: 0.8rem;
  text-transform: uppercase;
  font-weight: 700;
  letter-spacing: 1px;
  display: inline-block;
}

.location {
  display: flex;
  align-items: center;
  gap: 5px;
  margin-top: 5px;
  opacity: 0.95;
}

/* Main Layout Grid */
.layout-grid {
  display: grid;
  grid-template-columns: minmax(0, 400px) minmax(0, 1fr);
  gap: 30px;
  margin-top: -40px;
}

.card {
  background: var(--bg-card);
  border-radius: 0;
  padding: 24px;
  border: 1px solid var(--border-default);
}

/* Info Column */
.info-card h3 {
  margin-top: 0;
  font-size: 1.3rem;
  color: var(--text-primary);
  margin-bottom: 15px;
}

.info-card h4 {
  font-size: 1rem;
  color: var(--text-primary);
  margin-bottom: 12px;
}

.bio-text {
  line-height: 1.7;
  color: var(--text-secondary);
  font-size: 1rem;
}

.bio-text.empty-bio {
  font-style: italic;
  color: var(--text-tertiary);
  opacity: 0.8;
}

.divider {
  height: 1px;
  background: var(--border-default);
  margin: 25px 0;
}

.credentials-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.credentials-list li {
  margin-bottom: 12px;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 10px;
}

.bullet {
  color: var(--color-primary);
  font-weight: bold;
  font-size: 1.1rem;
}

/* Schedule Column */
.card-header {
  margin-bottom: 25px;
}

.card-header h3 {
  margin: 0 0 5px 0;
  font-size: 1.3rem;
  color: var(--text-primary);
}

.subtitle {
  margin: 0;
  color: var(--text-secondary);
  font-size: 0.9rem;
}

/* Schedule Column Paginator */
.schedule-paginator {
  display: flex;
  flex-direction: column;
  background: var(--bg-card);
}

.paginator-nav {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 15px 0;
  border-bottom: 2px solid var(--border-default);
  margin-bottom: 20px;
}

.nav-arrow {
  width: 40px; height: 40px; border-radius: 50%;
  border: 1.5px solid var(--border-default);
  background: var(--bg-card); cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  color: var(--text-primary); transition: all 0.2s;
}

.nav-arrow:hover:not(:disabled) {
  border-color: var(--color-primary);
  color: var(--color-primary);
  transform: scale(1.1);
}

.nav-arrow:disabled { opacity: 0.3; cursor: not-allowed; }

.range-val { font-weight: 800; font-size: 1.1rem; color: var(--text-primary); }

.slots-vertical-list {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.day-row {
  display: grid;
  grid-template-columns: 180px 1fr;
  padding: 20px 0;
  border-bottom: 1px solid var(--border-default);
}

.day-row:last-child { border-bottom: none; }

.day-header {
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.day-name {
  font-size: 1.1rem;
  font-weight: 800;
  color: var(--text-primary);
}

.day-date {
  font-size: 0.85rem;
  color: var(--text-secondary);
  font-weight: 600;
}

.day-slots {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(110px, 1fr));
  gap: 10px;
  align-items: center;
}

.time-pill {
  background: white;
  border: 1.5px solid var(--border-default);
  padding: 10px;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  align-items: center;
  transition: all 0.2s;
}

.time-pill:hover {
  border-color: var(--color-primary);
  background: var(--bg-input);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px var(--color-primary-glow);
}

.time-text { font-weight: 700; font-size: 0.95rem; color: var(--text-primary); }
.btn-book-label { font-size: 0.65rem; font-weight: 800; text-transform: uppercase; color: var(--color-primary); opacity: 0.8; margin-top: 2px; }

.empty-day-note {
  font-style: italic;
  color: var(--text-tertiary);
  font-size: 0.9rem;
  padding: 10px 0;
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 60px 20px;
  color: var(--text-secondary);
}

.empty-icon {
  font-size: 4rem;
  margin-bottom: 15px;
  opacity: 0.5;
}

.empty-state h4 {
  font-size: 1.2rem;
  color: var(--text-primary);
  margin-bottom: 10px;
}

.empty-state p {
  max-width: 400px;
  margin: 0 auto;
  line-height: 1.6;
}

/* Loading State */
.loading-screen {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 60vh;
}

.loader {
  border: 4px solid var(--bg-input);
  border-top: 4px solid var(--color-primary);
  border-radius: 50%;
  width: 40px;
  height: 40px;
  animation: spin 1s linear infinite;
  margin-bottom: 20px;
}


@keyframes spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}

/* Responsive Media Queries */
@media (max-width: 1024px) {
  .layout-grid {
    grid-template-columns: 1fr;
    margin-top: 20px;
  }

  .hero-section {
    padding: 40px 0 60px 0;
  }
}

@media (max-width: 768px) {
  .hero-flex {
    flex-direction: column;
    text-align: center;
  }

  .hero-info h1 {
    font-size: 1.8rem;
  }

  .time-slots-list {
    grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  }

  .date-group {
    padding-left: 15px;
  }
}

@media (max-width: 480px) {
  .hero-info h1 {
    font-size: 1.5rem;
  }

  .card {
    padding: 20px;
  }

  .time-slots-list {
    grid-template-columns: 1fr 1fr;
  }
}
</style>

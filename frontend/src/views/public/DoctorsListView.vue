<script setup>
import Header from '@/layouts/Header.vue'
import Footer from '@/layouts/Footer.vue'
import DoctorCard from '@/components/DoctorCard.vue'
import AppLoader from '@/components/LoadingBar.vue'
import { useLoaderStore } from '@/stores/loader'
import { ref, computed, onMounted, watch } from 'vue'
import axios from '@/api/axios'

const loaderStore = useLoaderStore()
const searchQuery = ref('')
const selectedSpecialties = ref([])
const selectedGender = ref(null)
const isSidebarOpen = ref(false)
const availableSpecialties = ref([])

// --- Date Logic ---
const today = new Date()
today.setHours(0, 0, 0, 0)
const currentStartDate = ref(today)
const isCurrentPeriod = computed(() => currentStartDate.value.getTime() === today.getTime())

// 4 Months limit (approx 120 days)
const MAX_FUTURE_DATE = new Date(today)
MAX_FUTURE_DATE.setMonth(today.getMonth() + 4)

const isMaxPeriod = computed(() => {
  const nextRangeStart = new Date(currentStartDate.value)
  nextRangeStart.setDate(nextRangeStart.getDate() + 7)
  return nextRangeStart > MAX_FUTURE_DATE
})

// Week Navigation (Steps of 7 days)
const nextWeek = () => {
  if (!isMaxPeriod.value) {
    const newDate = new Date(currentStartDate.value)
    newDate.setDate(newDate.getDate() + 7)
    currentStartDate.value = newDate
  }
}

const prevWeek = () => {
  if (!isCurrentPeriod.value) {
    const newDate = new Date(currentStartDate.value)
    newDate.setDate(newDate.getDate() - 7)
    currentStartDate.value = newDate < today ? today : newDate
  }
}

const weekRange = computed(() => {
  const start = currentStartDate.value
  const end = new Date(start)
  end.setDate(start.getDate() + 6)
  const options = { month: 'long', day: 'numeric' }
  return `${start.toLocaleDateString('en-US', options)} - ${end.toLocaleDateString('en-US', options)}`
})

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

// --- Data Fetching ---
const rawDoctors = ref([])

const toggleSpecialty = (spec) => {
  const index = selectedSpecialties.value.indexOf(spec)
  if (index > -1) {
    selectedSpecialties.value.splice(index, 1)
  } else {
    selectedSpecialties.value.push(spec)
  }
}

const fetchDoctors = async () => {
  loaderStore.startLoading()
  try {
    const params = {
      search: searchQuery.value,
      specialties: selectedSpecialties.value.join(','),
      gender: selectedGender.value || 'both'
    }
    const response = await axios.get('/api/doctors', { params })
    rawDoctors.value = response.data.map((doc) => ({
      id: doc.id,
      name: doc.fullName,
      specialty: doc.specialtyName,
      gender: doc.gender || 'unknown',
      timeslots: doc.timeslots || [],
    }))
  } catch (error) {
    console.error('Error fetching data:', error)
  } finally {
    loaderStore.stopLoading()
  }
}

const clearFilters = () => {
  searchQuery.value = ''
  selectedSpecialties.value = []
  selectedGender.value = null
}

// Watch filters – with a small debounce for search query
let searchTimeout = null
watch(searchQuery, () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    fetchDoctors()
  }, 1000)
})

watch([selectedSpecialties, selectedGender], () => {
  fetchDoctors()
}, { deep: true })

onMounted(async () => {
  try {
    // Fetch specialties
    const specialtiesRes = await axios.get('/api/doctors/specialties')
    availableSpecialties.value = specialtiesRes.data.map(s => s.name)
  } catch (error) {
    console.error('Error fetching specialties:', error)
  }
  
  // Initial fetch
  fetchDoctors()
})

const filteredDoctors = computed(() => rawDoctors.value)
</script>

<template>
  <Header />

  <div class="page-container">
    <header class="hero-section">
      <div class="wrapper">
        <h1>Find Your Specialist</h1>
        <div class="search-box">
          <input v-model="searchQuery" placeholder="Search by name or specialty..." @keyup.enter="fetchDoctors" />
        </div>
      </div>
    </header>

    <div class="layout-grid wrapper">
      <aside class="sidebar">
        <div class="filter-card">
          <div class="filter-header">
            <h3 class="filter-title">Filters</h3>
            <button class="clear-btn" @click="clearFilters">Clear All</button>
          </div>

          <div class="filter-group">
            <label>Gender</label>
            <div class="gender-toggle">
              <button :class="{ active: !selectedGender }" @click="selectedGender = null">
                Both
              </button>
              <button
                :class="{ active: selectedGender === 'male' }"
                @click="selectedGender = 'male'"
              >
                Male
              </button>
              <button
                :class="{ active: selectedGender === 'female' }"
                @click="selectedGender = 'female'"
              >
                Female
              </button>
            </div>
          </div>

          <div class="filter-group">
            <label>Specialties</label>
            <div class="specialty-grid">
              <div
                v-for="spec in availableSpecialties"
                :key="spec"
                class="spec-chip"
                :class="{ active: selectedSpecialties.includes(spec) }"
                @click="toggleSpecialty(spec)"
              >
                {{ spec }}
              </div>
            </div>
          </div>
        </div>
      </aside>

      <main class="results-column">
        <div class="date-bar">
          <div class="date-info">
            <span class="label">From: </span>
            <span class="range">{{ weekRange }}</span>
          </div>
          <div class="nav-buttons">
            <button class="nav-arrow" @click="prevWeek" :disabled="isCurrentPeriod">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M15 19l-7-7 7-7" />
              </svg>
            </button>
            <button class="nav-arrow" @click="nextWeek" :disabled="isMaxPeriod">
              <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M9 5l7 7-7 7" />
              </svg>
            </button>
          </div>
        </div>

        <div class="feed" v-if="!loaderStore.isLoading">
          <DoctorCard
            v-for="doctor in filteredDoctors"
            :key="doctor.id"
            :doctor="doctor"
            :week-dates="weekDates"
          />
          
          <div v-if="filteredDoctors.length === 0" class="no-results">
            <div class="no-results-content">
              <span class="no-results-icon">👨‍⚕️</span>
              <h3>No doctors found</h3>
              <p>Try adjusting your search or filters to find what you're looking for.</p>
            </div>
          </div>
        </div>
        <AppLoader v-else />
      </main>
    </div>
  </div>
  <Footer />
</template>

<style scoped>
.no-results {
  background: var(--bg-card);
  border-radius: 0;
  padding: 60px 20px;
  text-align: center;
  border: 1px dashed var(--border-default);
}

.no-results-icon {
  font-size: 3rem;
  margin-bottom: 20px;
  display: block;
}

.no-results h3 {
  font-size: 1.5rem;
  color: var(--text-primary);
  margin-bottom: 10px;
}

.no-results p {
  color: var(--text-secondary);
}

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
  background: var(--color-primary);
  color: var(--color-white);
  padding: 60px 0 80px;
  text-align: center;
}
.hero-section h1 {
  font-size: 2.2rem;
  margin-bottom: 25px;
}
.search-box {
  display: flex;
  max-width: 600px;
  margin: 0 auto;
  background: var(--bg-card);
  padding: 6px;
  border-radius: 50px;
  box-shadow: var(--shadow-lg);
}
.search-box input {
  flex: 1;
  border: none;
  padding: 12px 20px;
  outline: none;
  border-radius: 50px;
  font-size: 1rem;
}
.search-btn {
  background: var(--color-gray-800);
  color: var(--color-white);
  border: none;
  padding: 12px 30px;
  border-radius: 50px;
  cursor: pointer;
  font-weight: 600;
}

/* Layout Structure */
.layout-grid {
  display: grid;
  grid-template-columns: minmax(0, 300px) minmax(0, 1fr);
  gap: 30px;
  margin-top: -40px;
}

/* Sidebar & Filters */
.filter-card {
  background: var(--bg-card);
  border-radius: 0;
  padding: 24px;
  border: 1px solid var(--border-default);
  position: sticky;
  top: 20px;
}
.filter-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}
.filter-title {
  margin-top: 0;
  font-size: 1.2rem;
  color: var(--text-primary);
  margin-bottom: 0;
}
.clear-btn {
  background: transparent;
  border: none;
  color: var(--color-primary);
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 4px;
}
.clear-btn:hover {
  background: var(--bg-input);
}
.filter-group {
  margin-bottom: 30px;
}
.filter-group label {
  display: block;
  font-weight: 700;
  font-size: 0.85rem;
  color: var(--text-tertiary);
  text-transform: uppercase;
  margin-bottom: 12px;
}

/* Gender Toggle Styling */
.gender-toggle {
  display: flex;
  background: var(--bg-input);
  padding: 4px;
  border-radius: 10px;
}
.gender-toggle button {
  flex: 1;
  border: none;
  padding: 8px;
  background: transparent;
  cursor: pointer;
  border-radius: 8px;
  font-weight: 600;
  color: var(--text-secondary);
}
.gender-toggle button.active {
  background: var(--bg-card);
  color: var(--color-primary);
  box-shadow: var(--shadow-sm);
}

/* Specialty Chip Styling */
.specialty-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.spec-chip {
  padding: 8px 14px;
  border: 1px solid var(--border-default);
  border-radius: 50px;
  font-size: 0.85rem;
  cursor: pointer;
  background: var(--bg-card);
  transition: 0.2s;
}
.spec-chip.active {
  background: var(--color-primary);
  color: var(--color-white);
  border-color: var(--color-primary);
}
.spec-chip:hover:not(.active) {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

/* Results Section */
.date-bar {
  background: var(--bg-card);
  border-radius: 0;
  padding: 15px 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  border: 1px solid var(--border-default);
}
.date-info .range {
  font-weight: 700;
  color: var(--text-primary);
  font-size: 1.1rem;
}
.nav-buttons {
  display: flex;
  gap: 10px;
}
.nav-arrow {
  background: var(--bg-body);
  border: 1.5px solid var(--border-default);
  width: 42px;
  height: 42px;
  border-radius: 10px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  color: var(--text-secondary);
}

.nav-arrow:hover:not(:disabled) {
  border-color: var(--color-primary);
  color: var(--color-primary);
  background: var(--bg-input);
  transform: translateY(-1px);
}

.nav-icon {
  width: 20px;
  height: 20px;
}

.nav-arrow:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

/* Responsive Adjustments */
@media (max-width: 1024px) {
  .layout-grid {
    grid-template-columns: 1fr;
    margin-top: 20px;
  }
  .sidebar {
    display: none;
  } /* On mobile/tablet, filters would usually be in a modal */
}
</style>

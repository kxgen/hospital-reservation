<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import AppointmentCard from '@/components/AppointmentCard.vue'

const auth = useAuthStore()

const currentTab = ref('Upcoming')
const upcomingAppointments = ref([])
const pendingAppointments = ref([])
const historyAppointments = ref([])
const historyFilter = ref('All') // All, Completed, Cancelled
const isLoading = ref(false)

const getAuthHeaders = () => ({
  headers: { Authorization: `Bearer ${auth.token || localStorage.getItem('token')}` },
})

const fetchUpcoming = async () => {
  isLoading.value = true
  try {
    const res = await axios.get(`/api/patient/appointments/upcoming`, getAuthHeaders())
    upcomingAppointments.value = res.data
  } catch (err) {
    console.error('Fetch upcoming failed:', err)
  } finally {
    isLoading.value = false
  }
}

const fetchHistory = async () => {
  isLoading.value = true
  try {
    const res = await axios.get(`/api/patient/appointments/history`, getAuthHeaders())
    historyAppointments.value = res.data
  } catch (err) {
    console.error('Fetch history failed:', err)
  } finally {
    isLoading.value = false
  }
}

const fetchPending = async () => {
  isLoading.value = true
  try {
    const res = await axios.get(`/api/patient/appointments/pending`, getAuthHeaders())
    pendingAppointments.value = res.data
  } catch (err) {
    console.error('Fetch pending failed:', err)
  } finally {
    isLoading.value = false
  }
}

const loadTabContent = () => {
  if (currentTab.value === 'Upcoming') fetchUpcoming()
  else if (currentTab.value === 'Pending') fetchPending()
  else fetchHistory()
}

watch(currentTab, loadTabContent)

const filteredAppointments = computed(() => {
  if (currentTab.value === 'Upcoming') return upcomingAppointments.value
  if (currentTab.value === 'Pending') return pendingAppointments.value
  
  // History filtering
  if (historyFilter.value === 'All') return historyAppointments.value
  return historyAppointments.value.filter(a => a.status.toLowerCase() === historyFilter.value.toLowerCase())
})

const tabs = ['Upcoming', 'Pending', 'History']

onMounted(loadTabContent)
</script>

<template>
  <div class="appointments-view container">
    <header class="view-header">
      <div class="header-text">
        <h1>My <span>Appointments</span></h1>
        <p>Comprehensive history of your healthcare journey.</p>
      </div>
      <router-link to="/patient/dashboard" class="btn-back">
        <span class="icon">🏠</span> Dashboard
      </router-link>
    </header>

    <div class="tabs-wrapper">
      <div class="tabs-segmented">
        <button
          v-for="tab in tabs"
          :key="tab"
          :class="['tab-seg-btn', { active: currentTab === tab }]"
          @click="currentTab = tab"
        >
          {{ tab }}
        </button>
      </div>
    </div>

    <!-- History Filter Section -->
    <div v-if="currentTab === 'History'" class="filter-bar animate-fade-in">
      <span class="filter-label">Filter Status:</span>
      <div class="filter-options">
        <button 
          v-for="f in ['All', 'Completed', 'Cancelled']" 
          :key="f"
          :class="['filter-chip', { active: historyFilter === f }]"
          @click="historyFilter = f"
        >
          {{ f }}
        </button>
      </div>
    </div>

    <div class="list-wrapper">
      <div v-if="isLoading" class="state-msg">
        <div class="spinner"></div>
        <p>Retrieving records...</p>
      </div>

      <div v-else-if="filteredAppointments.length === 0" class="state-msg">
        <div class="empty-icon">
          <span v-if="currentTab === 'Upcoming'">📅</span>
          <span v-else-if="currentTab === 'History'">📂</span>
          <span v-else>🚫</span>
        </div>
        <h3>No {{ currentTab.toLowerCase() }} sessions</h3>
        <p v-if="currentTab === 'Upcoming'">You don't have any pending appointments at the moment.</p>
        <p v-else>Electronic records for {{ currentTab.toLowerCase() }} visits will appear here.</p>
        <router-link v-if="currentTab === 'Upcoming'" to="/doctors" class="btn-primary-small"
          >Book Appointment</router-link
        >
      </div>

      <div v-else class="cards-stack">
        <AppointmentCard
          v-for="appt in filteredAppointments"
          :key="appt.id"
          :appt="appt"
          show-action
          :action-label="currentTab === 'Upcoming' ? 'Manage Appointment' : 'View Details'"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
.appointments-view {
  min-height: 80vh;
  max-width: 75%;
  margin: 0 auto;
  padding: 1.5rem 15px;
}

.list-wrapper {
  padding: 1em;
  border-top: 1px solid var(--border-default);
}

.view-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 40px;
}

.view-header h1 {
  font-size: 2.2rem;
  font-weight: 800;
  color: var(--text-primary);
  margin: 0;
  letter-spacing: -0.5px;
}
.view-header h1 span { color: var(--color-primary); }
.view-header p { color: var(--text-secondary); margin-top: 5px; font-weight: 500; }

.btn-back {
  display: flex;
  align-items: center;
  gap: 8px;
  text-decoration: none;
  color: var(--text-secondary);
  font-weight: 700;
  font-size: 0.9rem;
  padding: 10px 18px;
  border-radius: 10px;
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  transition: all 0.2s;
}
.btn-back:hover { border-color: var(--color-primary); color: var(--color-primary); transform: translateX(-4px); }

/* Segmented Tabs */
.tabs-wrapper {
  margin-bottom: 25px;
}

/* History Filter */
.filter-bar {
  display: flex;
  align-items: center;
  gap: 15px;
  margin-bottom: 30px;
  padding: 0 5px;
}

.filter-label {
  font-size: 0.8rem;
  font-weight: 800;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.filter-options {
  display: flex;
  gap: 10px;
}

.filter-chip {
  padding: 6px 16px;
  border-radius: 50px;
  border: 1.5px solid var(--border-default);
  background: var(--bg-card);
  color: var(--text-secondary);
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.filter-chip:hover {
  border-color: var(--color-primary-border);
  color: var(--color-primary);
}

.filter-chip.active {
  background: var(--color-primary-light);
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.tabs-segmented {
  display: inline-flex;
  background: var(--bg-input);
  padding: 6px;
  border-radius: 14px;
}

.tab-seg-btn {
  padding: 10px 24px;
  border: none;
  background: none;
  cursor: pointer;
  font-weight: 700;
  color: var(--text-secondary);
  font-size: 0.95rem;
  border-radius: 10px;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
  display: flex;
  align-items: center;
  gap: 10px;
}

.tab-seg-btn.active {
  background: var(--bg-card);
  color: var(--color-primary);
  box-shadow: var(--shadow-sm);
}

.tab-badge {
  background: var(--color-primary-light);
  color: var(--color-primary);
  font-size: 0.7rem;
  padding: 2px 8px;
  border-radius: 20px;
  font-weight: 800;
}

/* List Layout */
.cards-stack {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

/* States */
.state-msg {
  text-align: center;
  padding: 80px 40px;
  color: var(--text-secondary);
  background: var(--bg-card);
  border-radius: 20px;
  border: 1px dashed var(--border-input);
}

.empty-icon { font-size: 3rem; margin-bottom: 15px; }
.state-msg h3 { font-size: 1.4rem; color: var(--text-primary); margin-bottom: 10px; font-weight: 800; }
.state-msg p { max-width: 400px; margin: 0 auto 25px; line-height: 1.6; }

.btn-primary-small {
  background: var(--color-primary);
  color: white;
  padding: 12px 28px;
  border-radius: 10px;
  text-decoration: none;
  font-weight: 700;
  display: inline-block;
  transition: all 0.2s;
}

.btn-primary-small:hover { transform: translateY(-2px); box-shadow: var(--shadow-primary); }

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid var(--color-gray-100);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 20px;
}

@keyframes spin { to { transform: rotate(360deg); } }

@media (max-width: 600px) {
  .tabs-segmented { width: 100%; display: flex; }
  .tab-seg-btn { flex: 1; padding: 10px 10px; font-size: 0.85rem; }
  .view-header { flex-direction: column; align-items: flex-start; gap: 20px; }
  .btn-back { order: -1; }
}
</style>

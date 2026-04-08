<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { formatTime, formatDate } from '@/utils/dateFormatter'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const appointmentId = route.params.id
const details = ref(null)
const isLoading = ref(true)
const error = ref(null)

const fetchDetails = async () => {
  isLoading.value = true
  try {
    const res = await axios.get(`/api/appointments/${appointmentId}/followup-details`, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    details.value = res.data
  } catch (err) {
    error.value = "Failed to load appointment details."
    console.error(err)
  } finally {
    isLoading.value = false
  }
}

const historyChain = computed(() => {
  if (!details.value || !details.value.appointmentHistory) return []
  
  const all = details.value.appointmentHistory
  const currentId = parseInt(appointmentId)
  
  // Find the 'root' of this specific follow-up chain
  const findRoot = (aptId) => {
    const apt = all.find(a => a.id === aptId)
    if (apt && apt.parentAppointmentId) {
      return findRoot(apt.parentAppointmentId)
    }
    return apt
  }

  const root = findRoot(currentId)
  if (!root) return []

  // Build the chain downwards from root
  const chain = []
  let current = root
  while (current) {
    chain.push(current)
    current = all.find(a => a.parentAppointmentId === current.id)
  }
  
  // Filter chain if needed, though usually chain is relevant regardless of status?
  // User asked to hide cancelled from full history, maybe keep them in chain if they explain gaps?
  // Let's keep chain intact but filter Full History below.
  return chain.reverse()
})

const filteredHistory = computed(() => {
    if (!details.value || !details.value.appointmentHistory) return []
    const now = new Date()
    // Show completed or past appointments
    return details.value.appointmentHistory
        .filter(a => new Date(a.startTime) <= now || a.status.toLowerCase() === 'completed')
        .sort((a, b) => new Date(b.startTime) - new Date(a.startTime)) // Descending
})

const chainReliability = computed(() => {
    // Calculate reliability based on the *current* history chain only
    if (historyChain.value.length === 0) return { attended: 0, missed: 0, rate: '0%' }
    
    // Check 'completed' vs 'no_show' in the chain
    const attended = historyChain.value.filter(a => a.status.toLowerCase() === 'completed').length
    const missed = historyChain.value.filter(a => a.status.toLowerCase() === 'no_show').length
    const total = attended + missed
    const rate = total > 0 ? Math.round((attended / total) * 100) + '%' : 'N/A'
    
    return { attended, missed, rate }
})

// Find the next actionable appointment in the chain (for highlighting)
const nextActionableAppointment = computed(() => {
    if (historyChain.value.length === 0) return null
    
    // Find the first appointment that is NOT completed, cancelled, or no_show
    const actionable = historyChain.value.find(apt => {
        const status = apt.status.toLowerCase()
        return status !== 'completed' && status !== 'cancelled' && status !== 'no_show'
    })
    
    return actionable || null
})

const getPreviousPlan = (index) => {
    const prevApt = historyChain.value[index + 1]
    return prevApt ? prevApt.doctorReminder : null
}

const startSession = () => {
  router.push({ name: 'appointment-session', params: { id: appointmentId } })
}

const goBack = () => {
  router.push({ name: 'doctor-appointments' })
}

onMounted(fetchDetails)
</script>

<template>
  <div class="page-container">
    
    <div v-if="isLoading" class="state-container">
      <div class="loader"></div>
      <p>Loading Clinical Record...</p>
    </div>

    <div v-else-if="error" class="state-container">
      <div class="error-box">
        <h3>Unable to load record</h3>
        <p>{{ error }}</p>
        <button @click="goBack">Return to Dashboard</button>
      </div>
    </div>

    <div v-else-if="details" class="content-wrapper animate-entry">
      
      <header class="top-bar">
        <div class="breadcrumbs">
          <button @click="goBack" class="link-back">
            ← Back to Schedule
          </button>
          <span class="separator">/</span>
          <span class="current-crumb">Appointment Details</span>
        </div>
        <!-- Removed duplicate actions block here as requested -->
      </header>
      <!-- Removed phone number from meta-tags as requested -->
      <section class="patient-banner">
        <div class="patient-identity">
          <div class="avatar-circle">
            {{ details.patient.firstName.charAt(0) }}{{ details.patient.lastName.charAt(0) }}
          </div>
          <div class="identity-text">
            <h1>{{ details.patient.firstName }} {{ details.patient.lastName }}</h1>
            <div class="meta-tags">
              <span class="meta-item">{{ details.patient.age }} Years</span>
              |
              <span class="meta-item">{{ details.patient.gender }}</span>
            </div>
          </div>
        </div>
        
        <div class="patient-stats">
          <div class="stat-box">
            <span class="stat-label">Attendance</span>
            <div class="stat-value-row">
              <span class="big-number">{{ chainReliability.rate }}</span>
              <div class="stat-mini">
                <div>{{ chainReliability.attended }}</div>
                <div class="text-danger">{{ chainReliability.missed }}</div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <div class="clinical-grid">
        
        <main class="main-column">
          
          <!-- Consolidated Card: Timeline + Current Details -->
          <div class="card" v-if="historyChain.length > 0">
            <div class="card-header">
              <h2>Session Timeline</h2>
              <span class="badge">{{ historyChain.length }} Visits in Chain</span>
            </div>
            <div class="card-body">
              <div class="timeline-container">
                <div v-for="(apt, index) in historyChain" :key="apt.id" 
                     class="timeline-item"
                     :class="{ 'is-active': nextActionableAppointment && apt.id === nextActionableAppointment.id, 'is-past': index < historyChain.length - 1 }">
                  
                  <div class="timeline-marker">
                    <div class="line"></div>
                    <div class="dot"></div>
                  </div>
                  
                  <div class="timeline-content">
                    <div class="timeline-date">{{ formatDate(apt.startTime) }}</div>
                    <div class="timeline-card">
                      <div class="tc-header">
                        <div class="tc-header-start">
                          Status: 
                          <span class="status-badge" :class="apt.status.toLowerCase()">{{ apt.status }}</span>
                        </div>
                        <div class="tc-header-end">
                          <div class="scheduled-display">
                            <span class="s-time">{{ formatTime(apt.startTime) }}</span>
                          </div>
                        </div>
                      </div>
                      
                      <div class="timeline-body">
                         <!-- Patient's stated reason for visit -->
                         <div class="info-row">
                            <span class="info-label">Patient's Reason for Visit</span>
                            <p class="info-value">{{ apt.reason || 'Not specified' }}</p>
                         </div>

                         <!-- For EVERY appointment: show context from its previous visit in the chain -->
                         <div v-if="getPreviousPlan(index)" class="info-row context-box">
                            <span class="info-label">Plan from Previous Session</span>
                            <p class="info-value doctor-text">"{{ getPreviousPlan(index) }}"</p>
                         </div>
                      </div>

                      <!-- Action Area for Current Appointment (Only if not completed) -->
                      <div v-if="apt.id === parseInt(appointmentId) && !['completed', 'cancelled', 'no_show'].includes(apt.status.toLowerCase())" class="timeline-actions">
                         <div v-if="details.currentAppointment.checkedInAt" class="action-btn-wrapper">
                            <button @click="startSession" class="btn-primary-sq">
                               Start Medical Session
                            </button>
                         </div>
                         <div v-else class="check-in-warning">
                            <span class="icon">ℹ️</span> Waiting for Patient Check-in
                         </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="card">
            <div class="card-header">
              <h2>Clinical History</h2>
            </div>
            
            <div class="history-section">
                <div class="section-label">Past History</div>
                <div class="table-responsive">
                  <table class="clean-table">
                    <thead>
                      <tr>
                        <th>Date</th>
                        <th>Reason</th>
                        <th>Doctor Note</th>
                        <th>Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="apt in filteredHistory" :key="apt.id">
                        <td class="font-mono">{{ formatDate(apt.startTime) }}</td>
                        <td class="font-medium">{{ apt.reason || 'Check-up' }}</td>
                        <td class="text-muted">{{ apt.doctorReminder || '-' }}</td>
                        <td>
                          <span class="status-badge" :class="apt.status.toLowerCase()">
                            {{ apt.status }}
                          </span>
                        </td>
                      </tr>
                      <tr v-if="filteredHistory.length === 0">
                          <td colspan="4" class="empty-text">No past medical history found.</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
            </div>
          </div>
        </main>

        <!-- Removed Side Column as requested to merge elements into main flow -->
        
      </div>
    </div>
  </div>
</template>

<style scoped>
/* BASE VARIABLES */
:root {
  --radius-md: 0;
  --radius-lg: 0;
  --space-md: 24px;
}

.page-container {
  max-width: 1000px; /* narrowed for better focus */
  margin: 0 auto;
  padding: 40px 24px;
  background-color: var(--bg-body);
  min-height: 100vh;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
  color: var(--text-primary);
}

/* STATE CONTAINERS */
.state-container {
  height: 60vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: var(--text-secondary);
}
.loader {
  width: 40px; height: 40px;
  border: 3px solid var(--border-default);
  border-top-color: var(--color-primary);
  border-radius: 0;
  animation: spin 0.8s linear infinite;
  margin-bottom: 16px;
}
@keyframes spin { to { transform: rotate(360deg); } }

/* HEADER */
.top-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}
.breadcrumbs {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 0.9rem;
}
.link-back {
  background: none; border: none; padding: 0;
  color: var(--text-secondary);
  cursor: pointer;
  font-weight: 500;
}
.link-back:hover { color: var(--color-primary); text-decoration: underline; }
.separator { color: var(--border-default); }
.current-crumb { font-weight: 600; color: var(--text-primary); }

.status-notice {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--text-secondary);
  background: var(--bg-body);
  padding: 8px 16px;
  border-radius: 0;
  border: 1px solid var(--border-default);
  display: flex;
  align-items: center;
  gap: 8px;
}
.status-notice.inline { margin-top: 10px; justify-content: center; }
.status-notice .icon { font-size: 1rem; }

/* PATIENT BANNER */
.patient-banner {
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  border-radius: var(--radius-lg);
  padding: 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 32px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.02);
}

.patient-identity {
  display: flex;
  align-items: center;
  gap: 20px;
}
.avatar-circle {
  width: 64px; height: 64px;
  background-color: var(--color-primary);
  color: white;
  border-radius: 0;
  display: flex; align-items: center; justify-content: center;
  font-size: 1.5rem; font-weight: 700;
  letter-spacing: -1px;
}
.identity-text h1 {
  margin: 0 0 6px 0;
  font-size: 1.6rem;
  font-weight: 700;
}
.meta-tags {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--text-secondary);
  font-size: 0.95rem;
}
.dot { font-size: 0.6rem; opacity: 0.5; }

.stat-box {
  text-align: right;
  padding-left: 32px;
  border-left: 1px solid var(--border-default);
}
.stat-label {
  display: block;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--text-secondary);
  margin-bottom: 4px;
}
.stat-value-row {
  display: flex;
  align-items: center;
  gap: 16px;
}
.big-number {
  font-size: 2rem;
  font-weight: 800;
  color: var(--color-primary);
  line-height: 1;
}
.stat-mini {
  font-size: 0.8rem;
  font-weight: 600;
  line-height: 1.4;
  color: var(--text-tertiary);
}
.text-danger { color: #ef4444; }

/* GRID LAYOUT */
.clinical-grid {
  display: grid;
  grid-template-columns: 1fr; /* Single column now */
  gap: 32px;
}

/* CARDS */
.card {
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  border-radius: var(--radius-lg);
  margin-bottom: 24px;
  overflow: hidden;
}
.card-header {
  padding: 16px 24px;
  border-bottom: 1px solid var(--border-default);
  display: flex;
  justify-content: space-between;
  align-items: center;
  background-color: var(--bg-table-header);
}
.card-header.no-border { border-bottom: none; padding-bottom: 0; }
.card-header h2 {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 700;
  color: var(--text-primary);
}
.badge {
  background: var(--bg-body);
  border: 1px solid var(--border-default);
  padding: 4px 10px;
  border-radius: 0;
  font-size: 0.75rem;
  font-weight: 600;
}
.card-body { padding: 24px; }

/* TIMELINE STYLES */
.timeline-container {
  position: relative;
  padding-left: 10px;
}
.timeline-item {
  display: flex;
  gap: 24px;
  position: relative;
  padding-bottom: 32px;
}
.timeline-item:last-child { padding-bottom: 0; }

.timeline-marker {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 20px;
}
.line {
  position: absolute;
  top: 8px; bottom: -8px; left: 9px;
  width: 2px;
  background-color: var(--border-default);
}
.timeline-item:last-child .line { display: none; }
.dot {
  width: 12px; height: 12px;
  border-radius: 0;
  background: var(--bg-card);
  border: 2px solid var(--border-default);
  z-index: 2;
  margin-top: 6px;
}
.timeline-item.is-active .dot {
  background: var(--color-primary);
  border-color: var(--color-primary);
  box-shadow: 0 0 0 4px rgba(var(--color-primary-rgb), 0.15);
}

.timeline-content { flex: 1; }
.timeline-date {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-secondary);
  margin-bottom: 8px;
}

.timeline-card {
  background: var(--bg-body);
  border: 1px solid var(--border-default);
  border-radius: 0;
  padding: 16px;
  transition: 0.2s;
}
.is-active .timeline-card {
  border-color: var(--color-primary);
  background: var(--bg-card);
  box-shadow: 0 4px 12px rgba(0,0,0,0.05);
}

.tc-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
.tc-title { font-weight: 700; font-size: 1rem; }
.tc-note {
  font-style: italic;
  color: var(--text-secondary);
  font-size: 0.9rem;
  margin: 0;
  line-height: 1.5;
}
.current-indicator {
  display: inline-block;
  margin-top: 10px;
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--color-primary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

/* Merged Details Block */
.current-details-block { margin-top: 16px; }
.divider-sm { border: 0; border-top: 1px dashed var(--border-default); margin: 16px 0; }
.big-time-sm { font-size: 1.4rem; font-weight: 300; line-height: 1; margin-bottom: 5px; }
.action-area { margin-top: 20px; }

/* TABLE STYLES */
.clean-table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
.clean-table th {
  text-align: left;
  padding: 12px 16px;
  border-bottom: 2px solid var(--border-default);
  color: var(--text-secondary);
  font-weight: 600;
  font-size: 0.8rem;
  text-transform: uppercase;
}
.clean-table td {
  padding: 16px;
  border-bottom: 1px solid var(--border-default);
  vertical-align: middle;
}
.clean-table tr:last-child td { border-bottom: none; }
.font-mono { font-family: monospace; color: var(--text-secondary); }

.info-group label {
  display: block;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  color: var(--text-secondary);
  margin-bottom: 4px;
}
.reminder-box {
  background-color: #fefce8; 
  border: 1px solid #fde047;
  color: #854d0e;
  padding: 8px 12px;
  border-radius: 0;
  font-size: 0.9rem;
  line-height: 1.5;
}
.btn-block {
  width: 100%;
  padding: 12px;
  background: var(--color-primary);
  color: white;
  border: none;
  font-weight: 700;
  border-radius: 0;
  cursor: pointer;
  transition: 0.2s;
}
.btn-block:hover { opacity: 0.9; }

/* UTILS */
.status-badge {
  padding: 2px 8px; border-radius: 0; font-size: 0.7rem; font-weight: 700; text-transform: uppercase; border: 1px solid;
}
.status-text { font-weight: 600; font-size: 0.85rem; text-transform: capitalize; }

.status-badge.completed, .status-text.completed { color: #166534; border-color: #bbf7d0; background: #f0fdf4; }
.status-badge.scheduled, .status-text.scheduled { color: #b45309; border-color: #fde68a; background: #fffbeb; }
.status-badge.cancelled, .status-text.cancelled { color: #991b1b; border-color: #fecaca; background: #fef2f2; }
.status-text.completed, .status-text.scheduled, .status-text.cancelled { background: none; border: none; }

.animate-entry { animation: fadeUp 0.5s ease-out forwards; opacity: 0; transform: translateY(10px); }
@keyframes fadeUp { to { opacity: 1; transform: translateY(0); } }

@media (max-width: 900px) {
  .clinical-grid { grid-template-columns: 1fr; }
  .patient-banner { flex-direction: column; text-align: center; gap: 20px; }
  .patient-identity { flex-direction: column; }
  .stat-box { border-left: none; padding-left: 0; border-top: 1px solid var(--border-default); padding-top: 20px; text-align: center; }
  .stat-value-row { justify-content: center; }
}

/* timeline layout cleanup */
.tc-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--border-default);
  margin-bottom: 16px;
}
.scheduled-display {
  text-align: right;
}
.scheduled-display .s-label {
  display: block;
  font-size: 0.65rem;
  font-weight: 800;
  text-transform: uppercase;
  color: var(--text-tertiary);
  letter-spacing: 0.5px;
}
.scheduled-display .s-time {
  font-size: 1.1rem;
  font-weight: 700;
  color: var(--color-primary);
}

.timeline-body {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.info-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.info-label {
  font-size: 0.7rem;
  font-weight: 700;
  text-transform: uppercase;
  color: var(--text-secondary);
  letter-spacing: 0.5px;
}

.info-value {
  margin: 0;
  font-size: 0.95rem;
  color: var(--text-primary);
  line-height: 1.5;
}

.context-box {
  background-color: #fffbeb;
  border: 1px solid #fde68a;
  padding: 12px;
  margin: 4px 0;
}

.timeline-actions {
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px dashed var(--border-default);
}

.btn-primary-sq {
  width: 100%;
  padding: 14px;
  background-color: var(--color-primary);
  color: white;
  border: none;
  font-weight: 700;
  cursor: pointer;
  text-transform: uppercase;
  letter-spacing: 1px;
}
.btn-primary-sq:hover { opacity: 0.95; }

.check-in-warning {
  background-color: #f8fafb;
  border: 1px solid var(--border-default);
  padding: 12px;
  text-align: center;
  font-weight: 600;
  color: var(--text-secondary);
  font-size: 0.9rem;
}

.current-indicator-line {
  margin-top: 12px;
  text-align: center;
  font-size: 0.7rem;
  font-weight: 800;
  color: var(--color-primary);
  text-transform: uppercase;
  letter-spacing: 2px;
  opacity: 0.8;
}

.doctor-text {
  color: var(--color-primary);
  font-weight: 500;
  font-style: italic;
}

.history-section {
  margin-bottom: 60px;
}
.history-section:last-child {
  margin-bottom: 0;
}


.section-label {
  padding: 16px 24px 8px;
  font-size: 0.85rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--color-primary);
  background: var(--bg-body);
  border-bottom: 1px solid var(--border-default);
}

.empty-text {
  text-align: center;
  color: var(--text-tertiary);
  font-style: italic;
  padding: 32px;
}
</style>